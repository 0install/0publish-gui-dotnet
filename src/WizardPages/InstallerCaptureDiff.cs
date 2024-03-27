// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using AeroWizard;

namespace ZeroInstall.Publish.WinForms;

partial class NewFeedWizard
{
    private void buttonSelectInstallationDir_Click(object sender, EventArgs e)
    {
        using var folderBrowserDialog = new FolderBrowserDialog
        {
            RootFolder = Environment.SpecialFolder.MyComputer,
            SelectedPath = textBoxInstallationDir.Text
        };
        folderBrowserDialog.ShowDialog(this);
        textBoxInstallationDir.Text = folderBrowserDialog.SelectedPath;
    }

    private void pageInstallerCaptureDiff_Commit(object sender, WizardPageConfirmEventArgs e)
    {
        var session = _installerCapture.CaptureSession;
        if (session == null) return;

        try
        {
            session.InstallationDir = textBoxInstallationDir.Text;
            using var handler = new DialogTaskHandler(this);
            session.Diff(handler);
        }
        #region Error handling
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            e.Cancel = true;
            Log.Warn("Feed Wizard: Failed to collect desktop integration data", ex);
            Msg.Inform(this, ex.GetMessageWithInner(), MsgSeverity.Warn);
            return;
        }
        #endregion

        try
        {
            using (var handler = new DialogTaskHandler(this))
                _installerCapture.ExtractInstallerAsArchive(_feedBuilder, handler);

            pageInstallerCaptureDiff.NextPage = pageArchiveExtract;
        }
        catch (OperationCanceledException)
        {
            e.Cancel = true;
        }
        catch (IOException ex)
        {
            Log.Warn("Feed Wizard: Failed to extract installer", ex);
            Msg.Inform(this, Resources.InstallerExtractFailed + Environment.NewLine + Resources.InstallerNeedAltSource, MsgSeverity.Info);
            pageInstallerCaptureDiff.NextPage = pageInstallerCollectFiles;
        }
        catch (UnauthorizedAccessException ex)
        {
            e.Cancel = true;
            Log.Error("Feed Wizard: Failed to extract installer", ex);
            Msg.Inform(this, ex.GetMessageWithInner(), MsgSeverity.Error);
        }
    }

    private void pageInstallerCaptureDiff_Rollback(object sender, WizardPageConfirmEventArgs e) => _installerCapture.CaptureSession = null;
}
