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
            var lines = new List<string>();

            if (config.DefaultUsings != null && config.DefaultUsings.Any())
            {
                foreach (var usingStatement in config.DefaultUsings)
                {
                    lines.Add($"using {usingStatement};");
                }
                lines.Add("");
            }

            lines.AddRange(new List<string> {
                $"namespace {namespaceId}",
                "{",
                "\t[TestFixture]",
                $"\tpublic class {classId}",
                "\t{",
                "\t\t[SetUp]",
                "\t\tpublic void Setup()",
                "\t\t{",
            });
            if (config.CustomSetupMethodLines != null && config.CustomSetupMethodLines.Any())
            {

                foreach (var line in config.CustomSetupMethodLines)
                {
                    lines.Add($"\t\t\t{line}");
                }

            }
            else
            {
                lines.Add("\t\t\t");
            }
            lines.Add("\t\t}");

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
