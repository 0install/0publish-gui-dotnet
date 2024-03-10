// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using System.Globalization;
using IniParser;
using Mono.Cecil;
using NanoByte.Common.Collections;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using Vestris.ResourceLib;
using ZeroInstall.Client;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Icons;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish.Cli;

/// <summary>
/// Builds a customized Zero Install Bootstrapper for running or integrating a specific feed.
/// </summary>
internal class BootstrapCommand : ICommand
{
    private readonly ITaskHandler _handler;
    private readonly IFeedCache _feedCache = FeedCaches.Default(OpenPgp.Verifying());
    private readonly IIconStore _iconStore;

    /// <summary>The feed URI of the target application to bootstrap.</summary>
    private readonly FeedUri _feedUri;

    /// <summary>The path of the bootstrap EXE to build.</summary>
    private readonly string _outputFile;

    /// <summary>
    /// Parses command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to be parsed.</param>
    /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
    /// <exception cref="OperationCanceledException">The user asked to see help information, version information, etc..</exception>
    /// <exception cref="OptionException"><paramref name="args"/> contains unknown options.</exception>
    public BootstrapCommand(IEnumerable<string> args, ITaskHandler handler)
    {
        _handler = handler;
        _iconStore = IconStores.Cache(Config.LoadSafe(), handler);

        switch (BuildOptions().Parse(args))
        {
            case [var feedUri, var outputFile]:
                _feedUri = new(feedUri);
                _outputFile = Path.GetFullPath(outputFile);
                break;

            default:
                throw new OptionException(string.Format(Resources.MissingArguments, "0publish bootstrap --help"), "");
        }
    }

    #region Options
    /// <summary>Overwrite existing files.</summary>
    private bool _force;

    /// <summary>Additional command-line arguments to pass to the application launched by the feed.</summary>
    private string? _appArgs;

    /// <summary>Command-line arguments to pass to <c>0install integrate</c>. <c>null</c> to not call '0install integrate' at all.</summary>
    private string? _integrateArgs;

    /// <summary>The URI of the catalog to replace the default catalog. Only applies if Zero Install is not already deployed.</summary>
    private FeedUri? _catalogUri;

    /// <summary>Offer the user to choose a custom path for storing implementations.</summary>
    private bool _customizableStorePath;

    /// <summary>Show the estimated disk space required (in bytes). Only works when <see cref="_customizableStorePath"/> is <c>true</c>.</summary>
    private int? _estimatedRequiredSpace;

    /// <summary>Set Zero Install configuration options. Only overrides existing config files if Zero Install is not already deployed.</summary>
    private readonly Config _config = new();

    /// <summary>A directory containing additional content to be embedded in the bootstrapper.</summary>
    private DirectoryInfo? _contentDir;

    /// <summary>Path or URI to the boostrap template executable.</summary>
    private Uri _template = new("https://get.0install.net/zero-install.exe");

