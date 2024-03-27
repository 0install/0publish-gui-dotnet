// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using AeroWizard;

namespace ZeroInstall.Publish.WinForms;

partial class NewFeedWizard
{
    private void pageDownload_ToggleControls(object sender, EventArgs e)
    {
        groupLocalCopy.Enabled = checkLocalCopy.Checked;

        pageDownload.AllowNext =
            (textBoxDownloadUrl.Text.Length > 0) && textBoxDownloadUrl.IsValid &&
            (!checkLocalCopy.Checked || textBoxLocalPath.Text.Length > 0);
    }

    private void buttonSelectLocalPath_Click(object sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog {FileName = textBoxLocalPath.Text};
        if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            textBoxLocalPath.Text = openFileDialog.FileName;
    }

    private void pageDownload_Commit(object sender, WizardPageConfirmEventArgs e)
    {
        string fileName = checkLocalCopy.Checked ? textBoxLocalPath.Text : textBoxDownloadUrl.Text;
        outputBoxDownloadUrl.Text = textBoxDownloadUrl.Text;

        try
        {
            if (fileName.EndsWithIgnoreCase(@".exe"))
            {
                switch (Msg.YesNoCancel(this, Resources.AskInstallerEXE, MsgSeverity.Info, Resources.YesInstallerExe, Resources.NoSingleExecutable))
                {
                    case DialogResult.Yes:
                        OnInstaller();
                        break;
                    case DialogResult.No:
                        OnSingleFile();
                        break;
                    default:
                        e.Cancel = true;
                        break;
                }
            }
            else
            {
                try
                {
                    switch (Archive.GuessMimeType(fileName))
                    {
                        case Archive.MimeTypeMsi:
                            OnInstaller();
                            break;
                        default:
                            OnArchive();
                            break;
                    }
                }
                catch (NotSupportedException)
                {
                    OnSingleFile();
                }
            }
        }
        #region Error handling
        catch (OperationCanceledException)
        {
            e.Cancel = true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or WebException or NotSupportedException)
        {
            e.Cancel = true;
            Log.Warn("Feed Wizard: Failed to download file", ex);
            Msg.Inform(this, ex.GetMessageWithInner(), MsgSeverity.Warn);
        }
        #endregion
    }

    private void OnSingleFile()
    {
        Retrieve(
            new SingleFile {Href = textBoxDownloadUrl.Uri, Destination = "dummy"},
            checkLocalCopy.Checked ? textBoxLocalPath.Text : null);
        _feedBuilder.ImplementationDirectory = _feedBuilder.TemporaryDirectory!;
        using (var handler = new DialogTaskHandler(this))
        {
            _feedBuilder.DetectCandidates(handler);
            _feedBuilder.GenerateDigest(handler);
        }
        if (_feedBuilder.MainCandidate == null) throw new NotSupportedException(Resources.NoEntryPointsFound);
        else  pageDownload.NextPage = pageDetails;
    }

    private void OnArchive()
    {
        Retrieve(
            new Archive {Href = textBoxDownloadUrl.Uri},
            checkLocalCopy.Checked ? textBoxLocalPath.Text : null);
        pageDownload.NextPage = pageArchiveExtract;
    }

    private void OnInstaller()
    {
        if (checkLocalCopy.Checked)
            _installerCapture.SetLocal(textBoxDownloadUrl.Uri!, textBoxLocalPath.Text);
        else
        {
            using var handler = new DialogTaskHandler(this);
            _installerCapture.Download(textBoxDownloadUrl.Uri!, handler);
        }

        pageDownload.NextPage = pageIstallerCaptureStart;
    }
}
