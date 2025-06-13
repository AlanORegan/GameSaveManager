using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace GameSaveManager
{
    public partial class MainForm : Form
    {
        private List<GameConfig> games = new List<GameConfig>();
        private List<SavedGame> backups = new List<SavedGame>();
        public ListBox listGames, listBackups;
        private bool ignoreGameFocusChange = false;
        private Label lblError, lblGames, lblBackups;
        public GameManagerSettings gameManagerSettings;
        private Label lblMonitor, lblLastSave, lblFileCount;
        private Label lblMonitorValue, lblLastSaveValue, lblFileCountValue;
        private Label lblLastBackup, lblLastBackupValue, lblLastAuto, lblLastAutoValue;
        private Panel statusPanel;
        private int fileCounter = 0;
        private GameConfig previousGame = null;
        private Button btnBackupStatus;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public MainForm()
        {
            SynchronizationContextManager.Initialize();
            SetProcessDPIAware();
            LoadManagerSettings();
            InitializeComponent();
            SetWindowPosition();
            LoadGameConfigs();

            // Select the last selected game if available
            if (!string.IsNullOrEmpty(gameManagerSettings.LastSelectedGame))
            {
                foreach (GameConfig game in listGames.Items)
                {
                    if (string.Equals(game.Name, gameManagerSettings.LastSelectedGame, StringComparison.OrdinalIgnoreCase))
                    {
                        listGames.SelectedItem = game;
                        break;
                    }
                }
            }

            MainForm_Resize(this, EventArgs.Empty); // Set initial sizes based on form width
        }

        private void SetWindowPosition()
        {
            var screens = Screen.AllScreens;
            if (gameManagerSettings.WindowPosition.ScreenIndex >= 0 && gameManagerSettings.WindowPosition.ScreenIndex < screens.Length)
            {
                var screen = screens[gameManagerSettings.WindowPosition.ScreenIndex];
                this.StartPosition = FormStartPosition.Manual;
                var newLocation = new Point(gameManagerSettings.WindowPosition.X, gameManagerSettings.WindowPosition.Y);

                // Ensure the window is fully visible on the screen
                if (newLocation.X < screen.Bounds.Left || newLocation.X > screen.Bounds.Right - gameManagerSettings.WindowPosition.Width ||
                    newLocation.Y < screen.Bounds.Top || newLocation.Y > screen.Bounds.Bottom - gameManagerSettings.WindowPosition.Height)
                {
                    this.Location = new Point(screen.Bounds.Right - gameManagerSettings.WindowPosition.Width, screen.Bounds.Top);
                }
                else
                {
                    this.Location = newLocation;
                }
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
                this.Location = new Point(gameManagerSettings.WindowPosition.X, gameManagerSettings.WindowPosition.Y);
            }
            this.Size = new Size(gameManagerSettings.WindowPosition.Width, gameManagerSettings.WindowPosition.Height);
        }

        private void InitializeComponent()
        {
            // Form setup
            this.Text = "Game Save Manager";
            this.Size = new Size(gameManagerSettings.WindowPosition.Width, gameManagerSettings.WindowPosition.Height);
            this.Location = new Point(gameManagerSettings.WindowPosition.X, gameManagerSettings.WindowPosition.Y);
            this.Resize += MainForm_Resize;

            // Menu setup
            var menu = new MenuStrip();
            var addGame = new ToolStripMenuItem("Add", null, AddGame_Click);
            var editGame = new ToolStripMenuItem("Edit", null, EditGame_Click);
            var deleteGame = new ToolStripMenuItem("Delete", null, DeleteGame_Click);
            var backupGame = new ToolStripMenuItem("Backup", null, BackupGame_Click);
            var restoreGame = new ToolStripMenuItem("Restore", null, RestoreGame_Click);
            var revertGame = new ToolStripMenuItem("Revert", null, RevertGame_Click);
            var renameBackup = new ToolStripMenuItem("Rename", null, RenameBackup_Click);
            var helpMenu = new ToolStripMenuItem("Help", null, HelpMenu_Click) { Alignment = ToolStripItemAlignment.Right };
            menu.Items.AddRange(new ToolStripItem[] { addGame, editGame, deleteGame, backupGame, restoreGame, revertGame, renameBackup, helpMenu });
            this.Controls.Add(menu);

            // Labels
            lblGames = new Label { Text = "Games", Location = new Point(10, 30), Size = new Size(200, 20) };
            this.Controls.Add(lblGames);
            // Fix: Point only takes 2 arguments (x, y)
            lblBackups = new Label { Text = "Backups", Location = new Point(220, 30), Size = new Size(300, 20) };
            this.Controls.Add(lblBackups);

            // Left panel for games list
            listGames = new ListBox { Location = new Point(10, 50), Size = new Size(200, 400) };
            listGames.SelectedIndexChanged += ListGames_SelectedIndexChanged;
            listGames.DisplayMember = "Name"; // Request the "Name" property is used to display the GameConfig object in the ListBox
            this.Controls.Add(listGames);

            // Right panel for backups list
            listBackups = new ListBox { Location = new Point(220, 50), Size = new Size(300, 400) };
            this.Controls.Add(listBackups);

            // Label for error messages
            lblError = new Label
            {
                Location = new Point(10, 490),
                Size = new Size(510, 40), // Increase height to 40 for two lines
                ForeColor = Color.Red,
                AutoSize = false,
                MaximumSize = new Size(this.ClientSize.Width - 20, 0), // Set maximum width to form width minus padding
                AutoEllipsis = true // Enable text wrapping
            };
            this.Controls.Add(lblError);

            // ****************************************************************************************************
            // Status bar setup
            // ****************************************************************************************************

            statusPanel = new Panel
            {
                Size = new Size(this.ClientSize.Width, 30),
                Location = new Point(0, this.ClientSize.Height - 30),
                BackColor = SystemColors.ControlLightLight // Match the title bar background color
            };
            this.Controls.Add(statusPanel);
            this.Controls.SetChildIndex(statusPanel, 0); // Bring statusPanel to the front

            int statusPanelHeight = statusPanel.Height;
            int labelVerticalPosition = (statusPanelHeight - 15) / 2; // Center vertically assuming label height is 15

            lblMonitor = new Label { Text = "Monitor:", Location = new Point(10, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };
            lblMonitor.Click += MonitorLabel_Click;
            lblMonitorValue = new Label { Text = "Off", Location = new Point(70, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };

            // Backup status button
            btnBackupStatus = new Button
            {
                Size = new Size(20, 20),
                BackColor = Color.Gray,
                Location = new Point(150, labelVerticalPosition - 3) // Adjust vertical position to align with labels
            };
            btnBackupStatus.Click += BtnBackupStatus_Click;
            statusPanel.Controls.Add(btnBackupStatus);

            lblLastSave = new Label { Text = "Last Save:", Location = new Point(200, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };
            lblLastSaveValue = new Label { Text = "00:00:00", Location = new Point(270, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };

            lblLastBackup = new Label { Text = "Last Backup:", Location = new Point(350, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };
            lblLastBackupValue = new Label { Text = "00:00:00", Location = new Point(430, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };

            lblLastAuto = new Label { Text = "Last Auto:", Location = new Point(510, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };
            lblLastAutoValue = new Label { Text = "00:00:00", Location = new Point(580, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };

            lblFileCount = new Label { Text = "File Count:", Location = new Point(660, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };
            lblFileCountValue = new Label { Text = "000", Location = new Point(740, labelVerticalPosition), ForeColor = SystemColors.ControlDark, AutoSize = true };

            statusPanel.Controls.Add(lblMonitor);
            statusPanel.Controls.Add(lblMonitorValue);
            statusPanel.Controls.Add(lblLastSave);
            statusPanel.Controls.Add(lblLastSaveValue);
            statusPanel.Controls.Add(lblLastBackup);
            statusPanel.Controls.Add(lblLastBackupValue);
            statusPanel.Controls.Add(lblLastAuto);
            statusPanel.Controls.Add(lblLastAutoValue);
            statusPanel.Controls.Add(lblFileCount);
            statusPanel.Controls.Add(lblFileCountValue);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            int formWidth = this.ClientSize.Width;
            int formHeight = this.ClientSize.Height;
            int gamesWidth = (int)(formWidth * 0.2);
            int backupsWidth = formWidth - gamesWidth - 30;
            int listHeight = formHeight - 140; // Adjust height based on remaining space

            lblGames.Size = new Size(gamesWidth, 20);
            listGames.Size = new Size(gamesWidth, listHeight);

            lblBackups.Location = new Point(gamesWidth + 20, 30);
            lblBackups.Size = new Size(backupsWidth, 20);
            listBackups.Location = new Point(gamesWidth + 20, 50);
            listBackups.Size = new Size(backupsWidth, listHeight);

            lblError.Location = new Point(10, formHeight - 70); // Adjust location to account for increased height
            lblError.Size = new Size(formWidth - 20, formHeight - lblError.Location.Y - 10); // Set size to form width minus padding and height to remaining space

            // Adjust the status panel location on resize
            statusPanel.Location = new Point(0, formHeight - 30);
            statusPanel.Size = new Size(formWidth, 30);

            // Ensure visibility of labels based on monitor status
            UpdateStatusBar();

            // Update manager settings with the new size and position using outer bounds
            gameManagerSettings.WindowPosition.X = this.Bounds.X;
            gameManagerSettings.WindowPosition.Y = this.Bounds.Y;
            gameManagerSettings.WindowPosition.Width = this.Bounds.Width;
            gameManagerSettings.WindowPosition.Height = this.Bounds.Height;
            gameManagerSettings.WindowPosition.ScreenIndex = Array.IndexOf(Screen.AllScreens, Screen.FromControl(this)); // Save the screen index

            // Save the updated settings
            gameManagerSettings.Save();
        }

        public void LoadGameConfigs()
        {
            games = GameConfigManager.LoadGameConfigs();
            RefreshListOfGames();
        }

        private void RefreshListOfGames()
        {
            // Sort games by Name before displaying
            games = games.OrderBy(g => g.Name).ToList();

            // Check if a game is selected
            GameConfig selectedGame = listGames.SelectedItem as GameConfig;
            bool gameIsSelected = selectedGame != null;

            // reset the list datasource to update the list
            ignoreGameFocusChange = true; // Prevent the SelectedIndexChanged event from executing anything
            listGames.DataSource = null;
            listGames.DataSource = games;
            listGames.DisplayMember = "Name"; // Ensure the "Name" property is used to display list items
            listGames.SelectedIndex = -1; // Prevent the first item from being selected by default
            ignoreGameFocusChange = false; // Re-enable the SelectedIndexChanged event

            // Check if a game was selected before the update
            if (gameIsSelected)
            {
                // find the selected game and reselect it
                foreach (GameConfig game in listGames.Items)
                {
                    if (game == selectedGame)
                    {
                        listGames.SelectedItem = game;
                        break;
                    }
                }
            }
            else
            {
                // Clear the list of backups if no game is selected
                backups.Clear();
                RefreshListOfBackups();
            }

        }

        private void RefreshListOfBackups()
        {
            // Check if a backup is selected
            SavedGame selectedBackup = listBackups.SelectedItem as SavedGame;
            bool backupIsSelected = selectedBackup != null;

            // reset the list datasource to update the list
            listBackups.DataSource = null;
            listBackups.DataSource = backups;
            listBackups.SelectedIndex = -1; // Prevent the first item from being selected by default

            // Ensure the "Name" property is used to display list items
            listBackups.DisplayMember = "Name";

            // Check if a backup was selected before the update
            if (backupIsSelected)
            {
                // find the selected backup and reselect it
                foreach (SavedGame backup in listBackups.Items)
                {
                    if (backup == selectedBackup)
                    {
                        listBackups.SelectedItem = backup;
                        break;
                    }
                }
            }
        }

        private void AddGame_Click(object sender, EventArgs e)
        {
            MessageUtils.ClearMessage(lblError);

            GameConfig initialConfig = null;

            // If a game is selected, ask if the user wants to prepopulate
            if (listGames.SelectedItem is GameConfig selectedConfig)
            {
                using (var dialog = new CenteredYesNoDialog(
                    $"Would you like to prepopulate the new game from the selected game's configuration?\nSelected game: {selectedConfig.Name}",
                    "Prepopulate Game Config"))
                {
                    var result = dialog.ShowDialog(this);
                    if (dialog.Result == DialogResult.Yes)
                    {
                        initialConfig = selectedConfig.CloneWithCopyName();
                      }
                }
            }

            var dialog2 = new GameConfigDialog(initialConfig, listBackups.Size, listBackups.Location, this);
            if (dialog2.ShowDialog() == DialogResult.OK)
            {
                // Add the new game configuration to the list
                games.Add(dialog2.saveConfig);

                // Sort games by Name after adding
                games = games.OrderBy(g => g.Name).ToList();

                // Save the updated game configurations
                GameConfigManager.SaveGameConfigs(games);

                // Refresh the list of games
                RefreshListOfGames();

                // Keep focus on the newly added game
                listGames.SelectedItem = dialog2.saveConfig;
                MessageUtils.SetUserMessage(lblError, "Game added successfully.");
            }
        }

        private void EditGame_Click(object sender, EventArgs e)
        {
            MessageUtils.ClearMessage(lblError);
            if (listGames.SelectedItem is GameConfig selectedConfig)
            {
                var dialog = new GameConfigDialog(selectedConfig, listBackups.Size, listBackups.Location, this);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Sort games by Name after editing
                    games = games.OrderBy(g => g.Name).ToList();

                    // Update the selected game configuration with the edited values
                    GameConfigManager.SaveGameConfigs(games);

                    // Refresh the list of games
                    RefreshListOfGames();

                    MessageUtils.SetUserMessage(lblError, "Game edited successfully.");
                }
            }
        }

        private void DeleteGame_Click(object sender, EventArgs eventArgs)
        {
            MessageUtils.ClearMessage(lblError);

            // If a backup is selected, delete it instead of the game
            if (listBackups.SelectedItem is SavedGame selectedBackup && listGames.SelectedItem is GameConfig game)
            {
                using (var dialog = new CenteredYesNoDialog(
                    $"Are you sure you want to delete this backup?\nBackup: {selectedBackup.Name}",
                    selectedBackup.Name))
                {
                    var result = dialog.ShowDialog(this);
                    if (dialog.Result == DialogResult.Yes)
                    {
                        // Delete the backup through the game
                        game.DeleteBackup(selectedBackup, lblError);

                        // Refresh the list of backups
                        game.LoadBackups(backups, lblError);
                        RefreshListOfBackups();

                        MessageUtils.SetUserMessage(lblError, "Backup deleted successfully.");
                    }
                }
                return;
            }

            // Otherwise, handle game deletion as before
            if (listGames.SelectedItem is GameConfig selectedConfig)
            {
                using (var dialog = new CenteredYesNoDialog(
                    $"Are you sure you want to delete this game config?\nGame: {selectedConfig.Name}",
                    selectedConfig.Name))
                {
                    var result = dialog.ShowDialog(this);
                    if (dialog.Result == DialogResult.Yes)
                    {
                        // Remove the selected game configuration from the list
                        games.Remove(selectedConfig);

                        // Persist the updated list of game configurations
                        GameConfigManager.SaveGameConfigs(games);

                        // Refresh the list of games
                        RefreshListOfGames();

                        MessageUtils.SetUserMessage(lblError, "Game deleted successfully.");
                    }
                }
            }
        }

        private void HelpMenu_Click(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;
            MessageBox.Show("Help clicked");
        }

        private void ListGames_SelectedIndexChanged(object sender, EventArgs e)
        {
            GameConfig game = listGames.SelectedItem as GameConfig;

            bool gameIsSelected = game != null;
            bool gameHasChanged = previousGame != game;

            // Check if the event was disabled
            if (ignoreGameFocusChange)
            {
                return;
            }

            // Clear previous messages
            MessageUtils.ClearMessage(lblError);

            // If a game is selected, load its backups
            if (gameIsSelected && gameHasChanged)
            {
                // Deregister change events from the previous game if different
                if (previousGame != null)
                {
                    previousGame.LoseFocus(new System.ComponentModel.PropertyChangedEventHandler(Game_PropertyChanged));
                }

                // Register for change events for the selected game
                if (game != null)
                {
                    game.GainFocus(Game_PropertyChanged);
                }

                // Load the backups for the newly selected game
                game.LoadBackups(backups, lblError);

                // Refresh the listbox view of the selected game's backups
                RefreshListOfBackups();

                // Store the game to enable monitoring to stop when the focus changes
                previousGame = game;

                // Save the last selected game to settings
                gameManagerSettings.LastSelectedGame = game.Name;
                gameManagerSettings.Save();
            }

            // If the same game is selected, clear the selected backup
            if (gameIsSelected && !gameHasChanged)
            {
                listBackups.SelectedIndex = -1; // Clear the selected backup
            }

            // Refresh the Status bar
            UpdateStatusBar();
        }

        private void BackupGame_Click(object sender, EventArgs e)
        {
            // Clear previous messages
            MessageUtils.ClearMessage(lblError);

            // Return if a game is not selected
            if (listGames.SelectedItem is not GameConfig game)
            {
                MessageUtils.SetErrorMessage(lblError, "Select a game before requesting Backup.");
                return;
            }

            // Get template backup name from selection if available
            string newName = string.Empty;
            if (listBackups.SelectedItem is SavedGame selectedBackup)
            {
                newName = selectedBackup.Name;
            }

            // Prepare a new save for the game using the template tag
            SavedGame backup = game.GetNewBackup(newName, lblError);

            // Present the proposed backup name to the user for editing
            using (var dialog = new BackupNameDialog(backup.Tag, listBackups.Size, listBackups.Location, this))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string userInput = dialog.BackupName;

                    // Backup the game
                    game.BackupGame(backup, userInput, lblError);
                    MessageUtils.SetUserMessage(lblError, $"Backup at {DateTime.Now:HH:mm} completed successfully.");

                    // Update the list of backups
                    game.LoadBackups(backups, lblError);
                    RefreshListOfBackups();
                }
            }
        }

        private void RestoreGame_Click(object sender, EventArgs e)
        {
            // Clear previous messages
            MessageUtils.ClearMessage(lblError);

            // Check if a game is selected
            if (listGames.SelectedItem is not GameConfig game)
            {
                MessageUtils.SetErrorMessage(lblError, "Select a game and optionally an existing backup before requesting Restore.");
                return;
            }
            else
            {
                SavedGame backup;

                if (listBackups.SelectedItem == null)
                {
                    // If no backup selected, restore the most recent backup
                    backup = backups[0];
                }
                else
                {
                    // Get the selected backup
                    backup = listBackups.SelectedItem as SavedGame;
                }

                // Restore the backup of the game
                game.RestoreGame(backup, lblError);
                MessageUtils.SetUserMessage(lblError, $"Restore at {DateTime.Now:HH:mm} completed successfully from backup ({backup.Name}).");

                // Update the list of backups
                game.LoadBackups(backups, lblError);
                RefreshListOfBackups();
            }
        }

        private void RenameBackup_Click(object sender, EventArgs e)
        {
            MessageUtils.ClearMessage(lblError);

            // Check if a game is selected
            if (listGames.SelectedItem is not GameConfig game)
            {
                MessageUtils.SetErrorMessage(lblError, "Select a game and an existing backup before requesting Rename.");
                return;
            }

            SavedGame backup;
            // Check if a backup is selected
            if (listBackups.SelectedItem == null)
            {
                MessageUtils.SetErrorMessage(lblError, "Select a game backup before requesting Rename. If you want to Rename a game, use the Edit button.");
                return;
            }
            else
            {
                backup = listBackups.SelectedItem as SavedGame;
            }

            // Extract the backup name from the selected item
            string oldBackupName = backup.Name;

            // Present the current backup name to the user for editing
            using (var dialog = new BackupNameDialog(backup.Tag, listBackups.Size, listBackups.Location, this))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string userInput = dialog.BackupName;

                    if (userInput == oldBackupName)
                    {
                        MessageUtils.SetErrorMessage(lblError, "The new backup name is the same as the old one.");
                        return;
                    }

                    // Rename the game backup
                    game.RenameBackup(backup, userInput, lblError);
                    MessageUtils.SetUserMessage(lblError, $"Rename at {DateTime.Now:HH:mm} completed successfully, from ({oldBackupName}) to ({backup.Name}).");

                    // Update the list of backups
                    game.LoadBackups(backups, lblError);
                    RefreshListOfBackups();
                }
            }
        }

        private void RevertGame_Click(object sender, EventArgs e)
        {
            // Clear previous messages
            MessageUtils.ClearMessage(lblError);

            // Check if a game is selected
            if (listGames.SelectedItem is not GameConfig game)
            {
                MessageUtils.SetErrorMessage(lblError, "Select a game before requesting Revert.");
                return;
            }

            // Revert the game using the mybak file
            game.RevertGame(lblError);
            MessageUtils.SetUserMessage(lblError, $"Revert at {DateTime.Now:HH:mm} completed successfully.");

            // Update the list of backups
            RefreshListOfBackups();
        }

        private void UpdateStatusBar()
        {
            if (listGames.SelectedItem is GameConfig game)
            {
                // Set the monitor status
                lblMonitorValue.Text = game.monitor.monitoringMode.ToString();

                // Set colour of backup status button based on status
                switch (game.monitor.monitoringStatus)
                {
                    case MonitoringStatus.Playing:
                        btnBackupStatus.BackColor = Color.Red;
                        break;
                    case MonitoringStatus.Restored:
                        btnBackupStatus.BackColor = Color.Yellow;
                        break;
                    case MonitoringStatus.BackedUp:
                        btnBackupStatus.BackColor = Color.Green;
                        break;
                    default:
                        btnBackupStatus.BackColor = Color.Gray;
                        break;
                }

                // Set LastSave time
                lblLastSaveValue.Text = game.monitor.point.GameSaveTime.ToString("HH:mm:ss");

                // Set LastAuto time
                lblLastBackupValue.Text = game.monitor.point.LastBackupTime.ToString("HH:mm:ss");

                // Set FileCount value
                lblFileCountValue.Text = game.monitor.point.AutoBackupFileCounter.ToString("D3");

                // Set visibility of status bar labels based on monitor status
                lblLastSave.Visible = game.monitor.isMonitoring;
                lblLastSaveValue.Visible = game.monitor.isMonitoring;
                lblLastBackup.Visible = game.monitor.isMonitoring;
                lblLastBackupValue.Visible = game.monitor.isMonitoring;

                lblLastAuto.Visible = game.monitor.isMonitoringAuto;
                lblLastAutoValue.Visible = game.monitor.isMonitoringAuto;
                lblFileCount.Visible = game.monitor.isMonitoringAuto;
                lblFileCountValue.Visible = game.monitor.isMonitoringAuto;
            }
            else
            {
                // The backup status button is grayed out
                btnBackupStatus.BackColor = Color.Gray;

                // Nothing further is visible
                lblLastSave.Visible = false;
                lblLastSaveValue.Visible = false;
                lblLastBackup.Visible = false;
                lblLastBackupValue.Visible = false;
                lblLastAuto.Visible = false;
                lblLastAutoValue.Visible = false;
                lblFileCount.Visible = false;
                lblFileCountValue.Visible = false;
            }
        }

        private void Game_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is GameSaveManager.MonitoringManager monitoringManager)
            {
                if (e.PropertyName == nameof(MonitoringManager.monitoringMode))
                {
                    GameConfigManager.SaveGameConfigs(games);
                }

                UpdateStatusBar();
            }
        }

        private void LoadManagerSettings()
        {
            try
            {
                gameManagerSettings = GameManagerSettings.Load();
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, ex.Message);
                gameManagerSettings = new GameManagerSettings(); // Use default settings if loading fails
            }
        }


        private void BtnBackupStatus_Click(object sender, EventArgs e)
        {
            if (btnBackupStatus.BackColor == Color.Red)
            {
                BackupGame_Click(sender, e);
            }
            else if (btnBackupStatus.BackColor == Color.Yellow)
            {
                RevertGame_Click(sender, e);
            }
        }

        private void MonitorLabel_Click(object sender, EventArgs e)
        {
            if (listGames.SelectedItem is GameConfig game)
            {
                game.monitor.CycleMonitoringMode();
            }
            else
            {
                MessageUtils.SetErrorMessage(lblError, "Select a game before cycling the monitoring mode.");
            }
        }

        /// <summary>
        /// Restores the most recent backup for the currently (or last) selected game, without any dialogs.
        /// </summary>
        public void RestoreMostRecentBackupSilent()
        {
            if (listGames.Items.Count == 0)
                return;

            // Clear previous messages
            MessageUtils.ClearMessage(lblError);

            // If nothing is selected, try to select the last selected game from settings
            if (listGames.SelectedItem == null && !string.IsNullOrEmpty(gameManagerSettings.LastSelectedGame))
            {
                foreach (GameConfig g in listGames.Items)
                {
                    if (string.Equals(g.Name, gameManagerSettings.LastSelectedGame, StringComparison.OrdinalIgnoreCase))
                    {
                        // Temporarily suppress ListGames_SelectedIndexChanged side effects
                        ignoreGameFocusChange = true;
                        listGames.SelectedItem = g;
                        ignoreGameFocusChange = false;
                        break;
                    }
                }
            }

            // Now, ensure the correct game is selected
            var game = listGames.SelectedItem as GameConfig;
            if (game == null)
            {
                MessageBox.Show("No game selected for restore. Please check your settings.", "Restore Error");
                return;
            }

            // Load backups for the selected game
            game.LoadBackups(backups, lblError);

            if (backups.Count == 0)
                return;

            // Restore the most recent backup (first in the list)
            var backup = backups[0];
            backup.RestoreGame(lblError);

            // Refresh the list of backups after restore
            game.LoadBackups(backups, lblError);
            RefreshListOfBackups();

            MessageUtils.SetUserMessage(lblError, $"Silent restore at {DateTime.Now:HH:mm} completed from backup ({backup.Name}).");
        }

        /// <summary>
        /// Creates a backup of the currently (or last) selected game without any dialogs.
        /// </summary>
        public void BackupGameSilent()
        {
            if (listGames.Items.Count == 0)
            {
                MessageBox.Show("No games configured.", "Backup Error");
                return;
            }

            // Clear previous messages
                MessageUtils.ClearMessage(lblError);

            // If nothing is selected, try to select the last selected game from settings
            if (listGames.SelectedItem == null && !string.IsNullOrEmpty(gameManagerSettings.LastSelectedGame))
            {
                bool found = false;
                foreach (GameConfig g in listGames.Items)
                {
                    if (string.Equals(g.Name, gameManagerSettings.LastSelectedGame, StringComparison.OrdinalIgnoreCase))
                    {
                        // Temporarily suppress ListGames_SelectedIndexChanged side effects
                        ignoreGameFocusChange = true;
                        listGames.SelectedItem = g;
                        ignoreGameFocusChange = false;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    MessageBox.Show($"Could not find last selected game: {gameManagerSettings.LastSelectedGame}", "Backup Error");
                    return;
                }
            }

            // Now, ensure the correct game is selected
            var game = listGames.SelectedItem as GameConfig;
            if (game == null)
            {
                MessageBox.Show("No game selected for backup. Please check your settings.", "Backup Error");
                return;
            }

            // Load backups for the selected game
            game.LoadBackups(backups, lblError);

            // Check if there are any existing backups to get the tag from
            if (backups.Count == 0)
            {
                MessageBox.Show($"No existing backups found for game: {game.Name}. Please create a backup manually first.", "Backup Error");
                return;
            }

            try
            {
                // Get the tag from the most recent backup (first in the list)
                var latestBackup = backups[0];
                SavedGame backup = game.GetNewBackup(latestBackup.Name, lblError);
                backup.BackupGame(latestBackup.Tag, lblError);

                // Update the list of backups
                game.LoadBackups(backups, lblError);
                RefreshListOfBackups();

                MessageUtils.SetUserMessage(lblError, $"Silent backup at {DateTime.Now:HH:mm} completed ({backup.Name}).");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during backup: {ex.Message}", "Backup Error");
            }
        }
    }
}