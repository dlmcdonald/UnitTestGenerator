using System.Composition;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using UnitTestGenerator.Services;

namespace UnitTestGenerator.Mac.Services
{
    [Export(typeof(INavigationService))]
    public class NavigationService : INavigationService
    {
        public void Navigate(Project project, ISymbol symbol)
        {
            IdeApp.ProjectOperations.JumpToDeclaration(symbol);
        }
    }
}
