// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Drawing;
using System.Windows.Forms;
using NanoByte.StructureEditor.WinForms;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.WinForms.Controls
{
    /// <summary>
    /// A common base for <see cref="ISummaryContainer"/> editors.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="ISummaryContainer"/> to edit.</typeparam>
    public class SummaryEditor<T> : DescriptionEditor<T>
        where T : class, ISummaryContainer
    {
        protected readonly LocalizableTextBox TextBoxSummary;

        public SummaryEditor()
        {
            TextBoxSummary = new LocalizableTextBox
            {
                Location = new Point(0, 0),
                Size = new Size(Width, 23),
                Multiline = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                HintText = "Summary",
                TabIndex = 0
            };
            RegisterControl(TextBoxSummary, () => Target.Summaries);

            // Shift existing controls down
            SuspendLayout();
            TextBoxDescription.Top = TextBoxSummary.Bottom + 6;
            EditorControl.Top = TextBoxDescription.Bottom + 6;
            EditorControl.Height = Height - TextBoxDescription.Bottom - 6;
            ResumeLayout();
        }
    }
}
