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

using System.Linq;
using AeroWizard;
using ZeroInstall.Publish.EntryPoints;

namespace ZeroInstall.Publish.WinForms
{
    partial class NewFeedWizard
    {
        private void pageEntryPoint_Initialize(object sender, WizardPageInitEventArgs e)
        {
            listBoxEntryPoint.Items.Clear();
            listBoxEntryPoint.Items.AddRange(_feedBuilder.Candidates.Cast<object>().ToArray());
            listBoxEntryPoint.SelectedItem = _feedBuilder.MainCandidate;
        }

        private void pageEntryPoint_Commit(object sender, WizardPageConfirmEventArgs e)
        {
            _feedBuilder.MainCandidate = listBoxEntryPoint.SelectedItem as Candidate;
            if (_feedBuilder.MainCandidate == null)
            {
                e.Cancel = true;
                return;
            }

            if (_installerCapture.CaptureSession == null)
                _feedBuilder.GenerateCommands();
            else
                _installerCapture.CaptureSession.Finish(); // internally calls _feedBuilder.GenerateCommands()
        }
    }
}
