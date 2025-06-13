using System;
using System.Drawing;
using System.Windows.Forms;

namespace GameSaveManager
{
    public class CenteredYesNoDialog : Form
    {
        public DialogResult Result { get; private set; } = DialogResult.No;

        public CenteredYesNoDialog(string message, string title)
        {
            this.Text = title;
            this.Size = new Size(440, 210); // Increased width and height
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            var label = new Label
            {
                Text = message,
                AutoSize = false,
                Size = new Size(400, 70), // Increased width and height for long text
                Location = new Point(20, 20)
            };
            this.Controls.Add(label);

            var buttonYes = new Button
            {
                Text = "Yes",
                DialogResult = DialogResult.Yes,
                Location = new Point(110, 110),
                Size = new Size(80, 30)
            };
            buttonYes.Click += (s, e) => { Result = DialogResult.Yes; this.Close(); };
            this.Controls.Add(buttonYes);

            var buttonNo = new Button
            {
                Text = "No",
                DialogResult = DialogResult.No,
                Location = new Point(230, 110),
                Size = new Size(80, 30)
            };
            buttonNo.Click += (s, e) => { Result = DialogResult.No; this.Close(); };
            this.Controls.Add(buttonNo);

            this.AcceptButton = buttonYes;
            this.CancelButton = buttonNo;
            this.ActiveControl = buttonNo;
        }
    }
}
