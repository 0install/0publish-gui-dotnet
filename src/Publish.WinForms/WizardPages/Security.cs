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
using System.Diagnostics;
using System.IO;
using System.Linq;
using AeroWizard;
using NanoByte.Common;
using ZeroInstall.Publish.Properties;
using ZeroInstall.Store;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish.WinForms
{
    partial class NewFeedWizard
    {
        /// <summary>Used to get a list of <see cref="OpenPgpSecretKey"/>s.</summary>
        private readonly IOpenPgp _openPgp;

        private void pageSecurity_Initialize(object sender, WizardPageInitEventArgs e) => ListKeys();

        private void ListKeys()
        {
            comboBoxKeys.Items.Clear();
            comboBoxKeys.Items.Add("");
            comboBoxKeys.Items.AddRange(_openPgp.ListSecretKeys().Cast<object>().ToArray());

            comboBoxKeys.SelectedItem = _feedBuilder.SecretKey;
        }

        private void comboBoxKeys_SelectedIndexChanged(object sender, EventArgs e)
            => _feedBuilder.SecretKey = comboBoxKeys.SelectedItem as OpenPgpSecretKey;

        private void buttonNewKey_Click(object sender, EventArgs e)
        {
            Process process;
            try
            {
                process = GnuPG.GenerateKey();
            }
            #region Error handling
            catch (IOException ex)
            {
                Log.Error(ex);
                Msg.Inform(this, ex.Message, MsgSeverity.Error);
                return;
            }
            #endregion

            ThreadUtils.StartBackground(() =>
            {
                process.WaitForExit();

                // Update key list when done
                try
                {
                    Invoke(new Action(ListKeys));
                }
                #region Sanity checks
                catch (InvalidOperationException)
                {
                    // Ignore if window has been dispoed
                }
                #endregion
            }, name: "WaitForOpenPgp");
        }

        private void pageSecurity_Commit(object sender, WizardPageConfirmEventArgs e)
        {
            _feedBuilder.SecretKey = comboBoxKeys.SelectedItem as OpenPgpSecretKey;
            try
            {
                _feedBuilder.Uri = (textBoxInterfaceUri.Uri == null) ? null : new FeedUri(textBoxInterfaceUri.Uri);
            }
            #region Error handling
            catch (UriFormatException ex)
            {
                e.Cancel = true;
                Msg.Inform(this, ex.Message, MsgSeverity.Warn);
                return;
            }
            #endregion

            if (_feedBuilder.SecretKey == null || _feedBuilder.Uri == null)
                if (!Msg.YesNo(this, Resources.AskSkipSecurity, MsgSeverity.Info)) e.Cancel = true;
        }
    }
}
