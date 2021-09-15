// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NanoByte.Common;
using NanoByte.Common.Info;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using NanoByte.Common.Undo;
using NDesk.Options;
using Spectre.Console;
using ZeroInstall.Model;
using ZeroInstall.Publish.Cli.Properties;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish.Cli
{
    /// <summary>
    /// Represents a single run of the 0publish tool.
    /// </summary>
    public sealed class PublishCommand : ICommand
    {
        #region Variables
        private readonly ITaskHandler _handler;

        /// <summary>
        /// List of operational modes for the feed editor that can be selected via command-line arguments.
        /// </summary>
        private enum OperationMode
        {
            /// <summary>Modify an existing <see cref="Feed"/> or create a new one.</summary>
            Normal,

            /// <summary>Combine all specified <see cref="Feed"/>s into a single <see cref="Catalog"/> file.</summary>
            Catalog
        }

        /// <summary>
        /// The operational mode for the feed editor.
        /// </summary>
        private OperationMode _mode;

        /// <summary>
        /// The feeds to apply the operation on.
        /// </summary>
        private ICollection<FileInfo> _feeds;

        /// <summary>
        /// The file to store the aggregated <see cref="Catalog"/> data in.
        /// </summary>
        private string? _catalogFile;

        /// <summary>
        /// Download missing archives, calculate manifest digests, etc..
        /// </summary>
        private bool _addMissing;

        /// <summary>
        /// Add XML signature blocks to the feed.
        /// </summary>
        private bool _xmlSign;

        /// <summary>
        /// Remove any existing signatures from the feeds.
        /// </summary>
        private bool _unsign;

        /// <summary>
        /// A key specifier (key ID, fingerprint or any part of a user ID) for the secret key to use to sign the feeds.
        /// </summary>
        /// <remarks>Will use existing key or default key when left at <c>null</c>.</remarks>
        private string? _key;

        /// <summary>
        /// The passphrase used to unlock the <see cref="OpenPgpSecretKey"/>.
        /// </summary>
        private string? _openPgpPassphrase;
        #endregion

        #region Constructor
        /// <summary>
        /// Parses command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments to be parsed.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        /// <exception cref="OperationCanceledException">The user asked to see help information, version information, etc..</exception>
        /// <exception cref="OptionException"><paramref name="args"/> contains unknown options.</exception>
        public PublishCommand(IEnumerable<string> args, ITaskHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));

            var additionalArgs = BuildOptions().Parse(args ?? throw new ArgumentNullException(nameof(args)));
            try
            {
                _feeds = Paths.ResolveFiles(additionalArgs, "*.xml");
            }
            #region Error handling
            catch (FileNotFoundException ex)
            {
                // Report as an invalid command-line argument
                throw new OptionException(ex.Message, ex.FileName);
            }
            #endregion
        }

        [SuppressMessage("ReSharper", "LocalizableElement")]
        private OptionSet BuildOptions()
        {
            var options = new OptionSet
            {
                // Version information
                {
                    "V|version", () => Resources.OptionVersion, _ =>
                    {
                        Console.WriteLine(AppInfo.Current.Name + @" " + AppInfo.Current.Version + Environment.NewLine + AppInfo.Current.Copyright + Environment.NewLine + Resources.LicenseInfo);
                        throw new OperationCanceledException(); // Don't handle any of the other arguments
                    }
                },

                // Mode selection
                {
                    "catalog=", () => Resources.OptionCatalog, delegate(string catalogFile)
                    {
                        if (string.IsNullOrEmpty(catalogFile)) return;
                        _mode = OperationMode.Catalog;
                        _catalogFile = Path.GetFullPath(catalogFile);
                    }
                },

                // Modifications
                {"add-missing", () => Resources.OptionAddMissing, _ => _addMissing = true},

                // Signatures
                {"x|xmlsign", () => Resources.OptionXmlSign, _ => _xmlSign = true},
                {"u|unsign", () => Resources.OptionUnsign, _ => _unsign = true},
                {"k|key=", () => Resources.OptionKey, user => _key = user},
                {"gpg-passphrase=", () => Resources.OptionGnuPGPassphrase, passphrase => _openPgpPassphrase = passphrase}
            };
            options.Add("h|help|?", () => Resources.OptionHelp, _ =>
            {
                Console.WriteLine(Resources.Usage);
                Console.WriteLine("\t0publish [OPTIONS] feed.xml");
                Console.WriteLine("\t0publish capture --help");
                Console.WriteLine();
                Console.WriteLine(Resources.Options);
                options.WriteOptionDescriptions(Console.Out);

                // Don't handle any of the other arguments
                throw new OperationCanceledException();
            });
            return options;
        }
        #endregion

        #region Execute
        /// <inheritdoc/>
        public ExitCode Execute()
        {
            switch (_mode)
            {
                case OperationMode.Normal:
                    if (_feeds.Count == 0)
                    {
                        Log.Error(string.Format(Resources.MissingArguments, "0publish"));
                        return ExitCode.InvalidArguments;
                    }

                    foreach (var feedEditing in _feeds.Select(file => FeedEditing.Load(file.FullName)))
                    {
                        HandleModify(feedEditing);
                        SaveFeed(feedEditing);
                    }
                    return ExitCode.OK;

                case OperationMode.Catalog:
                    // Default to using all XML files in the current directory
                    if (_feeds.Count == 0)
                        _feeds = Paths.ResolveFiles(new[] {Environment.CurrentDirectory}, "*.xml");

                    var catalog = new Catalog();
                    foreach (var feed in _feeds.Select(feedFile => XmlStorage.LoadXml<Feed>(feedFile.FullName)))
                    {
                        feed.Strip();
                        catalog.Feeds.Add(feed);
                    }
                    if (catalog.Feeds.Count == 0) throw new FileNotFoundException(Resources.NoFeedFilesFound);

                    SaveCatalog(catalog);
                    return ExitCode.OK;

                default:
                    Log.Error(string.Format(Resources.UnknownMode, "0publish"));
                    return ExitCode.InvalidArguments;
            }
        }
        #endregion

        #region Storage
        /// <summary>
        /// Saves a feed.
        /// </summary>
        /// <exception cref="IOException">A file could not be read or written or the GnuPG could not be launched or the feed file could not be read or written.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a feed file is not permitted.</exception>
        /// <exception cref="KeyNotFoundException">An OpenPGP key could not be found.</exception>
        private void SaveFeed(FeedEditing feedEditing)
        {
            if (_unsign)
            {
                // Remove any existing signatures
                feedEditing.SignedFeed.SecretKey = null;
            }
            else
            {
                var openPgp = OpenPgp.Signing();
                if (_xmlSign)
                { // Signing explicitly requested
                    if (feedEditing.SignedFeed.SecretKey == null)
                    { // No previous signature
                        // Use user-specified key or default key
                        feedEditing.SignedFeed.SecretKey = openPgp.GetSecretKey(_key);
                    }
                    else
                    { // Existing signature
                        if (!string.IsNullOrEmpty(_key)) // Use new user-specified key
                            feedEditing.SignedFeed.SecretKey = openPgp.GetSecretKey(_key);
                        //else resign implied
                    }
                }
                //else resign implied
            }

            // If no signing or unsigning was explicitly requested and the content did not change
            // there is no need to overwrite (and potential resign) the file
            if (!_xmlSign && !_unsign && !feedEditing.UnsavedChanges) return;

            PromptPassphrase(
                () => feedEditing.SignedFeed.Save(feedEditing.Path!, _openPgpPassphrase),
                feedEditing.SignedFeed.SecretKey);
        }

        /// <summary>
        /// Saves a catalog.
        /// </summary>
        /// <param name="catalog">The catalog to save.</param>
        /// <exception cref="IOException">A file could not be read or written or the GnuPG could not be launched or the catalog file could not be written.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a catalog file is not permitted.</exception>
        /// <exception cref="KeyNotFoundException">An OpenPGP key could not be found.</exception>
        private void SaveCatalog(Catalog catalog)
        {
            if (_xmlSign)
            {
                var openPgp = OpenPgp.Signing();
                var signedCatalog = new SignedCatalog(catalog, openPgp.GetSecretKey(_key));

                PromptPassphrase(
                    () => signedCatalog.Save(_catalogFile!, _openPgpPassphrase),
                    signedCatalog.SecretKey);
            }
            else catalog.SaveXml(_catalogFile!);
        }

        /// <summary>
        /// Runs the specified <paramref name="action"/> and prompts for the <paramref name="secretKey"/> if <see cref="WrongPassphraseException"/> is thrown.
        /// </summary>
        /// <exception cref="OperationCanceledException">The user cancelled the passphrase entry.</exception>
        private void PromptPassphrase(Action action, OpenPgpSecretKey? secretKey)
        {
            while (true)
            {
                try
                {
                    action();
                    return; // Exit loop if passphrase is correct
                }
                catch (WrongPassphraseException ex) when (secretKey != null)
                {
                    // Only print error if a passphrase was actually entered
                    if (_openPgpPassphrase != null) Log.Error(ex);

                    // Ask for passphrase to unlock secret key if we were unable to save without it
                    var task = Task.Run(() => AnsiCli.Error.Prompt(new TextPrompt<string>(string.Format(Resources.AskForPassphrase, secretKey.UserID)).Secret()));
                    task.Wait(_handler.CancellationToken);
                    _openPgpPassphrase = task.Result;
                }
            }
        }
        #endregion

        #region Modify
        /// <summary>
        /// Applies user-selected modifications to a feed.
        /// </summary>
        /// <param name="feedEditing">The feed to modify.</param>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="IOException">There is a problem access a temporary file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read or write access to a temporary file is not permitted.</exception>
        /// <exception cref="DigestMismatchException">An existing digest does not match the newly calculated one.</exception>
        private void HandleModify(FeedEditing feedEditing)
        {
            if (_addMissing)
            {
                var feed = feedEditing.SignedFeed.Feed;
                feed.ResolveInternalReferences();

                // Enable Recipe steps to call out to external Fetcher
                using (FetchHandle.Register(impl =>
                {
                    _handler.RunTask(new ExternalFetch(impl));
                    return ImplementationStores.Default().GetPath(impl);
                }))
                    AddMissing(feed.Elements, feedEditing);
            }
        }

        private void AddMissing(IEnumerable<Element> elements, ICommandExecutor executor)
        {
            foreach (var element in elements)
            {
                switch (element)
                {
                    case Implementation implementation:
                        implementation.AddMissing(_handler, executor);
                        break;

                    case Group group:
                        AddMissing(group.Elements, executor); // recursion
                        break;
                }
            }
        }
        #endregion
    }
}
