using System;
using Microsoft.CodeAnalysis;

namespace UnitTestGenerator.Services
{
    public interface INavigationService
    {
        void Navigate(Project project, ISymbol symbol);
    }
}
