using System;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using Newtonsoft.Json;
using UnitTestGenerator.Models;
using UnitTestGenerator.Services.Interfaces;

namespace UnitTestGenerator.Services.Implementations
{
    [Export(typeof(IConfigurationService))]
    public class ConfigurationService : IConfigurationService
    {
        public async Task<Configuration> GetConfiguration()
        {
            var solution = IdeApp.Workspace.GetAllSolutions().FirstOrDefault();

            //Check if we have a solution folder called "Solution items"
            //if it doesnt exist, create it
            if (!(solution.RootFolder.Items.FirstOrDefault(s => s is SolutionFolder && s.Name.Equals("solution items", StringComparison.InvariantCultureIgnoreCase)) is SolutionFolder solutionItemsFolder))
            {
                solution.RootFolder.AddItem(new SolutionFolder { Name = "Solution items" });
                solutionItemsFolder = solution.RootFolder.Items.FirstOrDefault(s => s is SolutionFolder && s.Name.Equals("solution items", StringComparison.InvariantCultureIgnoreCase)) as SolutionFolder;
            }
            //check if it contains the config file
            var configFile = solutionItemsFolder.Files.FirstOrDefault(f => f.FileName.Equals("testgenerator.json"));
            var config = Configuration.DefaultConfiguration();
            //if it doesnt exist, create it
            if (configFile == null)
            {
                var configJson = JsonConvert.SerializeObject(config, Formatting.Indented);
                var filePath = solutionItemsFolder.BaseDirectory + "/testgenerator.json";
                //write the file to disk
                File.WriteAllText(filePath, configJson);
                //add the file to the project
                solutionItemsFolder.Files.Add(filePath);
                using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor(false))
                {
                    await solution.SaveAsync(monitor);
                }
            }
            else
            {
                //Load the configuration
                var configString = File.ReadAllText(configFile);
                config = JsonConvert.DeserializeObject<Configuration>(configString);
            }
            return config;
        }

        public void Save(Configuration config)
        {
            var solution = IdeApp.Workspace.GetAllSolutions().FirstOrDefault();
            var solutionFolder = solution.RootFolder.Items.FirstOrDefault(s => s is SolutionFolder && s.Name.Equals("solution items", StringComparison.InvariantCultureIgnoreCase)) as SolutionFolder;
            var configFile = solutionFolder.Files.FirstOrDefault(f => f.FileName.Equals("testgenerator.json"));
            var filePath = solutionFolder.BaseDirectory + "/testgenerator.json";
            File.WriteAllText(filePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }
}
