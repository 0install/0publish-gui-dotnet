// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.Publish.WinForms.Controls;

/// <summary>
/// A hierarchical <see cref="Feed"/> editor with Undo support.
/// </summary>
public class FeedStructureEditor : StructureEditor<Feed>
{
    public FeedStructureEditor()
        : base(() => new Feed {Name = "My App"})
    {
        DescribeRoot<FeedEditor>("interface")
           .AddPlainList("category", x => x.Categories)
           .AddPlainList("feed", x => x.Feeds, () => new() {Source = null!})
           .AddPlainList("feed-for", x => x.FeedFor, () => new() {Target = null!})
           .AddProperty("replaced-by", x => PropertyPointer.For(() => x.ReplacedBy), () => new() {Target = null!})
           .AddPlainList("entry-point", x => x.EntryPoints, () => new() {Command = Command.NameRun}, new EntryPointEditor())
           .AddPlainList("capabilities", x => x.CapabilityLists)
           .AddPlainList("splash-screen", x => x.SplashScreens, () => new() {Href = null!}, new IconEditor());

        Describe<IIconContainer>()
           .AddPlainList("icon", x => x.Icons, () => new()  {Href = null!}, new IconEditor());

        Describe<IDependencyContainer>()
           .AddPlainList("requires", x => x.Dependencies, () => new() {InterfaceUri = null!})
           .AddPlainList("restricts", x => x.Restrictions, () => new() {InterfaceUri = null!});

        Describe<IBindingContainer>()
           .AddList(x => x.Bindings)
           .AddElement("binding", () => new GenericBinding())
           .AddElement("environment", () => new EnvironmentBinding {Name = "ENV_VAR"})
           .AddElement("overlay", () => new OverlayBinding())
           .AddElement("executable-in-var", () => new ExecutableInVar {Name = "ENV_VAR"})
           .AddElement("executable-in-path", () => new ExecutableInPath {Name = "ENV_VAR"});

        Describe<Element>()
           .AddPlainList("command", x => x.Commands, () => new() {Name = Command.NameRun});

        Describe<Implementation>()
           .AddList(implementation => implementation.RetrievalMethods)
           .AddElement("archive", () => new Archive {Href = null!}, new ArchiveEditor())
           .AddElement("file", () => new SingleFile {Href = null!, Destination = null!}, new SingleFileEditor())
           .AddElement("recipe", () => new Recipe(), new RecipeEditor());

        Describe<IElementContainer>()
           .AddList(x => x.Elements)
           .AddElement("implementation", () => new Implementation {ID = null!, Version = new("1.0")})
           .AddElement("package-implementation", () => new PackageImplementation())
           .AddElement("group", () => new Group());

        Describe<Restriction>()
           .AddPlainList("version", x => x.Constraints);

        Describe<Command>()
           .AddProperty("runner", x => PropertyPointer.For(() => x.Runner), () => new() {InterfaceUri = null!})
           .AddProperty("working-dir", x => PropertyPointer.For(() => x.WorkingDir));

        Describe<IArgBaseContainer>()
           .AddList(x => x.Arguments)
           .AddElement("arg", () => new Arg {Value = "--arg"})
           .AddElement("for-each", () => new ForEachArgs {ItemFrom = "ENV_VAR"});
        Describe<ForEachArgs>()
           .AddPlainList("arg", x => x.Arguments, () => new() {Value = "--arg=${item}"});

        Describe<Recipe>()
           .AddList(x => x.Steps)
           .AddElement("archive", () => new Archive {Href = null!}, new ArchiveEditor())
           .AddElement("file", () => new SingleFile {Href = null!, Destination = null!}, new SingleFileEditor())
           .AddElement("rename", () => new RenameStep {Source = "source_dir", Destination = "target_dir"})
           .AddElement("remove", () => new RemoveStep {Path = "file_or_dir"})
           .AddElement("copy-from", () => new CopyFromStep());

        Describe<CapabilityList>()
           .AddList(x => x.Entries)
           .AddElement("file-type", () => new FileType {ID = RandomID()}, new DescriptionEditor<FileType>())
           .AddElement("url-protocol", () => new UrlProtocol {ID = RandomID()}, new DescriptionEditor<UrlProtocol>())
           .AddElement("context-menu", () => new Model.Capabilities.ContextMenu {ID = RandomID()}, new DescriptionEditor<Model.Capabilities.ContextMenu>())
           .AddElement("com-server", () => new ComServer {ID = RandomID()})
           .AddElement("auto-play", () => new AutoPlay {ID = RandomID(), Provider = "My App", Verb = null!}, new DescriptionEditor<AutoPlay>())
           .AddElement("registration", () => new AppRegistration {ID = RandomID(), CapabilityRegPath = null!})
           .AddElement("default-program", () => new DefaultProgram {ID = RandomID(), Service = "Media"}, new DescriptionEditor<DefaultProgram>())
           .AddElement("remove-hook", () => new RemoveHook {ID = RandomID()});
        Describe<AutoPlay>()
           .AddPlainList("event", x => x.Events, () => new() {Name = null!})
           .AddProperty("verb", x => PropertyPointer.For(() => x.Verb), () => new() {Name = "play"}, new DescriptionEditor<Verb>());
        Describe<Model.Capabilities.ContextMenu>()
           .AddPlainList("extension", x => x.Extensions, () => new() {Value = ".ext"});
        Describe<FileType>()
           .AddPlainList("extension", x => x.Extensions, () => new() {Value = ".ext"});
        Describe<UrlProtocol>()
           .AddPlainList("known-prefix", x => x.KnownPrefixes, () => new() {Value = "http"});
        Describe<VerbCapability>()
           .AddPlainList("verb", x => x.Verbs, () => new() {Name = "open"}, new DescriptionEditor<Verb>());
        Describe<Verb>()
           .AddPlainList("arg", x => x.Arguments, () => new() {Value = "\"%V\""});
    }

    private static string RandomID()
        => Guid.NewGuid().ToString();
}
