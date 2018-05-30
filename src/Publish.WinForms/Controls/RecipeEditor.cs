// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Linq;
using ZeroInstall.Publish.WinForms.Properties;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.WinForms.Controls
{
    /// <summary>
    /// Edits <see cref="Recipe"/> instances.
    /// </summary>
    public partial class RecipeEditor : RecipeEditorShim
    {
        public RecipeEditor()
        {
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void UpdateHint()
        {
            if (Target.Steps.OfType<DownloadRetrievalMethod>().Any(x => x.Size == 0)) ShowUpdateHint(Resources.SizeMissing);
            else base.UpdateHint();
        }
    }

    /// <summary>
    /// Non-generic base class for <see cref="RecipeEditor"/>, because WinForms editor cannot handle generics.
    /// </summary>
    public class RecipeEditorShim : RetrievalMethodEditor<Recipe>
    {}
}
