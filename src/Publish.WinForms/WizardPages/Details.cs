// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using AeroWizard;
using NanoByte.Common;

namespace ZeroInstall.Publish.WinForms
{
    partial class NewFeedWizard
    {
        private void pageDetails_Initialize(object sender, WizardPageInitEventArgs e)
            => propertyGridCandidate.SelectedObject = _feedBuilder.MainCandidate;

        private void pageDetails_Commit(object sender, WizardPageConfirmEventArgs e)
        {
            if (string.IsNullOrEmpty(_feedBuilder.MainCandidate?.Name) || string.IsNullOrEmpty(_feedBuilder.MainCandidate.Summary) || _feedBuilder.MainCandidate.Version == null)
            {
                e.Cancel = true;
                Msg.Inform(this, labelDetails.Text, MsgSeverity.Warn);
            }
        }
    }
}
