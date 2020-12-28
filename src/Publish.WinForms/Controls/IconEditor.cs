// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NanoByte.Common;
using NanoByte.StructureEditor.WinForms;
using ZeroInstall.Publish.WinForms.Properties;
using Icon = ZeroInstall.Model.Icon;

namespace ZeroInstall.Publish.WinForms.Controls
{
    /// <summary>
    /// Edits <see cref="Model.Icon"/> instances.
    /// </summary>
    public partial class IconEditor : IconEditorShim
    {
        #region Constructor
        public IconEditor()
        {
            InitializeComponent();
            Bind(textBoxHref, PropertyPointer.For(() => Target!.Href));
            Bind(comboBoxMimeType, PropertyPointer.For(() => Target!.MimeType));

            // ReSharper disable once CoVariantArrayConversion
            comboBoxMimeType.Items.AddRange(Icon.KnownMimeTypes);
        }
        #endregion

        #region Preview
        private async void buttonPreview_Click(object sender, EventArgs e)
        {
            if (!textBoxHref.IsValid || textBoxHref.Uri == null) return;

            pictureBoxPreview.Image = null;
            ShowStatusMessage(SystemColors.ControlText, Resources.DownloadingPeviewImage);

            try
            {
                var icon = await Task.Run(() => GetImageFromUrl(textBoxHref.Uri));
                if (icon.RawFormat.Equals(ImageFormat.Png))
                {
                    if (string.IsNullOrEmpty(comboBoxMimeType.Text)) comboBoxMimeType.Text = Icon.MimeTypePng;
                    else if (comboBoxMimeType.Text != Icon.MimeTypePng) ShowStatusMessage(Color.Red, string.Format(Resources.WrongMimeType, Icon.MimeTypePng));
                    else ShowStatusMessage(Color.Green, "OK");
                }
                else if (icon.RawFormat.Equals(ImageFormat.Icon))
                {
                    if (string.IsNullOrEmpty(comboBoxMimeType.Text)) comboBoxMimeType.Text = Icon.MimeTypeIco;
                    else if (comboBoxMimeType.Text != Icon.MimeTypeIco) ShowStatusMessage(Color.Red, string.Format(Resources.WrongMimeType, Icon.MimeTypeIco));
                    else ShowStatusMessage(Color.Green, "OK");
                }
                else ShowStatusMessage(Color.Red, Resources.ImageFormatNotSupported);

                pictureBoxPreview.Image = icon;
            }
            catch (Exception ex)
            {
                ShowStatusMessage(Color.Red, ex.Message);
            }
        }

        /// <summary>
        /// Downloads an <see cref="Image"/> from a specific url.
        /// </summary>
        /// <param name="url">To an <see cref="Image"/>.</param>
        /// <returns>The downloaded <see cref="Image"/>.</returns>
        /// <exception cref="WebException">The image file could not be downloaded.</exception>
        /// <exception cref="InvalidDataException">The downloaded data is not a valid image files.</exception>
        private static Image GetImageFromUrl(Uri url)
        {
            try
            {
                using var imageStream = WebRequest.Create(url).GetResponse().GetResponseStream();
                return Image.FromStream(imageStream);
            }
            #region Error handling
            catch (ArgumentException ex)
            {
                // Wrap exception since only certain exception types are allowed
                throw new InvalidDataException(ex.Message, ex);
            }
            #endregion
        }

        private void ShowStatusMessage(Color color, string message)
        {
            lableStatus.Text = message;
            lableStatus.ForeColor = color;
            lableStatus.Visible = true;
        }
        #endregion
    }

    /// <summary>
    /// Non-generic base class for <see cref="IconEditor"/>, because WinForms editor cannot handle generics.
    /// </summary>
    public class IconEditorShim : NodeEditorBase<Icon>
    {}
}
