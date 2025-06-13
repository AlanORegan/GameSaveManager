using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.ComponentModel;

namespace GameSaveManager
{
    public partial class GameConfigDialog : Form
    {
        public GameConfig saveConfig { get; private set; }

        private TextBox txtName = new TextBox();
        private Label lblName = new Label();

        private TextBox txtParentDirectory = new TextBox();
        private Label lblParentDirectory = new Label();
        private TextBox txtUserDirectory = new TextBox();
        private Label lblUserDirectory = new Label();

        private ComboBox cmbStrategy = new ComboBox();
        private Label lblStrategy = new Label();

        private TextBox txtGameDirectory = new TextBox();
        private Label lblGameDirectory = new Label();

        private TextBox txtNameFormat = new TextBox();
        private Label lblNameFormat = new Label();

        private TextBox txtDateFormat = new TextBox();
        private Label lblDateFormat = new Label();

        private TextBox txtVersionFormat = new TextBox();
        private Label lblVersionFormat = new Label();

        private TextBox txtPrefix = new TextBox();
        private Label lblPrefix = new Label();

        private TextBox txtParts = new TextBox();
        private Label lblParts = new Label();

        private TextBox txtSeparator = new TextBox();
        private Label lblSeparator = new Label();

        private TextBox txtSavePrefix = new TextBox();
        private Label lblSavePrefix = new Label();

        private TextBox txtSaveExtension = new TextBox();
        private Label lblSaveExtension = new Label();

        private TextBox txtMaxBackups = new TextBox();
        private Label lblMaxBackups = new Label();
        private TextBox txtRevertSuffix = new TextBox();
        private Label lblRevertSuffix = new Label();

        int verticalSpacing = 30;

        private Button btnSave;
        private Button btnCancel;

        private Label lblError = new Label(); // Add this line with other controls

        public GameConfigDialog(GameConfig? game = null, Size backupListBoxSize = default, Point backupListBoxLocation = default, Form owner = null)
        {
            InitializeComponent();
            saveConfig = game ?? new GameConfig();
            if (game != null)
            {
                txtName.Text = game.Name;
                txtParentDirectory.Text = game.ParentDirectory;
                txtUserDirectory.Text = game.UserDirectory;
                cmbStrategy.SelectedItem = game.StrategyType;
                txtGameDirectory.Text = game.GameDirectory;
                txtNameFormat.Text = game.NameFormat;
                txtDateFormat.Text = game.DateFormat;
                txtVersionFormat.Text = game.VersionFormat;
                txtPrefix.Text = game.SavePrefix;
                txtParts.Text = game.Parts;
                txtSeparator.Text = game.Separator;               

                // Save file Prefix and Extension are only configured for SubordinateUserFile strategy
                txtSavePrefix.Visible = game.StrategyType == BackupStrategy.SubordinateUserFile;
                lblSavePrefix.Visible = game.StrategyType == BackupStrategy.SubordinateUserFile;
                txtSaveExtension.Visible = game.StrategyType == BackupStrategy.SubordinateUserFile;
                lblSaveExtension.Visible = game.StrategyType == BackupStrategy.SubordinateUserFile;
                txtSavePrefix.Text = game.SaveFile?.Prefix;
                txtSaveExtension.Text = game.SaveFile?.Extension;
                txtMaxBackups.Text = game.MaxBackups.ToString();
                txtRevertSuffix.Text = game.RevertSuffix;
            }
            else
            {
                // Default to hidden for new games
                txtSavePrefix.Visible = false;
                lblSavePrefix.Visible = false;
                txtSaveExtension.Visible = false;
                lblSaveExtension.Visible = false;
            }

            // Set the owner and calculate the position based on the backup listbox bounds
            if (owner != null)
            {
                this.Owner = owner;
                if (backupListBoxSize != default && backupListBoxLocation != default)
                {
                    // Adjust size to account for window borders and title bar
                    int borderWidth = SystemInformation.FrameBorderSize.Width;
                    int titleBarHeight = SystemInformation.CaptionHeight;
                    this.Size = new Size(backupListBoxSize.Width + 3 * borderWidth + 2, 
                                         backupListBoxSize.Height + 2 * borderWidth - 1);
                    this.Location = new Point(owner.Location.X + backupListBoxLocation.X + 1, 
                                              owner.Location.Y + backupListBoxLocation.Y + titleBarHeight + 2 * borderWidth);
                }
            }
            this.Resize += SaveConfigDialog_Resize; // Add this line to subscribe to the Resize event
            SaveConfigDialog_Resize(this, EventArgs.Empty); // Trigger the resize event on startup
            txtNameFormat.Validating += TxtNameFormat_Validating;
            txtDateFormat.Validating += TxtDateFormat_Validating;
            txtVersionFormat.Validating += TxtVersionFormat_Validating;
            txtPrefix.Validating += TxtPrefix_Validating;
            txtSavePrefix.Validating += TxtSavePrefix_Validating;
            txtSaveExtension.Validating += TxtSaveExtension_Validating;
            txtParts.Validating += TxtParts_Validating;
            txtSeparator.Validating += TxtSeparator_Validating;
            txtMaxBackups.Validating += TxtMaxBackups_Validating;
            txtRevertSuffix.Validating += TxtRevertSuffix_Validating;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
            this.FormClosing += SaveConfigDialog_FormClosing;

            // Add handlers to clear error label when editing fields
            txtNameFormat.TextChanged += (s, e) => lblError.Text = "";
            txtDateFormat.TextChanged += (s, e) => lblError.Text = "";
            txtVersionFormat.TextChanged += (s, e) => lblError.Text = "";
            txtPrefix.TextChanged += (s, e) => lblError.Text = "";
            txtParts.TextChanged += (s, e) => lblError.Text = "";
            txtSeparator.TextChanged += (s, e) => lblError.Text = "";
            txtMaxBackups.TextChanged += (s, e) => lblError.Text = "";
            txtRevertSuffix.TextChanged += (s, e) => lblError.Text = "";
            txtSavePrefix.TextChanged += (s, e) => lblError.Text = "";
            txtSaveExtension.TextChanged += (s, e) => lblError.Text = "";
        }

