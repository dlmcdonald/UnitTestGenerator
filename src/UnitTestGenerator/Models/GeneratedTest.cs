using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<string> RequiredNamespaces { get; set; }

        public void AddNamespaces(List<string> namespaceList)
        {
            if (RequiredNamespaces == null)
                RequiredNamespaces = new List<string>();
            if (namespaceList == null || !namespaceList.Any())
                return;
            foreach (var namespaceItem in namespaceList)
            {
                if (!string.IsNullOrWhiteSpace(namespaceItem) && !RequiredNamespaces.Contains(namespaceItem))
                    RequiredNamespaces.Add(namespaceItem);
            }
        }
    }
}
