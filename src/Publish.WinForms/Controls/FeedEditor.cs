// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common;
using NanoByte.Common.Controls;
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

            RegisterControl(textBoxName, new PropertyPointer<string>(() => Target.Name, value => Target.Name = value));
            RegisterControl(textBoxUri, new PropertyPointer<Uri>(() => Target.Uri, value => Target.Uri = (value == null) ? null : new FeedUri(value)));
            RegisterControl(textBoxDescription, () => Target.Descriptions);
            RegisterControl(textBoxSummary, () => Target.Summaries);
            RegisterControl(textBoxHomepage, new PropertyPointer<Uri>(() => Target.Homepage, value => Target.Homepage = value));
            RegisterControl(checkBoxNeedTerminal, new PropertyPointer<bool>(() => Target.NeedsTerminal, value => Target.NeedsTerminal = value));
            RegisterControl(comboBoxMinimumZeroInstallVersion, new PropertyPointer<string>(() => Target.MinInjectorVersionString, value => Target.MinInjectorVersionString = value));
        }
    }

    /// <summary>
    /// Non-generic base class for <see cref="FeedEditor"/>, because WinForms editor cannot handle generics.
    /// </summary>
    public class FeedEditorShim : EditorControlBase<Feed>
    {}
}
