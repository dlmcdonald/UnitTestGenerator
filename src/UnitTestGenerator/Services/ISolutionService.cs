using System;
using Microsoft.CodeAnalysis;

namespace UnitTestGenerator.Services
{
    public interface ISolutionService
    {
        Solution GetCurrentSolution();
    }
}
