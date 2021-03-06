﻿using System;
using System.Threading.Tasks;
using Gtk;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Ide.Composition;
using UnitTestGenerator.Models;
using UnitTestGenerator.Mac.Services.Interfaces;

namespace UnitTestGenerator.Mac.Dialogs
{
    public class AddUnitTestDialog : Dialog
    {
        readonly MethodDeclarationSyntax _currentMethod;
        readonly Label _unitTestNameTitle;
        readonly Entry _unitTestName;
        readonly Button _confirm;
        readonly MonoDevelop.Ide.Gui.Document _document;
        readonly ITestGeneratorService _testGeneratorService;
        readonly GeneratedTest _generatedTestModel;

        public AddUnitTestDialog(MonoDevelop.Ide.Gui.Document document, MethodDeclarationSyntax currentMethod, GeneratedTest generatedTestModel)
        {
            _testGeneratorService = CompositionManager.Instance.GetExportedValue<ITestGeneratorService>();
            _currentMethod = currentMethod;
            _document = document;
            _generatedTestModel = generatedTestModel;
            WindowPosition = WindowPosition.CenterAlways;
            Title = "Add Unit test method";

            //entry setup
            _unitTestNameTitle = new Label
            {
                Text = "Unit test name:"
            };

            _unitTestName = new Entry();
            _unitTestName.Activated += EntryActivated;

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

        async void EntryActivated(object sender, EventArgs e) => await CreateTest();

        protected override void OnShown()
        {
            GrabFocus();
            _unitTestName.GrabFocus();
            base.OnShown();
        }

        async void Confirm_Clicked(object sender, EventArgs e) => await CreateTest();

        async Task CreateTest()
        {
            await _testGeneratorService.GenerateUnitTest(_unitTestName.Text, _currentMethod, _document, _generatedTestModel);
            Hide();
        }

        
    }
}
