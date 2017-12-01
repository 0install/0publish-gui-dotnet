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

using AeroWizard;
using NanoByte.Common;

namespace ZeroInstall.Publish.WinForms
{
    partial class NewFeedWizard
    {
        private void pageDetails_Initialize(object sender, WizardPageInitEventArgs e)
        {
            propertyGridCandidate.SelectedObject = _feedBuilder.MainCandidate;
        }

        private void pageDetails_Commit(object sender, WizardPageConfirmEventArgs e)
        {
            if (string.IsNullOrEmpty(_feedBuilder.MainCandidate.Name) || string.IsNullOrEmpty(_feedBuilder.MainCandidate.Summary) || _feedBuilder.MainCandidate.Version == null)
            {
                e.Cancel = true;
                Msg.Inform(this, labelDetails.Text, MsgSeverity.Warn);
            }
        }
    }
}
