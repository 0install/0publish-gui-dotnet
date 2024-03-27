// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.WinForms.Controls;

/// <summary>
/// Edits <see cref="Feed"/> instances.
/// </summary>
public partial class FeedEditor : FeedEditorShim
{
    public FeedEditor()
    {
        InitializeComponent();

        Bind(textBoxName, PropertyPointer.For(() => Target!.Name));
        Bind(textBoxUri, PropertyPointer.For(() => (Uri)Target!.Uri, value => Target!.Uri = (value == null) ? null : new FeedUri(value)));
        Bind(textBoxDescription, () => Target!.Descriptions);
        Bind(textBoxSummary, () => Target!.Summaries);
        Bind(textBoxHomepage, PropertyPointer.For(() => Target!.Homepage));
        Bind(checkBoxNeedTerminal, PropertyPointer.For(() => Target!.NeedsTerminal));
        Bind(comboBoxMinimumZeroInstallVersion, PropertyPointer.For(() => Target!.MinInjectorVersionString));
    }
}

/// <summary>
/// Non-generic base class for <see cref="FeedEditor"/>, because WinForms editor cannot handle generics.
/// </summary>
public class FeedEditorShim : NodeEditorBase<Feed>;
