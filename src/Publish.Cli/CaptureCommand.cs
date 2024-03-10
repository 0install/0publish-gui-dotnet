// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Info;
using ZeroInstall.Publish.Capture;

namespace ZeroInstall.Publish.Cli;

/// <summary>
/// Captures snapshots of the system state and compares them to generate a feed.
/// </summary>
internal class CaptureCommand : ICommand
{
    #region Parse
    private readonly ITaskHandler _handler;

    /// <summary>Ignore warnings and perform the operation anyway.</summary>
    private bool _force;

    /// <summary>The directory the application to be captured is installed in; <c>null</c> to create no ZIP archive.</summary>
    private string? _installationDirectory;

    /// <summary>The relative path to the main EXE of the application to be captured; <c>null</c> to auto-detect.</summary>
    private string? _mainExe;

    /// <summary>The path of the ZIP file to create from the installation directory; <c>null</c> to create no ZIP archive.</summary>
    private string? _zipFile;

    private readonly List<string> _additionalArgs;

    public CaptureCommand(IEnumerable<string> args, ITaskHandler handler)
    {
        _handler = handler;

        var options = new OptionSet
        {
            {
                "V|version", _ =>
                {
                    Console.WriteLine(@"Zero Install Capture CLI v{0}", AppInfo.Current.Version);
                    throw new OperationCanceledException();
                }
            },
            {"f|force", _ => _force = true},
            {
                "installation-dir=", value =>
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
            {"main-exe=", value => _mainExe = value},
            {"collect-files=", value => _zipFile = value},
            {
                "h|help|?", _ =>
                {
                    PrintHelp();
                    throw new OperationCanceledException();
                }
            }
        };
        _additionalArgs = options.Parse(args);
    }

    [SuppressMessage("ReSharper", "LocalizableElement")]
    private static ExitCode PrintHelp()
    {
        Console.WriteLine("0publish capture start myapp.snapshot [--force]");
        Console.WriteLine("0publish capture finish myapp.snapshot myapp.xml [--force]");
        Console.WriteLine("\t[--installation-dir=C:\\myapp] [--main-exe=myapp.exe] [--collect-files=myapp.zip]");

        return ExitCode.InvalidArguments;
    }
    #endregion

    public ExitCode Execute()
        => _additionalArgs.Count == 0
            ? PrintHelp()
            : _additionalArgs[0] switch
            {
                "start" => Start(),
                "finish" => Finish(),
                _ => PrintHelp()
            };

    private ExitCode Start()
    {
        if (_additionalArgs.Count != 2) return PrintHelp();
        string snapshotFile = _additionalArgs[1];
        if (FileExists(snapshotFile)) return ExitCode.IOError;

        var session = CaptureSession.Start(new FeedBuilder());
        session.Save(snapshotFile);

        return ExitCode.OK;
    }

    private ExitCode Finish()
    {
        if (_additionalArgs.Count != 3) return PrintHelp();
        string snapshotFile = _additionalArgs[1];
        string feedFile = _additionalArgs[2];
        if (FileExists(feedFile)) return ExitCode.IOError;

        var feedBuilder = new FeedBuilder();
        var session = CaptureSession.Load(snapshotFile, feedBuilder);

        session.InstallationDir = _installationDirectory;
        session.Diff(_handler);

        feedBuilder.MainCandidate = string.IsNullOrEmpty(_mainExe)
            ? feedBuilder.Candidates.FirstOrDefault()
            : feedBuilder.Candidates.FirstOrDefault(x => StringUtils.EqualsIgnoreCase(x.RelativePath.ToNativePath(), _mainExe));
        session.Finish();

        if (!string.IsNullOrEmpty(_zipFile))
        {
            if (FileExists(_zipFile)) return ExitCode.IOError;

            var relativeUri = new Uri(Path.GetFullPath(feedFile)).MakeRelativeUri(new Uri(Path.GetFullPath(_zipFile!)));
            session.CollectFiles(_zipFile, relativeUri, _handler);
            Log.Warn("If you wish to upload this feed and ZIP archive, make sure to turn the <archive>'s relative href into an absolute one.");
        }

        feedBuilder.Build().Save(feedFile);

        return ExitCode.OK;
    }

    private bool FileExists(string path)
    {
        if (File.Exists(path) && !_force)
        {
            Log.Error($"The file '{Path.GetFullPath(path)}' already exists. Use --force to overwrite.");
            return true;
        }
        else return false;
    }
}
