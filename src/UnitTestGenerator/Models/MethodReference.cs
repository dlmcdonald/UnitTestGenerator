using Microsoft.CodeAnalysis;

namespace UnitTestGenerator.Models
{
    public class MethodReference
    {
        public string MethodName { get; set; }
        public ISymbol MethodSymbol { get; set; }
    }
}
