using System.Collections.Generic;

namespace UnitTestGenerator.Models
{
    public class TestReference
    {
        public string ClassName { get; set; }
        public List<MethodReference> MethodReferences { get; set; }
    }
}
