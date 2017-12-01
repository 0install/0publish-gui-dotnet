/*
 * Copyright 2010-2017 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.WinForms
{
    partial class NewFeedWizard
    {
        private void pageInstallerCollectFiles_ToggleControls(object sender, EventArgs e)
        {
            buttonCreateArchive.Enabled = (textBoxUploadUrl.Text.Length > 0) && textBoxUploadUrl.IsValid && (textBoxArchivePath.Text.Length > 0);
        }

        private void buttonSelectArchivePath_Click(object sender, EventArgs e)
        {
            string filter = StringUtils.Join(@"|",
                ArchiveGenerator.SupportedMimeTypes.Select(x => string.Format(
                    @"{0} archive (*{0})|*{0}",
                    Archive.GetDefaultExtension(x))));
            using (var saveFileDialog = new SaveFileDialog { Filter = filter, FileName = textBoxArchivePath.Text })
            {
                if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                    textBoxArchivePath.Text = saveFileDialog.FileName;
            }
        }

        private void buttonCreateArchive_Click(object sender, EventArgs e)
        {
            try
            {
                using (var handler = new DialogTaskHandler(this))
                    _installerCapture.CaptureSession.CollectFiles(textBoxArchivePath.Text, textBoxUploadUrl.Uri, handler);
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