        private void InitializeComponent()
        {
            this.Text = "Game Configuration";
            this.StartPosition = FormStartPosition.Manual;

            int rowOffset = 0;
            lblName.Text = "Name";
            lblName.Location = new Point(10, 10 + rowOffset * verticalSpacing);
            lblName.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblName);
            txtName.Location = new Point(120, 10);
            txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtName.Size = new Size(this.ClientSize.Width - 130, 20);
            this.Controls.Add(txtName);

            rowOffset++;
            lblParentDirectory.Text = "Parent Directory";
            lblParentDirectory.Location = new Point(10, 10 + rowOffset * verticalSpacing);
            lblParentDirectory.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblParentDirectory);
            txtParentDirectory.Location = new Point(120, 10 + rowOffset * verticalSpacing);
            txtParentDirectory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtParentDirectory.Size = new Size(this.ClientSize.Width - 130, 20);
            this.Controls.Add(txtParentDirectory);

            rowOffset++;
            lblUserDirectory.Text = "User Directory";
            lblUserDirectory.Location = new Point(10, 10 + rowOffset * verticalSpacing);
            lblUserDirectory.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblUserDirectory);
            txtUserDirectory.Location = new Point(120, 10 + rowOffset * verticalSpacing);
            txtUserDirectory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtUserDirectory.Size = new Size(this.ClientSize.Width - 130, 20);
            this.Controls.Add(txtUserDirectory);

            // Additional fields in two columns
            rowOffset++;
            lblStrategy.Text = "Strategy";
            lblStrategy.Top = 10 + rowOffset * verticalSpacing;
            lblStrategy.Left = 10;
            lblStrategy.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblStrategy);
            cmbStrategy.Top = 10 + rowOffset * verticalSpacing;
            cmbStrategy.Left = 120;
            cmbStrategy.Width = 160;
            cmbStrategy.Items.AddRange(new string[] { BackupStrategy.PeerDirectory, BackupStrategy.SubordinateUserFile });
            cmbStrategy.SelectedIndexChanged += CmbStrategy_SelectedIndexChanged;
            this.Controls.Add(cmbStrategy);

            lblGameDirectory.Text = "Game Directory (G)";
            lblGameDirectory.Top = 10 + rowOffset * verticalSpacing;
            lblGameDirectory.Left = this.ClientSize.Width / 2 + 10;
            lblGameDirectory.Width = 110;
            lblGameDirectory.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblGameDirectory);
            txtGameDirectory.Top = 10 + rowOffset * verticalSpacing;
            txtGameDirectory.Left = this.ClientSize.Width / 2 + 120;
            txtGameDirectory.Width = this.ClientSize.Width / 2 - 130;
            txtGameDirectory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtGameDirectory);

            rowOffset++;
            lblNameFormat.Text = "Name Format";
            lblNameFormat.Top = 10 + rowOffset * verticalSpacing;
            lblNameFormat.Left = 10;
            lblNameFormat.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblNameFormat);
            txtNameFormat.Top = 10 + rowOffset * verticalSpacing;
            txtNameFormat.Left = 120;
            txtNameFormat.Width = this.ClientSize.Width / 2 - 130;
            txtNameFormat.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtNameFormat);

            lblDateFormat.Text = "Date Format (D)";
            lblDateFormat.Top = 10 + rowOffset * verticalSpacing;
            lblDateFormat.Left = this.ClientSize.Width / 2 + 10;
            lblDateFormat.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblDateFormat);
            txtDateFormat.Top = 10 + rowOffset * verticalSpacing;
            txtDateFormat.Left = 120;
            txtDateFormat.Width = this.ClientSize.Width / 2 - 130;
            txtDateFormat.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtDateFormat);

            rowOffset++;
            lblVersionFormat.Text = "Version Format (V)";
            lblVersionFormat.Top = 10 + rowOffset * verticalSpacing;
            lblVersionFormat.Left = 10;
            lblVersionFormat.Width = 110;
            lblVersionFormat.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblVersionFormat);
            txtVersionFormat.Top = 10 + rowOffset * verticalSpacing;
            txtVersionFormat.Left = 120;
            txtVersionFormat.Width = this.ClientSize.Width / 2 - 130;
            txtVersionFormat.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtVersionFormat);

            lblPrefix.Text = "Prefix (P)";
            lblPrefix.Top = 10 + rowOffset * verticalSpacing;
            lblPrefix.Left = this.ClientSize.Width / 2 + 10;
            lblPrefix.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblPrefix);
            txtPrefix.Top = 10 + rowOffset * verticalSpacing;
            txtPrefix.Left = 120;
            txtPrefix.Width = this.ClientSize.Width / 2 - 130;
            this.Controls.Add(txtPrefix);

            rowOffset++;
            lblParts.Text = "Parts";
            lblParts.Top = 10 + rowOffset * verticalSpacing;
            lblParts.Left = 10;
            lblParts.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblParts);
            txtParts.Top = 10 + rowOffset * verticalSpacing;
            txtParts.Left = 120;
            txtParts.Width = this.ClientSize.Width / 2 - 130;
            txtParts.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtParts);

            lblSeparator.Text = "Separator (s)";
            lblSeparator.Top = 10 + rowOffset * verticalSpacing;
            lblSeparator.Left = this.ClientSize.Width / 2 + 10;
            lblSeparator.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblSeparator);
            txtSeparator.Top = 10 + rowOffset * verticalSpacing;
            txtSeparator.Left = 120;
            txtSeparator.Width = this.ClientSize.Width / 2 - 130;
            txtSeparator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtSeparator);

            rowOffset++;
            lblMaxBackups.Text = "Max Backups";
            lblMaxBackups.Top = 10 + rowOffset * verticalSpacing;
            lblMaxBackups.Left = 10;
            lblMaxBackups.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblMaxBackups);
            txtMaxBackups.Top = 10 + rowOffset * verticalSpacing;
            txtMaxBackups.Left = 120;
            txtMaxBackups.Width = this.ClientSize.Width / 2 - 130;
            txtMaxBackups.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtMaxBackups);

            lblRevertSuffix.Text = "Revert Suffix";
            lblRevertSuffix.Top = 10 + rowOffset * verticalSpacing;
            lblRevertSuffix.Left = this.ClientSize.Width / 2 + 10;
            lblRevertSuffix.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblRevertSuffix);
            txtRevertSuffix.Top = 10 + rowOffset * verticalSpacing;
            txtRevertSuffix.Left = 120;
            txtRevertSuffix.Width = this.ClientSize.Width / 2 - 130;
            txtRevertSuffix.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtRevertSuffix);

            // Fields for SubordinateUserFile strategy
            rowOffset++;
            lblSavePrefix.Text = "Save Prefix";
            lblSavePrefix.Location = new Point(10, 10 + rowOffset * verticalSpacing);
            lblSavePrefix.Visible = false;  // Initially hidden
            lblSavePrefix.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblSavePrefix);
            txtSavePrefix.Location = new Point(120, 10 + rowOffset * verticalSpacing);
            txtSavePrefix.Size = new Size(this.ClientSize.Width / 2 - 130, 20);
            txtSavePrefix.Visible = false;  // Initially hidden
            this.Controls.Add(txtSavePrefix);

            lblSaveExtension.Text = "Save Extension (E)";
            lblSaveExtension.Location = new Point(this.ClientSize.Width / 2 + 10, 10 + rowOffset * verticalSpacing);
            lblSaveExtension.Visible = false;
            lblSaveExtension.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblSaveExtension);
            txtSaveExtension.Location = new Point(this.ClientSize.Width / 2 + 120, 10 + rowOffset * verticalSpacing);
            txtSaveExtension.Size = new Size(this.ClientSize.Width / 2 - 130, 20); // Adjusted width to use half the row
            txtSaveExtension.Visible = false;
            txtSaveExtension.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtSaveExtension);

            rowOffset++;
            btnSave = new Button { Text = "Save", Top = 10 + rowOffset * verticalSpacing, Left = 120 };
            this.Controls.Add(btnSave);
            btnCancel = new Button { Text = "Cancel", Top = 10 + rowOffset * verticalSpacing, Left = 220 };
            this.Controls.Add(btnCancel);

            // Add error label at the bottom
            rowOffset++;
            lblError = new Label
            {
                Text = "",
                Location = new Point(10, 10 + rowOffset * verticalSpacing),
                Size = new Size(this.ClientSize.Width - 20, 2 * verticalSpacing),
                ForeColor = Color.Red,
                AutoSize = false,
                MaximumSize = new Size(0, 0), // Allow unlimited width
                AutoEllipsis = false, // No ellipsis, allow full text
                Visible = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            this.Controls.Add(lblError);
        }

        private void CmbStrategy_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isSubordinateUserFile = cmbStrategy.SelectedItem?.ToString() == BackupStrategy.SubordinateUserFile;
            lblSavePrefix.Visible = isSubordinateUserFile;
            txtSavePrefix.Visible = isSubordinateUserFile;
            lblSaveExtension.Visible = isSubordinateUserFile;
            txtSaveExtension.Visible = isSubordinateUserFile;
        }

        private void SaveConfigDialog_Resize(object sender, EventArgs e)
        {
            int formWidth = this.ClientSize.Width;
            int halfFormWidth = formWidth / 2; // Adjusted to use half the row width
            int txtColumnWidth = (halfFormWidth > 140) ? halfFormWidth - 130 : 10; // Adjusted to use half the row width
            int rightLabelStart = halfFormWidth + 10;
            int rightBoxStart = halfFormWidth + 120;

            // resize fields presented 1 per row
            txtName.Size = new Size(formWidth - 130, 20);
            txtParentDirectory.Size = new Size(formWidth - 130, 20);
            txtUserDirectory.Size = new Size(formWidth - 130, 20);

            // resize fields presented 2 per row
            cmbStrategy.Size = new Size(txtColumnWidth, 20);

            int rowOffset = 3;
            lblGameDirectory.Location = new Point(rightLabelStart, 10 + rowOffset * verticalSpacing);
            txtGameDirectory.Location = new Point(rightBoxStart, 10 + rowOffset * verticalSpacing);
            txtGameDirectory.Size = new Size(txtColumnWidth, 20);

            txtNameFormat.Size = new Size(txtColumnWidth, 20);

            rowOffset++;
            lblDateFormat.Location = new Point(rightLabelStart, 10 + rowOffset * verticalSpacing);
            txtDateFormat.Location = new Point(rightBoxStart, 10 + rowOffset * verticalSpacing);
            txtDateFormat.Size = new Size(txtColumnWidth, 20);

            txtVersionFormat.Size = new Size(txtColumnWidth, 20);

            rowOffset++;
            lblPrefix.Location = new Point(rightLabelStart, 10 + rowOffset * verticalSpacing);
            txtPrefix.Location = new Point(rightBoxStart, 10 + rowOffset * verticalSpacing);
            txtPrefix.Size = new Size(txtColumnWidth, 20);

            txtParts.Size = new Size(txtColumnWidth, 20);

            rowOffset++;
            lblSeparator.Location = new Point(rightLabelStart, 10 + rowOffset * verticalSpacing);
            txtSeparator.Location = new Point(rightBoxStart, 10 + rowOffset * verticalSpacing);
            txtSeparator.Size = new Size(txtColumnWidth, 20);

            txtParts.Size = new Size(txtColumnWidth, 20);

            rowOffset++;
            lblRevertSuffix.Location = new Point(rightLabelStart, 10 + rowOffset * verticalSpacing);
            txtRevertSuffix.Location = new Point(rightBoxStart, 10 + rowOffset * verticalSpacing);
            txtRevertSuffix.Size = new Size(txtColumnWidth, 20);

            txtMaxBackups.Size = new Size(txtColumnWidth, 20);

            rowOffset++;
            lblSaveExtension.Location = new Point(rightLabelStart, 10 + rowOffset * verticalSpacing);
            txtSaveExtension.Location = new Point(rightBoxStart, 10 + rowOffset * verticalSpacing);
            txtSaveExtension.Size = new Size(txtColumnWidth, 20);

            txtSavePrefix.Size = new Size(txtColumnWidth, 20);

            rowOffset++;
            // Buttons are not resized.

            rowOffset++;
            lblError.Location = new Point(10, 10 + rowOffset * verticalSpacing);
            lblError.Size = new Size(formWidth - 20, 2 * verticalSpacing); // Increase height for two lines
        }

        // --- Validation methods for each field ---
        private (bool, string) ValidateNameFormat() =>
            GameConfig.IsValidNameFormat(txtNameFormat.Text)
                ? (true, "")
                : (false, "Invalid NameFormat. It can only contain the characters PsDVTRE, each capital letter can only occur once, and 's' can be present only once between any two capitals.");

        private (bool, string) ValidateDateFormat() =>
            (!txtNameFormat.Text.Contains("D") || GameConfig.IsValidDateFormat(txtDateFormat.Text))
                ? (true, "")
                : (false, "Invalid DateFormat. It must be a valid format string for DateTime.");

        private (bool, string) ValidateVersionFormat() =>
            (!txtNameFormat.Text.Contains("V") || GameConfig.IsValidVersionFormat(txtVersionFormat.Text))
                ? (true, "")
                : (false, "Invalid VersionFormat. It must be of the form '000.00' or less digits, and having a decimal is optional.");

        private (bool, string) ValidatePrefix() =>
            (!txtNameFormat.Text.Contains("P") || SaveFile.IsValidPrefix(txtPrefix.Text))
                ? (true, "")
                : (false, "Invalid SavePrefix. It must be up to 20 alphanumeric characters that are valid in a filename, excluding the directory level separator '/'.");

        private (bool, string) ValidateParts() =>
            GameConfig.IsValidParts(txtParts.Text)
                ? (true, "")
                : (false, "Invalid Parts. It can contain any combination of -+^& in any order, all of which are optional.");

        private (bool, string) ValidateSeparator() =>
            (!txtNameFormat.Text.Contains("s") || GameConfig.IsValidSeparator(txtSeparator.Text))
                ? (true, "")
                : (false, "Invalid Separator. It can be up to 3 character that are valid in a filename.");

        private (bool, string) ValidateMaxBackups() =>
            (int.TryParse(txtMaxBackups.Text, out int maxBackups) && maxBackups > 0)
                ? (true, "")
                : (false, "Invalid MaxBackups. It must be a positive integer.");

        private (bool, string) ValidateRevertSuffix() =>
            !string.IsNullOrEmpty(txtRevertSuffix.Text)
                ? (true, "")
                : (false, "Invalid RevertSuffix. It cannot be empty.");

        private (bool, string) ValidateSavePrefix() =>
            (!txtSavePrefix.Visible || SaveFile.IsValidPrefix(txtSavePrefix.Text))
                ? (true, "")
                : (false, "Invalid SavePrefix. It can be up to 20 alphanumeric characters that are valid in a filename, excluding the directory level separator '/'.");

        private (bool, string) ValidateSaveExtension() =>
            (!txtSaveExtension.Visible || SaveFile.IsValidExtension(txtSaveExtension.Text))
                ? (true, "")
                : (false, "Invalid Extension. It can be up to 3 characters that are valid in a filename.");

        // --- Generic wrapper for Validating events ---
        private void GenericValidating(Func<(bool, string)> validator, CancelEventArgs e)
        {
            var (valid, msg) = validator();
            if (!valid)
            {
                lblError.Text = msg;
                lblError.Visible = true;
                lblError.BringToFront(); // Ensure error label is not hidden behind other controls
                this.Refresh(); // Force redraw in case of UI glitches
            }
            // Do not clear lblError here if valid, so the user always sees the last error until a successful save or explicit clear.
        }

        private void TxtNameFormat_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidateNameFormat, e);
        private void TxtDateFormat_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidateDateFormat, e);
        private void TxtVersionFormat_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidateVersionFormat, e);
        private void TxtPrefix_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidatePrefix, e);
        private void TxtParts_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidateParts, e);
        private void TxtSeparator_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidateSeparator, e);
        private void TxtMaxBackups_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidateMaxBackups, e);
        private void TxtRevertSuffix_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidateRevertSuffix, e);
        private void TxtSavePrefix_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidateSavePrefix, e);
        private void TxtSaveExtension_Validating(object sender, System.ComponentModel.CancelEventArgs e) => GenericValidating(ValidateSaveExtension, e);

        // --- Blocking validation for Save ---
        private bool ValidateAllFieldsAndShowErrors()
        {
            var validations = new (Func<(bool, string)> validator, Control ctrl)[]
            {
                (ValidateNameFormat, txtNameFormat),
                (ValidateDateFormat, txtDateFormat),
                (ValidateVersionFormat, txtVersionFormat),
                (ValidatePrefix, txtPrefix),
                (ValidateParts, txtParts),
                (ValidateSeparator, txtSeparator),
                (ValidateMaxBackups, txtMaxBackups),
                (ValidateRevertSuffix, txtRevertSuffix),
                (ValidateSavePrefix, txtSavePrefix),
                (ValidateSaveExtension, txtSaveExtension)
            };

            foreach (var (validator, ctrl) in validations)
            {
                var (valid, msg) = validator();
                if (!valid)
                {
                    lblError.Text = msg;
                    lblError.Visible = true;
                    ctrl.Focus();
                    return false;
                }
            }
            lblError.Text = "";
            lblError.Visible = false;
            return true;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateAllFieldsAndShowErrors())
                return;

            saveConfig.Name = txtName.Text;
            saveConfig.ParentDirectory = txtParentDirectory.Text;
            saveConfig.UserDirectory = txtUserDirectory.Text;
            saveConfig.StrategyType = cmbStrategy.SelectedItem?.ToString();
            saveConfig.GameDirectory = txtGameDirectory.Text;
            saveConfig.NameFormat = txtNameFormat.Text;
            saveConfig.DateFormat = txtDateFormat.Text;
            saveConfig.VersionFormat = txtVersionFormat.Text;
            saveConfig.SavePrefix = txtPrefix.Text;
            saveConfig.Parts = txtParts.Text;
            saveConfig.Separator = txtSeparator.Text;
            saveConfig.MaxBackups = int.Parse(txtMaxBackups.Text);
            saveConfig.RevertSuffix = txtRevertSuffix.Text;

            // Only set SaveFile for SubordinateUserFile strategy
            if (saveConfig.StrategyType == BackupStrategy.SubordinateUserFile)
            {
                saveConfig.SaveFile = new SaveFile
                {
                    Prefix = txtSavePrefix.Text,
                    Extension = txtSaveExtension.Text
                };
            }
            else
            {
                saveConfig.SaveFile = null;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void SaveConfigDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.Cancel)
            {
                e.Cancel = false;
            }
            else
            {
                // Trigger validation manually
                this.ValidateChildren();
            }
        }
    }
}