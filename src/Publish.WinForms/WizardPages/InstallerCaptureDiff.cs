// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Windows.Forms;
using AeroWizard;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Publish.Properties;

namespace ZeroInstall.Publish.WinForms
{
    partial class NewFeedWizard
    {
        private void buttonSelectInstallationDir_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog
            {
                RootFolder = Environment.SpecialFolder.MyComputer,
                SelectedPath = textBoxInstallationDir.Text
            })
            {
                folderBrowserDialog.ShowDialog(this);
                textBoxInstallationDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void pageInstallerCaptureDiff_Commit(object sender, WizardPageConfirmEventArgs e)
        {
            var session = _installerCapture.CaptureSession;
            if (session == null) return;

            try
            {
                session.InstallationDir = textBoxInstallationDir.Text;
                using (var handler = new DialogTaskHandler(this))
                    session.Diff(handler);
            }
            #region Error handling
            catch (InvalidOperationException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                return;
            }
            catch (IOException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                return;
            }
            #endregion

            try
            {
                using (var handler = new DialogTaskHandler(this))
                    _installerCapture.ExtractInstallerAsArchive(_feedBuilder, handler);

                pageInstallerCaptureDiff.NextPage = pageArchiveExtract;
            }
            catch (IOException)
            {
                Msg.Inform(this, Resources.InstallerExtractFailed + Environment.NewLine + Resources.InstallerNeedAltSource, MsgSeverity.Info);
                pageInstallerCaptureDiff.NextPage = pageInstallerCollectFiles;
            }
            #region Error handling
            catch (OperationCanceledException)
            {
                e.Cancel = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Error);
            }
            #endregion
        }

        private void pageInstallerCaptureDiff_Rollback(object sender, WizardPageConfirmEventArgs e) => _installerCapture.CaptureSession = null;
    }
}
