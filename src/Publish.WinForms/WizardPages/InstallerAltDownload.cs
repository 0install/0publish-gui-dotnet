// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using AeroWizard;

namespace ZeroInstall.Publish.WinForms;

partial class NewFeedWizard
{
    private void pageInstallerAltDownload_ToggleControls(object sender, EventArgs e)
    {
        groupAltLocalCopy.Enabled = checkAltLocalCopy.Checked;

        pageInstallerAltDownload.AllowNext =
            (textBoxAltDownloadUrl.Text.Length > 0) && textBoxAltDownloadUrl.IsValid &&
            (!checkAltLocalCopy.Checked || textBoxAltLocalPath.Text.Length > 0);
    }

    private void buttonSelectAltLocalPath_Click(object sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog {FileName = textBoxAltLocalPath.Text};
        if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            textBoxAltLocalPath.Text = openFileDialog.FileName;
    }

    private void pageInstallerAltDownload_Commit(object sender, WizardPageConfirmEventArgs e)
    {
        try
        {
            Retrieve(
                new Archive {Href = textBoxAltDownloadUrl.Uri!},
                checkAltLocalCopy.Checked ? textBoxAltLocalPath.Text : null);
            pageInstallerAltDownload.NextPage = pageArchiveExtract;
        }
        #region Error handling
        catch (OperationCanceledException)
        {
            e.Cancel = true;
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException or IOException or UnauthorizedAccessException or WebException or NotSupportedException)
        {
            e.Cancel = true;
            Log.Warn("Feed Wizard: Failed to download alternate archive", ex);
            Msg.Inform(this, ex.Message, MsgSeverity.Warn);
        }
        #endregion
    }
}
