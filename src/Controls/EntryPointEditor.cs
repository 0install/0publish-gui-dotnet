// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.WinForms.Controls;

/// <summary>
/// Edits <see cref="EntryPoint"/> instances.
/// </summary>
public sealed class EntryPointEditor : SummaryEditor<EntryPoint>
{
    public EntryPointEditor()
    {
        var textBoxNames = new LocalizableTextBox
        {
            Location = new Point(0, 0),
            Size = new Size(Width, 23),
            Multiline = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            HintText = Resources.Names,
            TabIndex = 0
        };
        Bind(textBoxNames, () => Target!.Names);

        SuspendLayout();

        // Shift existing controls down
        TextBoxSummary.Top = textBoxNames.Bottom + 6;
        TextBoxDescription.Top = TextBoxSummary.Bottom + 6;
        EditorControl.Top = TextBoxDescription.Bottom + 6;
        EditorControl.Height = Height - TextBoxDescription.Bottom - 6;

        // High DPI support
        AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

        ResumeLayout();
    }
}
