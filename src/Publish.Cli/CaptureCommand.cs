// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Publish.Capture;

namespace ZeroInstall.Publish.Cli;

/// <summary>
/// Captures snapshots of the system state and compares them to generate a feed.
/// </summary>
internal class CaptureCommand : ICommand
{
    private readonly ITaskHandler _handler;

    private readonly List<string> _additionalArgs;

    /// <summary>
    /// Parses command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to be parsed.</param>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <exception cref="OperationCanceledException">The user asked to see help information, version information, etc..</exception>
    /// <exception cref="OptionException"><paramref name="args"/> contains unknown options.</exception>
    public CaptureCommand(IEnumerable<string> args, ITaskHandler handler)
    {
        _handler = handler;
        _additionalArgs = BuildOptions().Parse(args);
    }

    #region Options
    /// <summary>Overwrite existing files.</summary>
    private bool _force;

    /// <summary>The directory the application to be captured is installed in. <c>null</c> to auto-detect.</summary>
    private string? _installationDirectory;

    /// <summary>The relative path to the main EXE of the application to be captured. <c>null</c> to auto-detect.</summary>
    private string? _mainExe;

    /// <summary>The path of the ZIP file to create from the installation directory. <c>null</c> to create no ZIP archive.</summary>
    private string? _zipFile;

    private OptionSet BuildOptions()
    {
        var options = new OptionSet
        {
            {"f|force", () => Resources.OptionForce, _ => _force = true},
            {
                "installation-dir=", () => Resources.OptionInstallationDirectory, value =>
                {
                    try
                    {
                        _installationDirectory = Path.GetFullPath(value);
                    }
                    #region Error handling
                    catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
                    {
                        // Wrap exception since only certain exception types are allowed
                        throw new OptionException(ex.Message, "installation-dir");
                    }
                    #endregion
                }
            },
            {"main-exe=", () => Resources.OptionMainExe, value => _mainExe = value},
            {"collect-files=", () => Resources.OptionCollectFiles, value => _zipFile = value}
        };

        options.Add("h|help|?", () => Resources.OptionHelp, _ =>
        {
            Console.WriteLine(Resources.DescriptionCapture);
            Console.WriteLine();
            Console.WriteLine(Resources.Usage);
            Console.WriteLine(@"0publish capture start SNAPSHOT-FILE [--force]");
            Console.WriteLine(@"0publish capture finish SNAPSHOT-FILE FEED-FILE [--force]");
            Console.WriteLine();
            Console.WriteLine(Resources.Options);
            options.WriteOptionDescriptions(Console.Out);

            // Don't handle any of the other arguments
            throw new OperationCanceledException();
        });

        return options;
    }
    #endregion

    /// <inheritdoc/>
    public void Execute()
    {
        switch (_additionalArgs)
        {
            case ["start", var snapshotFile]:
                Start(snapshotFile);
                break;
            case ["finish", var snapshotFile, var feedFile]:
                Finish(snapshotFile, feedFile);
                break;
            default:
                throw new OptionException(string.Format(Resources.MissingArguments, "0publish capture --help"), "");
        }
    }

    private void Start(string snapshotFile)
    {
        HandleFileAlreadyExists(snapshotFile);

        var session = CaptureSession.Start(new FeedBuilder());
        session.Save(snapshotFile);
    }

    private void Finish(string snapshotFile, string feedFile)
    {
        HandleFileAlreadyExists(feedFile);

        var feedBuilder = new FeedBuilder();
        var session = CaptureSession.Load(snapshotFile, feedBuilder);

        session.InstallationDir = _installationDirectory;
        try
        {
            session.Diff(_handler);
        }
        #region Error handling
        catch (InvalidOperationException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new InvalidDataException(ex.Message, ex);
        }
        #endregion

        feedBuilder.MainCandidate = string.IsNullOrEmpty(_mainExe)
            ? feedBuilder.Candidates.FirstOrDefault()
            : feedBuilder.Candidates.FirstOrDefault(x => StringUtils.EqualsIgnoreCase(x.RelativePath.ToNativePath(), _mainExe));
        session.Finish();

        if (!string.IsNullOrEmpty(_zipFile))
        {
            HandleFileAlreadyExists(_zipFile);

            var relativeUri = new Uri(Path.GetFullPath(feedFile)).MakeRelativeUri(new Uri(Path.GetFullPath(_zipFile!)));
            session.CollectFiles(_zipFile, relativeUri, _handler);
            Log.Warn("If you wish to upload this feed and ZIP archive, make sure to turn the <archive>'s relative href into an absolute one.");
        }

        feedBuilder.Build().Save(feedFile);
    }

    private void HandleFileAlreadyExists(string path)
    {
        if (File.Exists(path) && !_force)
            throw new IOException(string.Format(Resources.FileAlreadyExists, Path.GetFullPath(path)));
    }
}
