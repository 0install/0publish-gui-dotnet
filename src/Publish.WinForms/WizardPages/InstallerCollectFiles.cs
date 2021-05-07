// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NanoByte.Common;
using NanoByte.Common.Controls;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Archives;

namespace ZeroInstall.Publish.WinForms
{
    partial class NewFeedWizard
    {
        private void pageInstallerCollectFiles_ToggleControls(object sender, EventArgs e)
            => buttonCreateArchive.Enabled = (textBoxUploadUrl.Text.Length > 0) && textBoxUploadUrl.IsValid && (textBoxArchivePath.Text.Length > 0);

        private void buttonSelectArchivePath_Click(object sender, EventArgs e)
        {
            string filter = StringUtils.Join(@"|",
                ArchiveGenerator.SupportedMimeTypes.Select(x => string.Format(
                    @"{0} archive (*{0})|*{0}",
                    Archive.GetDefaultExtension(x))));
            using var saveFileDialog = new SaveFileDialog {Filter = filter, FileName = textBoxArchivePath.Text};
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                textBoxArchivePath.Text = saveFileDialog.FileName;
        }

        private void buttonCreateArchive_Click(object sender, EventArgs e)
        {
            try
            {
                using var handler = new DialogTaskHandler(this);
                _installerCapture.CaptureSession!.CollectFiles(textBoxArchivePath.Text, textBoxUploadUrl.Uri!, handler);
            }
            #region Error handling
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException ex)
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

            wizardControl.NextPage(pageEntryPoint);
        }

        private void buttonExistingArchive_Click(object sender, EventArgs e) => wizardControl.NextPage(pageInstallerAltDownload);
    }
}
