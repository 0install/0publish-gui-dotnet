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
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using AeroWizard;
using NanoByte.Common;
using ZeroInstall.Publish.EntryPoints;
using ZeroInstall.Publish.Properties;

namespace ZeroInstall.Publish.WinForms
{
    partial class NewFeedWizard
    {
        private Icon _icon;

        private void pageIcon_Initialize(object sender, WizardPageInitEventArgs e)
        {
            pictureBoxIcon.Visible = buttonSaveIco.Enabled = buttonSavePng.Enabled = false;

            if (_feedBuilder.MainCandidate is IIconContainer iconContainer)
            {
                try
                {
                    _icon = iconContainer.ExtractIcon();
                    pictureBoxIcon.Image = _icon.ToBitmap();
                }
                #region Error handling
                catch (IOException ex)
                {
                    Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                    return;
                }
                #endregion

                pictureBoxIcon.Visible = buttonSaveIco.Enabled = buttonSavePng.Enabled = true;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "System.Drawing exceptions are not clearly documented")]
        private void buttonSaveIco_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog {Filter = "Windows Icon files|*.ico|All files|*.*"})
            {
                if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    using (var stream = File.Create(saveFileDialog.FileName))
                    {
                        try
                        {
                            _icon.Save(stream);
                        }
                        #region Error handling
                        catch (Exception ex)
                        {
                            Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                        }
                        #endregion
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "System.Drawing exceptions are not clearly documented")]
        private void buttonSavePng_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog { Filter = "PNG image files|*.png|All files|*.*" })
            {
                if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _icon.ToBitmap().Save(saveFileDialog.FileName, ImageFormat.Png);
                    }
                    #region Error handling
                    catch (Exception ex)
                    {
                        Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                    }
                    #endregion
                }
            }
        }

        private void pageIcon_Commit(object sender, WizardPageConfirmEventArgs e)
        {
            _feedBuilder.Icons.Clear();
            try
            {
                if (textBoxHrefIco.Uri != null) _feedBuilder.Icons.Add(new Store.Model.Icon { Href = textBoxHrefIco.Uri, MimeType = Store.Model.Icon.MimeTypeIco });
                if (textBoxHrefPng.Uri != null) _feedBuilder.Icons.Add(new Store.Model.Icon { Href = textBoxHrefPng.Uri, MimeType = Store.Model.Icon.MimeTypePng });
            }
            #region Error handling
            catch (UriFormatException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                return;
            }
            #endregion

            if (_feedBuilder.Icons.Count != 2)
                if (!Msg.YesNo(this, Resources.AskSkipIcon, MsgSeverity.Info)) e.Cancel = true;
        }
    }
}
