// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using AeroWizard;

namespace ZeroInstall.Publish.WinForms;

partial class NewFeedWizard
{
    /// <summary>The result returned by <see cref="Run"/>.</summary>
    private SignedFeed? _signedFeed;

    private void pageDone_Commit(object sender, WizardPageConfirmEventArgs e)
    {
        if (_feedBuilder.Commands.Count == 0)
            _feedBuilder.GenerateCommands();

        _signedFeed = _feedBuilder.Build();
    }
}
