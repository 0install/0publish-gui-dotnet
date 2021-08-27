// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using NanoByte.Common.Controls;
using NanoByte.Common.Tasks;
using NanoByte.Common.Undo;
using NanoByte.StructureEditor;
using NanoByte.StructureEditor.WinForms;
using ZeroInstall.Model;
using ZeroInstall.Publish.WinForms.Properties;
using ICommandExecutor = NanoByte.Common.Undo.ICommandExecutor;

namespace ZeroInstall.Publish.WinForms.Controls
{
    /// <summary>
    /// A common base for <see cref="RetrievalMethod"/> editors.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="RetrievalMethod"/> to edit.</typeparam>
    public abstract class RetrievalMethodEditor<T> : NodeEditorBase<T>, ITargetContainerInject<Implementation>
        where T : RetrievalMethod
    {
        #region Properties
        private Implementation? _targetContainer;

        /// <inheritdoc/>
        public virtual Implementation? TargetContainer
        {
            get => _targetContainer;
            set
            {
                _targetContainer = value;
                _buttonAddMissing.Visible = (value != null);

                UpdateHint();
            }
        }
        #endregion

        #region Constructor
        private readonly Label _labelUpdateHint;
        private readonly Button _buttonAddMissing;

        protected RetrievalMethodEditor()
        {
            Controls.Add(_labelUpdateHint = new Label
            {
                Location = new Point(0, 150),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                TabIndex = 1000,
                ForeColor = Color.Red,
                Visible = false
            });
            Controls.Add(_buttonAddMissing = new Button
            {
                Top = _labelUpdateHint.Bottom + 6,
                Size = new Size(123, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                TabIndex = 1001,
                Text = Resources.AddMissing,
                UseVisualStyleBackColor = true,
                Visible = false
            });
            _buttonAddMissing.Click += buttonAddMissing_Click;
        }
        #endregion

        //--------------------//

        #region Add missing
        /// <summary>
        /// Displays hints explaining why calling "Update" may be required.
        /// </summary>
        protected virtual void UpdateHint()
        {
            if (TargetContainer != null && TargetContainer.ManifestDigest == default(ManifestDigest))
                ShowUpdateHint(Resources.ManifestDigestMissing);
            else _labelUpdateHint.Visible = false;
        }

        /// <summary>
        /// Displays a specific update hint if <see cref="_buttonAddMissing"/> is active.
        /// </summary>
        /// <param name="hint">The hint to display.</param>
        protected void ShowUpdateHint(string hint)
        {
            if (!_buttonAddMissing.Visible) return;

            _labelUpdateHint.Text = hint + @" " + Resources.PleaseClick;
            _labelUpdateHint.Visible = true;
        }

        private void buttonAddMissing_Click(object sender, EventArgs e)
        {
            var commandCollector = new CommandCollector {Path = CommandExecutor?.Path}; // Represent all changes in a single undo step
            try
            {
                using var handler = new DialogTaskHandler(this);
                CheckDigest(commandCollector, handler);
            }
            #region Error handling
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or WebException or NotSupportedException)
            {
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                Msg.Inform(this, ex.Message, MsgSeverity.Error);
                return;
            }
            #endregion

            finally
            {
                var command = commandCollector.BuildComposite();
                if (CommandExecutor == null) command.Execute();
                else CommandExecutor.Execute(command);
            }

            _labelUpdateHint.Visible = false;
        }
        #endregion

        #region Manifest digest
        /// <summary>
        /// Checks whether the <see cref="ManifestDigest"/> in <see cref="TargetContainer"/> matches the generated value.
        /// </summary>
        /// <param name="executor">Used to apply properties in an undoable fashion.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        private void CheckDigest(ICommandExecutor executor, ITaskHandler handler)
        {
            if (TargetContainer == null) return;

            var digest = Target!.CalculateDigest(executor, handler);

            if (digest.PartialEquals(ManifestDigest.Empty))
                Msg.Inform(this, Resources.EmptyImplementation, MsgSeverity.Warn);

            void SetDigest()
            {
                executor.Execute(SetValueCommand.For(() => TargetContainer.ManifestDigest, digest));

                if (string.IsNullOrEmpty(TargetContainer.ID) || TargetContainer.ID.StartsWith("sha"))
                    executor.Execute(SetValueCommand.For(() => TargetContainer.ID, digest.Best));
            }

            if (TargetContainer.ManifestDigest == default) SetDigest();
            else if (digest != TargetContainer.ManifestDigest)
            {
                bool warnOtherImplementations = (TargetContainer.RetrievalMethods.Count > 1);
                if (Msg.YesNo(this,
                    warnOtherImplementations ? Resources.DigestMismatch + Environment.NewLine + Resources.DigestOtherImplementations : Resources.DigestMismatch,
                    warnOtherImplementations ? MsgSeverity.Warn : MsgSeverity.Info,
                    Resources.DigestReplace, Resources.DigestKeep))
                    SetDigest();
            }
        }
        #endregion
    }
}
