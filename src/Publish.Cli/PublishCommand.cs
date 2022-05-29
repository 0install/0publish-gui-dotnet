// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Info;
using NanoByte.Common.Undo;
using Spectre.Console;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish.Cli;

/// <summary>
/// Represents a single run of the 0publish tool.
/// </summary>
public sealed class PublishCommand : ICommand
{
    private readonly ITaskHandler _handler;

    /// <summary>The feeds to apply the operation on.</summary>
    private ICollection<FileInfo> _feeds;

    /// <summary>The file to store the aggregated <see cref="Catalog"/> data in.</summary>
    private string? _catalogFile;

    /// <summary>Download missing archives, calculate manifest digests, etc..</summary>
    private bool _addMissing;

    /// <summary>Add XML signature blocks to the feed.</summary>
    private bool _xmlSign;

    /// <summary>Remove any existing signatures from the feeds.</summary>
    private bool _unsign;

    /// <summary>A key specifier (key ID, fingerprint or any part of a user ID) for the secret key to use to sign the feeds.</summary>
    private string? _key;

    /// <summary>The passphrase used to unlock the <see cref="OpenPgpSecretKey"/>.</summary>
    private string? _openPgpPassphrase;

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

            // Modes
            {"catalog=", () => Resources.OptionCatalog, path => _catalogFile = Path.GetFullPath(path)},
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

    /// <inheritdoc/>
    public ExitCode Execute()
    {
        if (!string.IsNullOrEmpty(_catalogFile))
        {
            // Default to using all XML files in the current directory
            if (_feeds.Count == 0)
                _feeds = Paths.ResolveFiles(new[] {Environment.CurrentDirectory}, "*.xml");

            GenerateCatalog();
            return ExitCode.OK;
        }

        if (_feeds.Count == 0)
        {
            Log.Error(string.Format(Resources.MissingArguments, "0publish --help"));
            return ExitCode.InvalidArguments;
        }

        foreach (var file in _feeds)
        {
            var feedEditing = FeedEditing.Load(file.FullName);
            var feed = feedEditing.SignedFeed.Feed;
            feed.ResolveInternalReferences();

            if (_addMissing) AddMissing(feed.Implementations, feedEditing);

            SaveFeed(feedEditing);
        }

        return ExitCode.OK;
    }

    private void GenerateCatalog()
    {
        var catalog = new Catalog();
        foreach (var feed in _feeds.Select(feedFile => XmlStorage.LoadXml<Feed>(feedFile.FullName)))
        {
            feed.Strip();
            catalog.Feeds.Add(feed);
        }

        if (catalog.Feeds.Count == 0) throw new FileNotFoundException(Resources.NoFeedFilesFound);

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

    private void AddMissing(IEnumerable<Implementation> implementations, ICommandExecutor executor)
    {
        executor = new ConcurrentCommandExecutor(executor);

        try
        {
            implementations.AsParallel()
                           .WithDegreeOfParallelism(Config.LoadSafe().MaxParallelDownloads)
                           .ForAll(implementation => implementation.SetMissing(executor, _handler));
        }
        catch (AggregateException ex)
        {
            throw ex.RethrowLastInner();
        }
    }

    private void SaveFeed(FeedEditing feedEditing)
    {
        if (!feedEditing.Path!.EndsWith(".xml.template")
         && !feedEditing.IsValid(out string problem))
            Log.Warn(problem);

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
                if (_openPgpPassphrase != null) Log.Error(ex.Message, ex);

                // Ask for passphrase to unlock secret key if we were unable to save without it
                _openPgpPassphrase = AnsiCli.Prompt(new TextPrompt<string>(string.Format(Resources.AskForPassphrase, secretKey.UserID)).Secret(), _handler.CancellationToken);
            }
        }
    }
}
