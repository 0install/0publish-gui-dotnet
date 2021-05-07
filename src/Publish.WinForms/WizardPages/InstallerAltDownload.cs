// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using AeroWizard;
using NanoByte.Common.Controls;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.WinForms
{
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
                    new Archive {Href = textBoxAltDownloadUrl.Uri},
                    checkAltLocalCopy.Checked ? textBoxAltLocalPath.Text : null);
                pageInstallerAltDownload.NextPage = pageArchiveExtract;
            }
            #region Error handling
            catch (OperationCanceledException)
            {
                e.Cancel = true;
            }
            catch (ArgumentException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
            }
            catch (UriFormatException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
            }
            catch (IOException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
            }
            catch (UnauthorizedAccessException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Error);
            }
            catch (WebException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
            }
            catch (NotSupportedException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
            }
            #endregion
        }
    }
}
