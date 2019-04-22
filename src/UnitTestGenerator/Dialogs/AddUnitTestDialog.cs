using System;
using System.Linq;
using Gtk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Ide.Composition;
using UnitTestGenerator.Helpers;
using UnitTestGenerator.Services.Interfaces;

namespace UnitTestGenerator.Dialogs
{
    public class AddUnitTestDialog : Dialog
    {
        readonly MethodDeclarationSyntax _currentMethod;
        readonly Label _unitTestNameTitle;
        readonly Entry _unitTestName;
        readonly Button _confirm;
        readonly MonoDevelop.Ide.Gui.Document _document;
        readonly ITestGeneratorService _testGeneratorService;

        public AddUnitTestDialog(MonoDevelop.Ide.Gui.Document document, MethodDeclarationSyntax currentMethod)
        {
            _testGeneratorService = CompositionManager.GetExportedValue<ITestGeneratorService>();
            _currentMethod = currentMethod;
            _document = document;
            WindowPosition = WindowPosition.CenterAlways;
            Title = "Add Unit test method";

            //entry setup
            _unitTestNameTitle = new Label
            {
                Text = "Unit test name:"
            };

            _unitTestName = new Entry();

            //button setup
            _confirm = new Button
            {
                Label = "Create"
            };
            _confirm.Clicked += Confirm_Clicked;

            VBox.Add(_unitTestNameTitle);
            VBox.Add(_unitTestName);
            VBox.Add(_confirm);

            VBox.ShowAll();
        }

        protected override void OnShown()
        {
            GrabFocus();
            _unitTestName.GrabFocus();
            base.OnShown();
        }

        async void Confirm_Clicked(object sender, EventArgs e)
        { 
            await _testGeneratorService.GenerateUnitTest(_unitTestName.Text, _currentMethod, _document);
            Hide();
        }

        
    }
}
