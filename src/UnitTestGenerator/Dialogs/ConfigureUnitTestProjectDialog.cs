using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Composition;
using UnitTestGenerator.Services.Interfaces;

namespace UnitTestGenerator.Dialogs
{
    public class ConfigureUnitTestProjectDialog : Dialog
    {
        readonly TreeView _projectList;
        readonly Button _confirm;
        readonly Label _selectedProjectLabel;
        readonly List<string> _projects;
        readonly IConfigurationService _configurationService;
        string _selectedProject = "";

        public ConfigureUnitTestProjectDialog()
        {
            WindowPosition = WindowPosition.CenterAlways;
            Title = "Configure UnitTest project";
            _configurationService = CompositionManager.GetExportedValue<IConfigurationService>();


            //TreeView setup
            _projectList = new TreeView();
            _projectList.Selection.Mode = SelectionMode.Single;
            _projectList.Selection.Changed += Selection_Changed;

            //Project column
            var projectCol = new TreeViewColumn
            {
                Title = "Projects"
            };
            _projectList.AppendColumn(projectCol);

            //project cell
            var projectCell = new CellRendererText();
            projectCol.PackStart(projectCell, true);
            projectCol.AddAttribute(projectCell, "text", 0);

            //project model
            var projectsData = new ListStore(typeof(string));
            _projectList.Model = projectsData;
            _projects = IdeApp.Workspace.GetAllProjects().Select(p => p.Name).ToList();
            foreach (var proj in _projects)
            {
                projectsData.AppendValues(proj);
            }

            //selected project label
            _selectedProjectLabel = new Label
            {
                Text = "Selected project: None"
            };

            var frame = new Frame
            {
                _selectedProjectLabel
            };

            //button setup
            _confirm = new Button
            {
                Label = "Save"
            };
            _confirm.Clicked += Confirm_Clicked;

            VBox.Add(_projectList);
            VBox.Add(frame);
            VBox.Add(_confirm);

            VBox.ShowAll();
            ShowAll();
        }

        void Selection_Changed(object sender, EventArgs e)
        {
            //Selection changed triggers before project label has been instantiated correctly
            if (_selectedProjectLabel != null)
            {
                if (!_projectList.Selection.GetSelected(out var model, out var iter))
                {
                    _selectedProjectLabel.Text = "Selected project: None";
                }
                var path = model.GetPath(iter);
                var selectedIndex = int.Parse(path.ToString());
                _selectedProject = _projects[selectedIndex];
                _selectedProjectLabel.Text = $"Selected project: {_selectedProject}";
            }
        }

        void Confirm_Clicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_selectedProject))
            {
                var config = _configurationService.GetConfiguration();
                config.UnitTestProjectName = _selectedProject;
                _configurationService.Save(config);
            }
            Hide();
        }

    }
}
