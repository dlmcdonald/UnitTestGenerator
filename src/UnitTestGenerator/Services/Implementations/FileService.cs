using System.Composition;
using System.IO;
using UnitTestGenerator.Services.Interfaces;

namespace UnitTestGenerator.Services.Implementations
{
    [Export(typeof(IFileService))]
    public class FileService : IFileService
    {
        public void GenerateFile(string namespaceId, string classId, string filePath, string templateName)
        {
            if (templateName == "XFUnitTestNUnit")
            {
                GenerateXFUnitTestNUnitFile(namespaceId, classId, filePath);
            }
        }

        public void GenerateXFUnitTestNUnitFile(string namespaceId, string classId, string filePath)
        {
            string[] lines = { "using System;",
                "using Moq;",
                "using NUnit.Framework;",
                "",
                $"namespace {namespaceId}",
                "{",
                "\t[TestFixture]",
                $"\tpublic class {classId}",
                "\t{",
                "\t\t[SetUp]",
                "\t\tpublic void Setup() => Xamarin.Forms.Mocks.MockForms.Init();",
                "",
                "\t}",
                "}"};
            var file = new FileInfo(filePath);
            file.Directory.Create(); // If the directory already exists, this method does nothing.
            File.WriteAllLines(file.FullName, lines);
        }
    }
}
