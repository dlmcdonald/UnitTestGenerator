﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using MonoDevelop.Ide;
using UnitTestGenerator.Helpers;

namespace UnitTestGenerator.Dialogs
{
    public class ConfigureUnitTestProjectDialog : Dialog
    {
        readonly TreeView _projectList;
        readonly Button _confirm;
        readonly Label _selectedProjectLabel;
        readonly List<string> _projects;
        string _selectedProject = "";

        public ConfigureUnitTestProjectDialog()
        {
            WindowPosition = WindowPosition.CenterAlways;
            Title = "Configure UnitTest project";



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

        private void Selection_Changed(object sender, EventArgs e)
        {
            if (!_projectList.Selection.GetSelected(out TreeModel model, out TreeIter iter))
            {
                _selectedProjectLabel.Text = "Selected project: None";
            }
            TreePath path = model.GetPath(iter);
            var selectedIndex = int.Parse(path.ToString());
            _selectedProject = _projects[selectedIndex];
            _selectedProjectLabel.Text = $"Selected project: {_selectedProject}";
        }

        protected override void OnShown()
        {
            base.OnShown();
        }

        void Confirm_Clicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_selectedProject))
            {
                var configHelper = new ConfigurationHelper();
                var config = configHelper.GetConfiguration();
                config.UnitTestProjectName = _selectedProject;
                configHelper.Save(config);
            }
            Hide();
        }

    }
}
