// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish.WinForms;

/// <summary>
/// A dialog for signing multiple <see cref="Feed"/>s with a single <see cref="OpenPgpSecretKey"/>.
/// </summary>
public partial class MassSignForm : OKCancelDialog
{
    #region Variables
    private readonly IEnumerable<FileInfo> _files;
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new mass signing dialog.
    /// </summary>
    /// <param name="files">The <see cref="Feed"/> files to be signed.</param>
    private MassSignForm(IEnumerable<FileInfo> files)
    {
        InitializeComponent();

        _files = files;
        foreach (var path in _files)
            listBoxFiles.Items.Add(path);
    }

    private void MassSignDialog_Load(object sender, EventArgs e)
    {
        comboBoxSecretKey.Items.Add("");
        foreach (var secretKey in OpenPgp.Signing().ListSecretKeys())
            comboBoxSecretKey.Items.Add(secretKey);
    }
    #endregion

    #region Static access
    /// <summary>
    /// Displays a dialog allowing the user to sign a set of <see cref="Feed"/>s.
    /// </summary>
    /// <param name="files">The <see cref="Feed"/> files to be signed.</param>
    public static void Show(IEnumerable<FileInfo> files)
    {
        #region Sanity checks
        if (files == null) throw new ArgumentNullException(nameof(files));
        #endregion

        using var dialog = new MassSignForm(files);
        dialog.ShowDialog();
    }
    #endregion

    //--------------------//

    #region Event handlers
    private void comboBoxSecretKey_SelectedValueChanged(object sender, EventArgs e)
        => buttonOK.Enabled = (comboBoxSecretKey.SelectedItem is OpenPgpSecretKey);

    private void buttonOK_Click(object sender, EventArgs e)
    {
        try
        {
            SignFiles(comboBoxSecretKey.SelectedItem as OpenPgpSecretKey, textPassword.Text);
        }
        #region Sanity checks
        catch (OperationCanceledException)
        {}
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            Log.Error("Failed to mass sign feeds", ex);
            Msg.Inform(this, ex.GetMessageWithInner(), MsgSeverity.Error);
        }
        #endregion
    }

    /// <summary>
    /// Signs a number of <see cref="Feed"/>s with a single <see cref="OpenPgpSecretKey"/>.
    /// </summary>
    /// <param name="secretKey">The private key to use for signing the files.</param>
    /// <param name="passphrase">The passphrase to use to unlock the key.</param>
    /// <exception cref="IOException">The feed file could not be read or written.</exception>
    /// <exception cref="UnauthorizedAccessException">Read or write access to the feed file is not permitted.</exception>
    private void SignFiles(OpenPgpSecretKey? secretKey, string passphrase)
    {
        var task = ForEachTask.Create(Resources.SigningFeeds, _files, file =>
        {
            SignedFeed signedFeed;
            try
            {
                signedFeed = SignedFeed.Load(file.FullName);
            }
            #region Error handling
            catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidDataException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion

            signedFeed.SecretKey = secretKey;
            try
            {
                signedFeed.Save(file.FullName, passphrase);
            }
            #region Error handling
            catch (Exception ex) when (ex is UnauthorizedAccessException or KeyNotFoundException or WrongPassphraseException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new IOException(ex.Message, ex);
            }
            #endregion
        });
        using (var handler = new DialogTaskHandler(this)) handler.RunTask(task);
        Msg.Inform(this, Resources.SignedFeeds, MsgSeverity.Info);
    }
    #endregion
}
