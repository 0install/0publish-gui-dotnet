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
using AeroWizard;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Publish.Capture;
using ZeroInstall.Publish.Properties;

namespace ZeroInstall.Publish.WinForms
{
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
            catch (IOException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
            }
            catch (UnauthorizedAccessException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
            }
            catch (InvalidOperationException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Error);
            }
            #endregion
        }

        private void buttonSkipCapture_Click(object sender, EventArgs e)
        {
            if (!Msg.YesNo(this, Resources.AskSkipCapture, MsgSeverity.Info)) return;

            try
            {
                using (var handler = new DialogTaskHandler(this))
                    _installerCapture.ExtractInstallerAsArchive(_feedBuilder, handler);
            }
            #region Error handling
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException ex)
            {
                Msg.Inform(this, Resources.InstallerExtractFailed + Environment.NewLine + ex.Message, MsgSeverity.Warn);
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                Msg.Inform(this, ex.Message, MsgSeverity.Error);
                return;
            }
            #endregion

            wizardControl.NextPage(pageArchiveExtract, skipCommit: true);
        }
    }
}
