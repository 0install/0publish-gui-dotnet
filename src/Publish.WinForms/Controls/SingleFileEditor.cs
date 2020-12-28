// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common;
using ZeroInstall.Model;
using ZeroInstall.Publish.WinForms.Properties;

namespace ZeroInstall.Publish.WinForms.Controls
{
    /// <summary>
    /// Edits <see cref="SingleFile"/> instances.
    /// </summary>
    public partial class SingleFileEditor : SingleFileEditorShim
    {
        public SingleFileEditor()
        {
            InitializeComponent();

            RegisterControl(textBoxDestination, PropertyPointer.For(() => Target!.Destination));
            RegisterControl(checkBoxExecutable, PropertyPointer.For(() => Target!.Executable, defaultValue: false));
        }

        private void textBox_TextChanged(object sender, EventArgs e) => ShowUpdateHint(Resources.ManifestDigestChanged);
    }

    /// <summary>
    /// Non-generic base class for <see cref="SingleFileEditor"/>, because WinForms editor cannot handle generics.
    /// </summary>
    public class SingleFileEditorShim : DownloadRetrievalMethodEditor<SingleFile>
    {}
}
