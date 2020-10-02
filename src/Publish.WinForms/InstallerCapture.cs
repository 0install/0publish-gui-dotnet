// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Net;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Publish.Capture;
using ZeroInstall.Publish.WinForms.Properties;

namespace ZeroInstall.Publish.WinForms
{
    /// <summary>
    /// Holds state shared between Wizard pages when capturing an installer.
    /// </summary>
    internal sealed class InstallerCapture : IDisposable
    {
        /// <summary>
        /// Holds the <see cref="CaptureSession"/> created at the start of the process.
        /// </summary>
        public CaptureSession? CaptureSession { get; set; }

        private Uri? _url;

        private string? _localPath;

        /// <summary>
        /// Sets the installer source to a pre-existing local file.
        /// </summary>
        /// <param name="url">The URL the file was originally downloaded from.</param>
        /// <param name="path">The local path of the file.</param>
        /// <remarks>Use either this or <see cref="Download"/>.</remarks>
        public void SetLocal(Uri url, string path)
        {
            _url = url;
            _localPath = path;
        }

        private TemporaryDirectory? _tempDir;

        /// <summary>
        /// Downloads the installer from the web to a temporary file.
        /// </summary>
        /// <param name="url">The URL of the file to download.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="IOException">A downloaded file could not be written to the disk.</exception>
        /// <exception cref="UnauthorizedAccessException">An operation failed due to insufficient rights.</exception>
        /// <remarks>Use either this or <see cref="SetLocal"/>.</remarks>
        public void Download(Uri url, ITaskHandler handler)
        {
            _url = url;

            _tempDir?.Dispose();
            _tempDir = new TemporaryDirectory("0publish");

            try
            {
                _localPath = Path.Combine(_tempDir, url.GetLocalFileName());
                handler.RunTask(new DownloadFile(url, _localPath));
            }
            #region Error handling
            catch (Exception)
            {
                _tempDir.Dispose();
                _tempDir = null;
                _url = null;
                _localPath = null;
                throw;
            }
            #endregion
        }

        /// <summary>
        /// Disposes any temporary files created by <see cref="Download"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_tempDir", Justification = "False positive")]
        public void Dispose() => _tempDir?.Dispose();

        /// <summary>
        /// Runs the installer and waits for it to exit.
        /// </summary>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        public void RunInstaller(ITaskHandler handler)
        {
            if (string.IsNullOrEmpty(_localPath)) throw new InvalidOperationException();

            var process = ProcessUtils.Start(_localPath);
            if (process == null) return;
            handler.RunTask(new SimpleTask(Resources.WaitingForInstaller, () => process.WaitForExit()));
        }

        /// <summary>
        /// Tries extracting the installer as an <see cref="Archive"/>.
        /// </summary>
        /// <param name="feedBuilder">All collected data is stored into this builder.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="IOException">The installer could not be extracted as an archive.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        public void ExtractInstallerAsArchive(FeedBuilder feedBuilder, ITaskHandler handler)
        {
            if (string.IsNullOrEmpty(_localPath) || _url == null) throw new InvalidOperationException();

            var archive = new Archive
            {
                Href = _url,
                MimeType = _localPath.EndsWith(@".msi")
                    ? Archive.MimeTypeMsi
                    // 7zip's extraction logic can handle a number of self-extracting formats
                    : Archive.MimeType7Z
            };
            feedBuilder.RetrievalMethod = archive;
            feedBuilder.TemporaryDirectory = archive.LocalApply(_localPath, handler);
        }
    }
}
