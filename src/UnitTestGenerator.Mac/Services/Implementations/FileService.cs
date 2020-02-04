using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using MonoDevelop.Ide.Composition;
using UnitTestGenerator.Mac.Services.Interfaces;
using System.Threading.Tasks;

namespace UnitTestGenerator.Mac.Services.Implementations
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
                if ("nunit".Equals(config.TestFramework))
                    lines.Add("using NUnit.Framework;");
                else if ("xunit".Equals(config.TestFramework))
                    lines.Add("using Xunit;");
                lines.Add("");
            }


            lines.Add($"namespace {namespaceId}");
            lines.Add("{");
            if ("nunit".Equals(config.TestFramework))
                lines.Add("\t[TestFixture]");
            lines.Add($"\tpublic class {classId}");
            lines.Add("\t{");
            if ("nunit".Equals(config.TestFramework))
            {
                lines.AddRange(new List<string>
                {
                    
                    "\t\t[SetUp]",
                    "\t\tpublic void Setup()",
                    "\t\t{",
                    "\t\t\t",
                    "\t\t}",
                    ""
                });
            }
            else
            {
                lines.Add("");
            }

            lines.AddRange(new List<string>{
                "\t}",
                "}"});
            var file = new FileInfo(filePath);
            file.Directory.Create(); // If the directory already exists, this method does nothing.
            File.WriteAllLines(file.FullName, lines);
        }
    }
}
