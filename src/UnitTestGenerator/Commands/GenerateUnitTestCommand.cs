using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using MonoDevelop.Components.Commands;
using UnitTestGenerator.Dialogs;
using UnitTestGenerator.Helpers;

namespace UnitTestGenerator.Commands
{
    public class GenerateUnitTestCommand : CommandHandler
    {

        protected override void Update(CommandInfo info)
        {
            info.Bypass = false;
            info.Visible = false;
            info.Enabled = false;

            var utHelper = new UnitTestHelper();
            try
            { 
                var method = utHelper.GetActiveMethodDeclarationSyntax();

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
            var config = new ConfigurationHelper().GetConfiguration();
            if (string.IsNullOrWhiteSpace(config.UnitTestProjectName))
            {
                var dialog = new ConfigureUnitTestProjectDialog();
                return;
            }

            var utHelper = new UnitTestHelper();
            var currentMethod = utHelper.GetActiveMethodDeclarationSyntax();
            if (currentMethod == null)
                return;
            var generatedTestModel = utHelper.CreateGeneratedTestModel(currentMethod);
            var document = utHelper.OpenDocument(generatedTestModel);



            var addTestDialog = new AddUnitTestDialog(document, currentMethod);
            addTestDialog.Show();

            base.Run();
        }
    }
}
