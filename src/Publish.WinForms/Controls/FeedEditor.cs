// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common;
using NanoByte.StructureEditor.WinForms;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.WinForms.Controls
{
    /// <summary>
    /// Edits <see cref="Feed"/> instances.
    /// </summary>
    public partial class FeedEditor : FeedEditorShim
    {
        public FeedEditor()
        {
            InitializeComponent();

            RegisterControl(textBoxName, PropertyPointer.For(() => Target.Name));
            RegisterControl(textBoxUri, new PropertyPointer<Uri>(() => Target.Uri, value => Target.Uri = (value == null) ? null : new FeedUri(value)));
            RegisterControl(textBoxDescription, () => Target.Descriptions);
            RegisterControl(textBoxSummary, () => Target.Summaries);
            RegisterControl(textBoxHomepage, PropertyPointer.For(() => Target.Homepage));
            RegisterControl(checkBoxNeedTerminal, PropertyPointer.For(() => Target.NeedsTerminal));
            RegisterControl(comboBoxMinimumZeroInstallVersion, PropertyPointer.For(() => Target.MinInjectorVersionString));
        }
    }

    /// <summary>
    /// Non-generic base class for <see cref="FeedEditor"/>, because WinForms editor cannot handle generics.
    /// </summary>
    public class FeedEditorShim : NodeEditorBase<Feed>
    {}
}
