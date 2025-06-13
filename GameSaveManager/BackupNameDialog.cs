using System;
using System.Windows.Forms;

namespace GameSaveManager
{
    public partial class BackupNameDialog : Form
    {
        private TextBox txtBackupName;
        private Button btnOK;
        private Button btnCancel;

        public string BackupName => txtBackupName.Text;

        public BackupNameDialog(string initialBackupName, Size backupListBoxSize, Point backupListBoxLocation, Form owner)
        {
            InitializeComponent();
            txtBackupName.Text = initialBackupName;

            // Set the owner and calculate the position based on the backup listbox bounds
            if (owner != null)
            {
                this.Owner = owner;
                if (backupListBoxSize != default && backupListBoxLocation != default)
                {
                    // Adjust size to account for window borders and title bar
                    int borderWidth = SystemInformation.FrameBorderSize.Width;
                    int titleBarHeight = SystemInformation.CaptionHeight;
                    this.Size = new Size(backupListBoxSize.Width + 3 * borderWidth + 2, this.Height);
                    this.Location = new Point(owner.Location.X + backupListBoxLocation.X + 1, 
                                              owner.Location.Y + backupListBoxLocation.Y + titleBarHeight + 2 * borderWidth + (backupListBoxSize.Height - this.Height)/2);
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Enter Backup Name";
            this.Size = new System.Drawing.Size(400, 150);
            this.StartPosition = FormStartPosition.Manual; // Change to Manual to prevent auto-centering

            var lblBackupName = new Label
            {
                Text = "New Backup Name:",
                Location = new System.Drawing.Point(10, 23), // Adjusted Y position to align with textbox
                Size = new System.Drawing.Size(120, 20) // Increased width to ensure all text is visible
            };
            this.Controls.Add(lblBackupName);

            txtBackupName = new TextBox
            {
                Location = new System.Drawing.Point(140, 20), // Adjusted X position to align with the new label width
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Size = new System.Drawing.Size(this.ClientSize.Width - 150, 20) // Adjusted width to account for the new label width
            };
            this.Controls.Add(txtBackupName);

            btnOK = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(140, 60), // Adjusted X position to align with the new label width
                DialogResult = DialogResult.OK
            };
            this.Controls.Add(btnOK);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(240, 60), // Adjusted X position to align with the new label width
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            this.Resize += BackupNameDialog_Resize;
        }

        private void BackupNameDialog_Resize(object sender, EventArgs e)
        {
            txtBackupName.Size = new System.Drawing.Size(this.ClientSize.Width - 150, 20); // Adjusted width to account for the new label width
        }
    }
}