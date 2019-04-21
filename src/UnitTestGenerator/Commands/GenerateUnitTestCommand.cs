using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Composition;
using UnitTestGenerator.Dialogs;
using UnitTestGenerator.Services.Interfaces;

namespace UnitTestGenerator.Commands
{
    public class GenerateUnitTestCommand : CommandHandler
    {
        readonly ITestGeneratorService _testGeneratorService;
        readonly IConfigurationService _configurationService;

        public GenerateUnitTestCommand()
        {
            _testGeneratorService = CompositionManager.GetExportedValue<ITestGeneratorService>();
            _configurationService = CompositionManager.GetExportedValue<IConfigurationService>();
        }

        protected override void Update(CommandInfo info)
        {
            info.Bypass = false;
            info.Visible = false;
            info.Enabled = false;

            try
            { 
                var method = _testGeneratorService.GetActiveMethodDeclarationSyntax();

                if (method == null)
                    return;
            
                if (method.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
                {

                    info.Bypass = false;
                    info.Visible = true;
                    info.Enabled = true;
                }
                else
                {
                    info.Bypass = false;
                    info.Visible = true;
                    info.Enabled = false;
                }                
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                info.Bypass = false;
                info.Visible = false;
                info.Enabled = false;
            }
        }

        protected override void Run()
        {
            var config = _configurationService.GetConfiguration();
            if (string.IsNullOrWhiteSpace(config.UnitTestProjectName))
            {
                var dialog = new ConfigureUnitTestProjectDialog();
                return;
            }

            
            var currentMethod = _testGeneratorService.GetActiveMethodDeclarationSyntax();
            if (currentMethod == null)
                return;
            var generatedTestModel = _testGeneratorService.CreateGeneratedTestModel(currentMethod);
            var document = _testGeneratorService.OpenDocument(generatedTestModel);



            var addTestDialog = new AddUnitTestDialog(document, currentMethod);
            addTestDialog.Show();

            base.Run();
        }
    }
}
