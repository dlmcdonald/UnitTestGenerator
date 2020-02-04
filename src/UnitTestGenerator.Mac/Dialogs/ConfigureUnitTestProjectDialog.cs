using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Gtk;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Composition;
using UnitTestGenerator.Mac.Services.Interfaces;

namespace UnitTestGenerator.Mac.Dialogs
{
    public class ConfigureUnitTestProjectDialog : Dialog
    {
        readonly TreeView _projectList;
        readonly Button _confirm;
        readonly Label _selectedProjectLabel;
        readonly List<string> _projects;
        readonly IConfigurationService _configurationService;
        string _selectedProject = "";
        Solution _solution;

        public ConfigureUnitTestProjectDialog(Solution solution)
        {
            _solution = solution;
            WindowPosition = WindowPosition.CenterAlways;
            Title = "Configure UnitTest project";
            _configurationService = CompositionManager.Instance.GetExportedValue<IConfigurationService>();


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
            _projects = _solution.Projects.Select(p => p.Name).ToList();
            //_projects = IdeApp.Workspace.GetAllProjects().Select(p => p.Name).ToList();
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

        async void Confirm_Clicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_selectedProject))
            {
                
                var project = _solution.Projects.FirstOrDefault(p => p.Name.Equals(_selectedProject));
                var projectType = "";
                if (project != null)
                {
                    //determine the framework
                    var references = GetPackageReferences(project);

                    Debug.WriteLine("Got project dependecies");

                    if (references.Contains("nunit"))
                        projectType = "nunit";
                    else if (references.Contains("xunit"))
                        projectType = "xunit";
                }
                var config = await _configurationService.GetConfiguration();
                config.UnitTestProjectName = _selectedProject;
                config.TestFramework = string.IsNullOrWhiteSpace(projectType) ? "nunit" : projectType;
                _configurationService.Save(config);
            }
            Hide();
        }

        List<string> GetPackageReferences(Project project)
        {
            var references = new List<string>();
            var csproj = new XmlDocument();
            try
            {
                csproj.Load(project.FilePath);
                var nodes = csproj.SelectNodes("//PackageReference[@Include and @Version]");

                foreach (XmlNode packageReference in nodes)
                {
                    var packageName = packageReference.Attributes["Include"].Value;
                    references.Add(packageName);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Got an exception while trying to load the selected project:");
                Debug.WriteLine(e);
            }

            return references;
        }

    }
}
