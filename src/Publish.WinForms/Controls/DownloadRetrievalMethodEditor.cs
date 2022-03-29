// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.WinForms.Controls;

/// <summary>
/// A common base for <see cref="DownloadRetrievalMethod"/> editors.
/// </summary>
/// <typeparam name="T">The type of <see cref="DownloadRetrievalMethod"/> to edit.</typeparam>
public abstract class DownloadRetrievalMethodEditor<T> : RetrievalMethodEditor<T>
    where T : DownloadRetrievalMethod
{
    #region Constructor
    protected DownloadRetrievalMethodEditor()
    {
        Controls.Add(new Label
        {
            Location = new Point(0, 3),
            AutoSize = true,
            TabIndex = 0,
            Text = Resources.SourceUrl + @":"
        });

        var textBoxHref = new UriTextBox
        {
            Location = new Point(77, 0),
            Size = new Size(73, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 1,
            HintText = Resources.RequiredUrl,
            AllowRelative = true
        };
        textBoxHref.TextChanged += delegate { ShowUpdateHint(Resources.ManifestDigestChanged); };
        Bind(textBoxHref, PropertyPointer.For(() => Target!.Href));

        Controls.Add(new Label
        {
            Location = new Point(0, 29),
            AutoSize = true,
            TabIndex = 2,
            Text = Resources.FileSize + @":"
        });

        var textBoxSize = new HintTextBox
        {
            Location = new Point(77, 26),
            Size = new Size(73, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TabIndex = 3,
            HintText = Resources.RequiredBytes
        };
        Bind(textBoxSize, PropertyPointer.For(() => Target!.Size, defaultValue: 0).ToStringPointer());
    }
    #endregion

    /// <inheritdoc/>
    protected override void UpdateHint()
    {
        if (Target!.Size == 0) ShowUpdateHint(Resources.SizeMissing);
        else base.UpdateHint();
    }
}
