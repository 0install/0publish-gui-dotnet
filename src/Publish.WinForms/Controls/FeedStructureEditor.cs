// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common;
using NanoByte.StructureEditor.WinForms;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Capabilities;

namespace ZeroInstall.Publish.WinForms.Controls
{
    /// <summary>
    /// A hierarchical <see cref="Feed"/> editor with Undo support.
    /// </summary>
    public class FeedStructureEditor : StructureEditor<Feed>
    {
        public FeedStructureEditor()
        {
            DescribeRoot<FeedEditor>("interface")
               .AddPlainList("category", x => x.Categories)
               .AddPlainList("feed", x => x.Feeds)
               .AddPlainList("feed-for", x => x.FeedFor)
               .AddProperty("replaced-by", x => PropertyPointer.For(() => x.ReplacedBy))
               .AddPlainList("entry-point", x => x.EntryPoints, new EntryPointEditor())
               .AddPlainList("capabilities", x => x.CapabilityLists);

            Describe<IIconContainer>()
               .AddPlainList("icon", x => x.Icons, new IconEditor());

            Describe<IDependencyContainer>()
               .AddPlainList("requires", x => x.Dependencies)
               .AddPlainList("restricts", x => x.Restrictions);

            Describe<IBindingContainer>()
               .AddList(x => x.Bindings)
               .AddElement("binding", new GenericBinding())
               .AddElement("environment", new EnvironmentBinding())
               .AddElement("overlay", new OverlayBinding())
               .AddElement("executable-in-var", new ExecutableInVar())
               .AddElement("executable-in-path", new ExecutableInPath());

            Describe<Element>()
               .AddPlainList("command", x => x.Commands);

            Describe<Implementation>()
               .AddList(implementation => implementation.RetrievalMethods)
               .AddElement("archive", new Archive(), new ArchiveEditor())
               .AddElement("file", new SingleFile(), new SingleFileEditor())
               .AddElement("recipe", new Recipe(), new RecipeEditor());

            Describe<IElementContainer>()
               .AddList(x => x.Elements)
               .AddElement("implementation", new Implementation())
               .AddElement("package-implementation", new PackageImplementation())
               .AddElement("group", new Group());

            Describe<Restriction>()
               .AddPlainList("version", x => x.Constraints);

            Describe<Command>()
               .AddProperty("runner", x => PropertyPointer.For(() => x.Runner))
               .AddProperty("working-dir", x => PropertyPointer.For(() => x.WorkingDir));

            Describe<IArgBaseContainer>()
               .AddList(x => x.Arguments)
               .AddElement("arg", new Arg())
               .AddElement("for-each", new ForEachArgs());
            Describe<ForEachArgs>()
               .AddPlainList("arg", x => x.Arguments);

            Describe<Recipe>()
               .AddList(x => x.Steps)
               .AddElement("archive", new Archive(), new ArchiveEditor())
               .AddElement("file", new SingleFile(), new SingleFileEditor())
               .AddElement("rename", new RenameStep())
               .AddElement("remove", new RemoveStep())
               .AddElement("copy-from", new CopyFromStep());

            Describe<CapabilityList>()
               .AddList(x => x.Entries)
               .AddElement("registration", new AppRegistration())
               .AddElement("auto-play", new AutoPlay(), new DescriptionEditor<AutoPlay>())
               .AddElement("com-server", new ComServer())
               .AddElement("context-menu", new ContextMenu())
               .AddElement("default-program", new DefaultProgram(), new DescriptionEditor<DefaultProgram>())
               .AddElement("file-type", new FileType(), new DescriptionEditor<FileType>())
               .AddElement("url-protocol", new UrlProtocol(), new DescriptionEditor<UrlProtocol>());
            Describe<AutoPlay>()
               .AddPlainList("event", x => x.Events);
            Describe<ISingleVerb>()
               .AddProperty("verb", x => PropertyPointer.For(() => x.Verb), new DescriptionEditor<Verb>());
            Describe<FileType>()
               .AddPlainList("extension", x => x.Extensions);
            Describe<UrlProtocol>()
               .AddPlainList("known-prefix", x => x.KnownPrefixes);
            Describe<VerbCapability>()
               .AddPlainList("verb", x => x.Verbs, new DescriptionEditor<Verb>());
        }

    }
}
