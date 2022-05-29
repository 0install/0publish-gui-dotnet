// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using AeroWizard;
using ZeroInstall.Publish.Capture;

namespace ZeroInstall.Publish.WinForms;

partial class NewFeedWizard
{
    private void pageInstallerCaptureStart_Commit(object sender, WizardPageConfirmEventArgs e)
    {
        try
        {
            var captureSession = CaptureSession.Start(_feedBuilder);

            using (var handler = new DialogTaskHandler(this))
                _installerCapture.RunInstaller(handler);

            _installerCapture.CaptureSession = captureSession;
        }
        #region Error handling
        catch (OperationCanceledException)
        {
            e.Cancel = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            e.Cancel = true;
            Log.Warn("Feed Wizard: Failed to run and capture installer", ex);
            Msg.Inform(this, ex.Message, MsgSeverity.Warn);
        }
        #endregion
    }

    private void buttonSkipCapture_Click(object sender, EventArgs e)
    {
        if (!Msg.YesNo(this, Resources.AskSkipCapture, MsgSeverity.Info)) return;

        try
        {
            using var handler = new DialogTaskHandler(this);
            _installerCapture.ExtractInstallerAsArchive(_feedBuilder, handler);
        }
        #region Error handling
        catch (OperationCanceledException)
        {
            return;
        }
        catch (IOException ex)
        {
            Log.Warn("Feed Wizard: Failed to extract installer", ex);
            Msg.Inform(this, Resources.InstallerExtractFailed + Environment.NewLine + ex.Message, MsgSeverity.Warn);
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Error("Feed Wizard: Failed to extract installer", ex);
            Msg.Inform(this, ex.Message, MsgSeverity.Error);
            return;
        }
        #endregion

        wizardControl.NextPage(pageArchiveExtract, skipCommit: true);
    }
}
