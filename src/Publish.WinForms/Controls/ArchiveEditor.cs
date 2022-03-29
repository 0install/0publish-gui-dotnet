// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Linq;
using NanoByte.Common;
using ZeroInstall.Model;
using ZeroInstall.Publish.WinForms.Properties;

namespace ZeroInstall.Publish.WinForms.Controls;

/// <summary>
/// Edits <see cref="Archive"/> instances.
/// </summary>
public partial class ArchiveEditor : ArchiveEditorShim
{
    public ArchiveEditor()
    {
        InitializeComponent();

        Bind(comboBoxMimeType, PropertyPointer.For(() => Target!.MimeType));
        Bind(textBoxExtract, PropertyPointer.For(() => Target!.Extract));
        Bind(textBoxDestination, PropertyPointer.For(() => Target!.Destination));

        // ReSharper disable once CoVariantArrayConversion
        comboBoxMimeType.Items.AddRange(Archive.KnownMimeTypes.Cast<object>().ToArray());
    }

    private void textBox_TextChanged(object sender, EventArgs e) => ShowUpdateHint(Resources.ManifestDigestChanged);
}

/// <summary>
/// Non-generic base class for <see cref="ArchiveEditor"/>, because WinForms editor cannot handle generics.
/// </summary>
public class ArchiveEditorShim : DownloadRetrievalMethodEditor<Archive>
{}
