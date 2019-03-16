// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Windows.Forms;
using JetBrains.Annotations;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish.WinForms
{
    /// <summary>
    /// The welcome window for the Zero Install Publishing Tools.
    /// </summary>
    internal partial class WelcomeForm : Form
    {
        #region Dependencies
        private readonly IOpenPgp _openPgp;

        /// <summary>
        /// Creates a new welcome form.
        /// </summary>
        /// <param name="openPgp">The OpenPGP-compatible system used to create signatures.</param>
        public WelcomeForm([NotNull] IOpenPgp openPgp)
        {
            #region Sanity checks
            if (openPgp == null) throw new ArgumentNullException(nameof(openPgp));
            #endregion

            InitializeComponent();

            _openPgp = openPgp;
        }
        #endregion

        private void buttonNewEmpty_Click(object sender, EventArgs e) => SwitchToMain(new FeedEditing());

        private void buttonNewWizard_Click(object sender, EventArgs e)
        {
            var result = NewFeedWizard.Run(_openPgp, this);
            if (result != null) SwitchToMain(new FeedEditing(result));
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            try
            {
                SwitchToMain(MainForm.OpenFeed(this));
            }
            catch (OperationCanceledException)
            {}
        }

        private void SwitchToMain(FeedEditing feedEditing)
        {
            using (var form = new MainForm(feedEditing, _openPgp))
            {
                form.Shown += delegate { Hide(); };
                form.ShowDialog();
            }
            Close();
        }
    }
}
