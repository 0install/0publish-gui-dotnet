// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Controls;
using NanoByte.Common.Info;
using NanoByte.Common.Tasks;
using ZeroInstall.Publish.WinForms.Properties;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish.WinForms
{
    /// <summary>
    /// The main GUI for the Zero Install Feed Editor.
    /// </summary>
    internal partial class MainForm : Form
    {
        #region Properties
        private FeedEditing _feedEditing;

        /// <summary>
        /// The feed currently being edited.
        /// </summary>
        private FeedEditing FeedEditing
        {
            get => _feedEditing;
            set
            {
                _feedEditing = value;

                if (_feedEditing != null)
                {
                    _feedEditing.UndoEnabledChanged += OnFeedChange;
                    _feedEditing.RedoEnabledChanged += OnFeedChange;

                    feedStructureEditor.Open(_feedEditing);
                    OnFeedChange();
                }

                ListKeys();
            }
        }

        private void OnFeedChange()
        {
            menuUndo.Enabled = buttonUndo.Enabled = _feedEditing.UndoEnabled;
            menuRedo.Enabled = buttonRedo.Enabled = _feedEditing.RedoEnabled;

            try
            {
                _feedEditing.SignedFeed.Feed.ResolveInternalReferences();
            }
            catch (InvalidDataException ex)
            {
                Log.Warn(ex);
            }
        }
        #endregion

        #region Constructor
        private readonly IOpenPgp _openPgp;
        private readonly IDisposable _implementationProviderRegistration;

        /// <summary>
        /// Creates a new feed editing form.
        /// </summary>
        /// <param name="feedEditing">The feed to open on start up.</param>
        /// <param name="openPgp">The OpenPGP-compatible system used to create signatures.</param>
        public MainForm([NotNull] FeedEditing feedEditing, [NotNull] IOpenPgp openPgp)
        {
            InitializeComponent();
            _openPgp = openPgp;

            FeedEditing = feedEditing;

            // Enable Recipe steps to call out to external Fetcher
            _implementationProviderRegistration = FetchHandle.Register(impl =>
            {
                using (var handler = new DialogTaskHandler(this))
                    handler.RunTask(new ExternalFetch(impl));
                return ImplementationStores.Default().GetPath(impl);
            });
        }
        #endregion

        #region Event handlers
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                AskForChangeSave();
            }
            catch (OperationCanceledException)
            {
                e.Cancel = true;
            }
        }

        private void menuNew_Click(object sender, EventArgs e)
        {
            try
            {
                AskForChangeSave();

                FeedEditing = new FeedEditing();
                comboBoxKeys.SelectedItem = null;
            }
            catch (OperationCanceledException)
            {}
        }

        private void menuNewWizard_Click(object sender, EventArgs e)
        {
            try
            {
                AskForChangeSave();

                var result = NewFeedWizard.Run(_openPgp, this);
                if (result != null) FeedEditing = new FeedEditing(result);
            }
            catch (OperationCanceledException)
            {}
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            try
            {
                AskForChangeSave();
                FeedEditing = OpenFeed(this);
            }
            catch (OperationCanceledException)
            {}
        }

        private void menuSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFeed();
            }
            catch (OperationCanceledException)
            {}
        }

        private void menuSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFeedAs();
            }
            catch (OperationCanceledException)
            {}
        }

        private void menuExit_Click(object sender, EventArgs e) => Close();

        private void menuUndo_Click(object sender, EventArgs e) => feedStructureEditor.Undo();

        private void menuRedo_Click(object sender, EventArgs e) => feedStructureEditor.Redo();

        private void menuRemove_Click(object sender, EventArgs e) => feedStructureEditor.Remove();

        private void menuAbout_Click(object sender, EventArgs e) => Msg.Inform(this, AppInfo.Current.Name + " " + AppInfo.Current.Version, MsgSeverity.Info);
        #endregion

        #region Storage
        internal static FeedEditing OpenFeed([CanBeNull] IWin32Window owner = null)
        {
            using var openFileDialog = new OpenFileDialog {Filter = Resources.FileDialogFilter};
            if (openFileDialog.ShowDialog(owner) != DialogResult.OK) throw new OperationCanceledException();

            try
            {
                return FeedEditing.Load(openFileDialog.FileName);
            }
            #region Error handling
            catch (IOException ex)
            {
                Msg.Inform(null, ex.Message, MsgSeverity.Warn);
                throw new OperationCanceledException();
            }
            catch (UnauthorizedAccessException ex)
            {
                Msg.Inform(null, ex.Message, MsgSeverity.Warn);
                throw new OperationCanceledException();
            }
            catch (InvalidDataException ex)
            {
                Msg.Inform(null, ex.Message + (ex.InnerException == null ? "" : Environment.NewLine + ex.InnerException.Message), MsgSeverity.Warn);
                throw new OperationCanceledException();
            }
            #endregion
        }

        private void SaveFeed()
        {
            if (string.IsNullOrEmpty(FeedEditing.Path)) SaveFeedAs();
            else SaveFeed(FeedEditing.Path);
        }

        private void SaveFeedAs()
        {
            using var saveFileDialog = new SaveFileDialog {Filter = Resources.FileDialogFilter};
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                SaveFeed(saveFileDialog.FileName);
            else
                throw new OperationCanceledException();
        }

        private void SaveFeed([NotNull] string path)
        {
            while (true)
            {
                try
                {
                    try
                    {
                        FeedEditing.Save(path);
                        break; // Exit loop if passphrase is correct
                    }
                    catch (WrongPassphraseException ex)
                    {
                        // Continue loop if passhrase is incorrect
                        Log.Error(ex);
                    }

                    // Ask for passphrase to unlock secret key if we were unable to save without it
                    FeedEditing.Passphrase = InputBox.Show(this, Text, string.Format(Resources.AskForPassphrase, FeedEditing.SignedFeed.SecretKey), password: true);
                    if (FeedEditing.Passphrase == null) throw new OperationCanceledException();
                }
                #region Error handling
                catch (ArgumentException ex)
                {
                    Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                    throw new OperationCanceledException();
                }
                catch (IOException ex)
                {
                    Msg.Inform(null, ex.Message, MsgSeverity.Warn);
                    throw new OperationCanceledException();
                }
                catch (UnauthorizedAccessException ex)
                {
                    Msg.Inform(null, ex.Message, MsgSeverity.Warn);
                    throw new OperationCanceledException();
                }
                catch (KeyNotFoundException ex)
                {
                    Msg.Inform(null, ex.Message, MsgSeverity.Warn);
                    throw new OperationCanceledException();
                }
                #endregion
            }
        }

        private void AskForChangeSave()
        {
            if (FeedEditing.UnsavedChanges)
            {
                switch (Msg.YesNoCancel(this, Resources.SaveQuestion, MsgSeverity.Warn, Resources.SaveChanges, Resources.DiscardChanges))
                {
                    case DialogResult.Yes:
                        SaveFeed();
                        break;
                    case DialogResult.Cancel:
                        throw new OperationCanceledException();
                }
            }
        }
        #endregion

        #region Keys
        private void ListKeys()
        {
            comboBoxKeys.Items.Clear();
            comboBoxKeys.Items.Add("");
            comboBoxKeys.Items.AddRange(_openPgp.ListSecretKeys().Cast<object>().ToArray());
            comboBoxKeys.Items.Add(NewKeyAction.Instance);

            if (FeedEditing != null) comboBoxKeys.SelectedItem = FeedEditing.SignedFeed.SecretKey;
        }

        private void comboBoxKeys_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxKeys.SelectedItem == NewKeyAction.Instance) NewKey();
            else FeedEditing.SignedFeed.SecretKey = comboBoxKeys.SelectedItem as OpenPgpSecretKey;
        }

        private void NewKey()
        {
            Process process;
            try
            {
                process = GnuPG.GenerateKey();
            }
            #region Error handling
            catch (IOException ex)
            {
                Log.Error(ex);
                Msg.Inform(this, ex.Message, MsgSeverity.Error);
                return;
            }
            #endregion

            ThreadUtils.StartBackground(() =>
            {
                process.WaitForExit();

                // Update key list when done
                try
                {
                    Invoke(new Action(ListKeys));
                }
                #region Sanity checks
                catch (InvalidOperationException)
                {
                    // Ignore if window has been dispoed
                }
                #endregion
            }, name: "WaitForOpenPgp");
        }

        private class NewKeyAction
        {
            private NewKeyAction()
            {}

            public static readonly NewKeyAction Instance = new NewKeyAction();

            public override string ToString() => Resources.NewKey;
        }
        #endregion
    }
}
