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
using NanoByte.Common.Tasks;
using ZeroInstall.Publish.Properties;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.WinForms
{
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
            using (var openFileDialog = new OpenFileDialog { FileName = textBoxLocalPath.Text })
            {
                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    textBoxLocalPath.Text = openFileDialog.FileName;
            }
        }

        private void pageDownload_Commit(object sender, WizardPageConfirmEventArgs e)
        {
            var fileName = checkLocalCopy.Checked ? textBoxLocalPath.Text : textBoxDownloadUrl.Text;

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
                    switch (Archive.GuessMimeType(fileName))
                    {
                        case Archive.MimeTypeMsi:
                            OnInstaller();
                            break;
                        case null:
                            OnSingleFile();
                            break;
                        default:
                            OnArchive();
                            break;
                    }
                }
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

        private void OnSingleFile()
        {
            Retrieve(
                new SingleFile { Href = textBoxDownloadUrl.Uri },
                checkLocalCopy.Checked ? textBoxLocalPath.Text : null);
            _feedBuilder.ImplementationDirectory = _feedBuilder.TemporaryDirectory;
            using (var handler = new DialogTaskHandler(this))
            {
                _feedBuilder.DetectCandidates(handler);
                _feedBuilder.GenerateDigest(handler);
            }
            if (_feedBuilder.MainCandidate == null) throw new NotSupportedException(Resources.NoEntryPointsFound);
            else
            {
                _feedBuilder.GenerateCommands();
                pageDownload.NextPage = pageDetails;
            }
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
                _installerCapture.SetLocal(textBoxDownloadUrl.Uri, textBoxLocalPath.Text);
            else
            {
                using (var handler = new DialogTaskHandler(this))
                    _installerCapture.Download(textBoxDownloadUrl.Uri, handler);
            }

            pageDownload.NextPage = pageIstallerCaptureStart;
        }
    }
}