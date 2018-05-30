// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Linq;
using NanoByte.Common;
using ZeroInstall.Publish.WinForms.Properties;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.WinForms.Controls
{
    /// <summary>
    /// Edits <see cref="Archive"/> instances.
    /// </summary>
    public partial class ArchiveEditor : ArchiveEditorShim
    {
        public ArchiveEditor()
        {
            InitializeComponent();

            RegisterControl(comboBoxMimeType, new PropertyPointer<string>(() => Target.MimeType, value => Target.MimeType = value));
            RegisterControl(textBoxExtract, new PropertyPointer<string>(() => Target.Extract, value => Target.Extract = value));
            RegisterControl(textBoxDestination, new PropertyPointer<string>(() => Target.Destination, value => Target.Destination = value));

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
}
