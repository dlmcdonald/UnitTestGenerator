using System;
using Gtk;

namespace UnitTestGenerator.Mac.Dialogs
{
    public class ErrorDialog : Dialog
    {
        readonly Label _messageLabel;
        readonly Button _okButton;
        public ErrorDialog(string message)
        {
            WindowPosition = WindowPosition.CenterAlways;
            Title = "Error";
            _messageLabel = new Label
            {
                Text = message
            };
            _okButton = new Button
            {
                Label = "OK"
            };
            _okButton.Clicked += OkButtonClicked;

            VBox.Add(_messageLabel);
            VBox.Add(_okButton);

            VBox.ShowAll();
        }

        private void OkButtonClicked(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
