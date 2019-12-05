using System;
using System.Collections.Generic;
using GLib;

namespace UnitTestGenerator.Models
{
    public class GeneratedTest
    {
        public string FilePath { get; set; }
        public string Namespace { get; set; }
        public string Name { get; set; }

        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public List<Parameter> ClassConstructorParameters { get; set; }
        public List<Parameter> MethodParameters { get; set; }
        public bool IsTask { get; set; }
        public string ReturnType { get; set; }
    }
}
