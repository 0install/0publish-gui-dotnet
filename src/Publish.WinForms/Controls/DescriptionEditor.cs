// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Drawing;
using System.Windows.Forms;
using NanoByte.StructureEditor.WinForms;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.WinForms.Controls
{
    /// <summary>
    /// A common base for <see cref="IDependencyContainer"/> editors.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDependencyContainer"/> to edit.</typeparam>
    public class DescriptionEditor<T> : EditorControlBase<T>
        where T : class, IDescriptionContainer
    {
        protected readonly LocalizableTextBox TextBoxDescription;
        protected readonly PropertyGridEditor<T> EditorControl;

        public DescriptionEditor()
        {
            TextBoxDescription = new LocalizableTextBox
            {
                Location = new Point(0, 0),
                Size = new Size(Width, 76),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TabIndex = 1,
                HintText = "Description"
            };
            RegisterControl(TextBoxDescription, () => Target.Descriptions);

            EditorControl = new PropertyGridEditor<T>
            {
                Location = new Point(0, TextBoxDescription.Bottom + 6),
                Size = new Size(Width, Height - TextBoxDescription.Bottom - 6),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                TabIndex = 2
            };
            RegisterControl(EditorControl, () => Target);
        }
    }
}
