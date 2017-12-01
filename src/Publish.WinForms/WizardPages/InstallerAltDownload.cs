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
using System.Net;
using System.Windows.Forms;
using AeroWizard;
using NanoByte.Common;
using ZeroInstall.Store.Model;

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
            using (var openFileDialog = new OpenFileDialog { FileName = textBoxAltLocalPath.Text })
            {
                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    textBoxAltLocalPath.Text = openFileDialog.FileName;
            }
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