    private OptionSet BuildOptions()
    {
        var options = new OptionSet
        {
            {"f|force", () => Resources.OptionForce, _ => _force = true},
            {"a|app-args=", () => Resources.OptionAppArgs, x => _appArgs = x},
            {"i|integrate-args=", () => Resources.OptionIntegrateArgs, x => _integrateArgs = x},
            {"catalog-uri=", () => Resources.OptionCatalogUri, (FeedUri x) => _catalogUri = x},
            {"customizable-store-path", () => Resources.OptionCustomizableStorePath, _ => _customizableStorePath = true},
            {"estimated-required-space=", () => Resources.OptionEstimatedRequiredSpace, (int x) =>
                {
                    _customizableStorePath = true;
                    _estimatedRequiredSpace = x;
                }
            },
            {"c|config==", () => Resources.OptionConfig, (key, value) => _config.SetOption(key, value) },
            {"content=", () => Resources.OptionContent, x => _contentDir = new(x)},
            {"template=", () => Resources.OptionTemplate, (Uri x) => _template = x}
        };

        options.Add("h|help|?", () => Resources.OptionHelp, _ =>
        {
            Console.WriteLine(Resources.DescriptionBootstrap);
            Console.WriteLine();
            Console.WriteLine(Resources.Usage);
            Console.WriteLine(@"0publish bootstrap [OPTIONS] FEED-URI OUTPUT-FILE");
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
        DownloadFeed();
        var feed = _feedCache.GetFeed(_feedUri) ?? throw new FileNotFoundException();
        string? keyFingerprint = _feedCache.GetSignatures(_feedUri).OfType<ValidSignature>().FirstOrDefault()?.FormatFingerprint();

        string? icon = feed.Icons.GetIcon(Icon.MimeTypeIco)?.To(_iconStore.GetFresh);
        string? splashScreen = feed.SplashScreens.GetIcon(Icon.MimeTypePng)?.To(_iconStore.GetFresh);

        InitializeFromTemplate();

        _handler.RunTask(new ActionTask(Resources.BuildingBootstrapper, () =>
        {
            using var bootstrapConfig = BuildBootstrapConfig(feed, keyFingerprint, customSplashScreen: splashScreen != null);
            ModifyEmbeddedResources(bootstrapConfig, splashScreen);
            if (icon != null) ReplaceIcon(icon);
        }));
    }

    private void DownloadFeed()
        => _handler.RunTask(new ActionTask(
            string.Format(Resources.Downloading, _feedUri.ToStringRfc()),
            () => ZeroInstallClient.Detect.SelectAsync(_feedUri, refresh: true).Wait()));

    private Stream BuildBootstrapConfig(Feed feed, string? keyFingerprint, bool customSplashScreen)
    {
        var iniData = _config.ToIniData();
        iniData.Sections.Add(new("bootstrap")
        {
            Keys =
            {
                ["key_fingerprint"] = keyFingerprint ?? "",
                ["app_uri"] = _feedUri.ToStringRfc(),
                ["app_name"] = feed.Name,
                ["app_args"] = _appArgs ?? "",
                ["integrate_args"] = _integrateArgs ?? "",
                ["catalog_uri"] = _catalogUri?.ToStringRfc() ?? "",
                ["show_app_name_below_splash_screen"] = (!customSplashScreen).ToString().ToLowerInvariant(),
                ["customizable_store_path"] = _customizableStorePath.ToString().ToLowerInvariant(),
                ["estimated_required_space"] = _estimatedRequiredSpace?.ToString(CultureInfo.InvariantCulture) ?? ""
            }
        });

        var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, EncodingUtils.Utf8, bufferSize: 1024, leaveOpen: true))
            new StreamIniDataParser().WriteData(writer, iniData);
        stream.Position = 0;
        return stream;
    }

    private void InitializeFromTemplate()
    {
        if (File.Exists(_outputFile) && !_force) throw new IOException(string.Format(Resources.FileAlreadyExists, _outputFile));

        if (_template.IsFile)
            _handler.RunTask(new ReadFile(_template.LocalPath, stream => stream.CopyToFile(_outputFile)));
        else
            _handler.RunTask(new DownloadFile(_template, _outputFile));
    }

    private void ModifyEmbeddedResources(Stream bootstrapConfig, string? splashScreenPath)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(_outputFile, new() {ReadWrite = true});
        assembly.Name.Name = Path.GetFileNameWithoutExtension(_outputFile);

        var resources = assembly.MainModule.Resources;

        void Replace(string name, Stream stream)
        {
            resources.RemoveAll(x => x.Name == name);
            resources.Add(new EmbeddedResource(name, ManifestResourceAttributes.Public, stream));
        }

        Replace("ZeroInstall.BootstrapConfig.ini", bootstrapConfig);

        using var splashScreen = splashScreenPath?.To(File.OpenRead);
        if (splashScreen != null) Replace("ZeroInstall.SplashScreen.png", splashScreen);

        _contentDir?.Walk(
            fileAction: file => resources.Add(new EmbeddedResource(
                name: "ZeroInstall.content." + WebUtility.UrlDecode(file.RelativeTo(_contentDir).Replace(Path.DirectorySeparatorChar, '.')),
                ManifestResourceAttributes.Public,
                file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))));

        assembly.Write();
    }

    private void ReplaceIcon(string path)
    {
        try
        {
            new IconDirectoryResource(new(path)).SaveTo(_outputFile);
        }
        #region Error handling
        catch (Win32Exception ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new IOException(ex.Message, ex);
        }
        #endregion
    }
}
