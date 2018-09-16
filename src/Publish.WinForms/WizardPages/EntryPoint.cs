// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

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

            _installerCapture.CaptureSession?.Finish();
        }
    }
}
