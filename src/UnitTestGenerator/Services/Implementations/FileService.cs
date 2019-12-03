using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using MonoDevelop.Ide.Composition;
using UnitTestGenerator.Services.Interfaces;
using System.Threading.Tasks;

namespace UnitTestGenerator.Services.Implementations
{
    [Export(typeof(IFileService))]
    public class FileService : IFileService
    {
        readonly IConfigurationService _configurationService;
        public FileService()
        {
            _configurationService = CompositionManager.Instance.GetExportedValue<IConfigurationService>();
        }

        public async Task GenerateFile(string namespaceId, string classId, string filePath)
        {
            var config = await _configurationService.GetConfiguration();
            var lines = new List<string> { "using System;",
                "using Moq;",
                "using NUnit.Framework;",
                "",
                $"namespace {namespaceId}",
                "{",
                "\t[TestFixture]",
                $"\tpublic class {classId}",
                "\t{",
                "\t\t[SetUp]"};
            if (config.UseCustomSetupMethod && config.CustomSetupMethodLines != null && config.CustomSetupMethodLines.Any())
            {
                    lines.Add("\t\tpublic void Setup()");
                    lines.Add("\t\t{");
                    foreach (var line in config.CustomSetupMethodLines)
                    {
                        lines.Add($"\t\t\t{line}");
                    }
                    lines.Add("\t\t}");
            }
            else
            {
                //use default
                lines.Add("\t\tpublic void Setup() => Xamarin.Forms.Mocks.MockForms.Init();");
            }

            lines.AddRange(new List<string>{
                "",
                "\t}",
                "}"});
            var file = new FileInfo(filePath);
            file.Directory.Create(); // If the directory already exists, this method does nothing.
            File.WriteAllLines(file.FullName, lines);
        }

        
    }
}
