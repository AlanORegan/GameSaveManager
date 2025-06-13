using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace GameSaveManager
{
    public abstract class BackupStrategy
    {
        protected GameConfig Game;
        protected BackupStrategy(GameConfig game)
        {
            Game = game;
        }
        public const string PeerDirectory = "PeerDirectory";
        public const string SubordinateUserFile = "SubordinateUserFile";
        public abstract void LoadBackups(List<SavedGame> backups, Label lblError);
        public abstract void BackupGame(string newBackupName, Label lblError);
        public abstract void RestoreGame(string restoreFromBackup, Label lblError);
        public abstract void RenameBackup(string oldBackupName, string newBackupName, Label lblError);
        public abstract string GetLatestBackupName(Label lblError);
        public abstract void RevertGame(Label lblError);
        public abstract void MonitorGameSaveLocation(FileSystemWatcher watcher, Label lblError);
        public abstract void HandleMonitorBackup(string backupName, Label lblError);
        public abstract void DeleteBackup(string backupName, Label lblError);
        
        public abstract MonitoringPoint getMonitorPointInfo(Label lblError);

        protected void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }

            // Delete the target directory if it exists
            if (Directory.Exists(destDirName))
            {
                Directory.Delete(destDirName, true);
            }

            // Create the target directory
            Directory.CreateDirectory(destDirName);

            // Copy the files and recursively the subdirectories
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, true);
                }
            }
        }
    }

    // ********************************************************************************************************************
    // BackupStrategy for PeerDirectory
    // ********************************************************************************************************************

    public class PeerDirectoryBackupStrategy : BackupStrategy
    {
        public PeerDirectoryBackupStrategy(GameConfig game) : base(game) { }

        public string gameFileName {
            get {
                return Game.SaveFile?.Name ?? string.Empty;
            }
        }
        
        public string gameSaveLocation{
            get {
                return Path.Combine(Game.ParentDirectory, Game.GameDirectory);
            }
        }

        public string revertDirectory{
            get {
                return Game.RevertSuffix;
            }
        }

        public string revertLocation{
            get {
                return $"{Game.GameDirectory}_{Game.RevertSuffix}";
            }
        }

        public string revertLocationWithPath{
            get {
                return Path.Combine(Game.UserDirectory, revertDirectory, revertLocation);
            }
        }

        public override void LoadBackups(List<SavedGame> backups, Label lblError)
        {
            backups.Clear();

            if (Directory.Exists(Game.UserDirectory))
            {
                var backupDirectories = Directory.GetDirectories(Game.UserDirectory)
                                                 .Where(d => !d.EndsWith(Game.GameDirectory) && !d.EndsWith(revertDirectory))
                                                 .OrderByDescending(d => d) // Sort by directory name in descending order
                                                 .Take(Game.MaxBackups); // Limit the number of backups displayed
                foreach (var backup in backupDirectories)
                {
                    backups.Add(new SavedGame(Game, Path.GetFileName(backup)));
                }
                MessageUtils.SetInfoMessage(lblError, "Backups loaded successfully.");
            }
            else
            {
                MessageUtils.SetErrorMessage(lblError, $"Backup directory for {Game.Name} not found: {Game.UserDirectory}");
            }
        }

        public override void BackupGame(string newSaveName, Label lblError)
        {
            try
            {
                // Calculate the fully path-qualified source to be backed up
                string sourceDirectory = gameSaveLocation;

                // Calculate the fully path-qualified target for the new save
                string targetDirectory = Path.Combine(Game.UserDirectory, newSaveName);
                
                DirectoryCopy(sourceDirectory, targetDirectory, true);

                // Remove the revert directory if present
                if (Directory.Exists(revertLocationWithPath))
                {
                    Directory.Delete(revertLocationWithPath, true);
                };

                MessageUtils.SetInfoMessage(lblError, "Backup completed successfully.");
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error during backup: {ex.Message}");
            }
        }

        public override void RestoreGame(string fromBackupName, Label lblError)
        {
            try
            {
                // Calculate the source directory to be restored
                string sourceDirectory = Path.Combine(Game.UserDirectory, fromBackupName);

                // Calculate the target location of the game save directory
                string targetDirectory = gameSaveLocation;

                // Make a backup of the game save directory for revert
                DirectoryCopy(targetDirectory, revertLocationWithPath, true);
                
                //Restore the game directory
                DirectoryCopy(sourceDirectory, targetDirectory, true);

                MessageUtils.SetInfoMessage(lblError, "Restore completed successfully.");
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error during restore: {ex.Message}");
            }
        }

        public override void RenameBackup(string oldBackupName, string newBackupName, Label lblError)
        {
            string oldBackupPath = Path.Combine(Game.UserDirectory, oldBackupName);
            string newBackupPath = Path.Combine(Game.UserDirectory, newBackupName);
            
            bool newBackupAlreadyExists = Directory.Exists(newBackupPath);
            bool caseInsensitiveDuplicate = string.Equals(oldBackupPath, newBackupPath, StringComparison.OrdinalIgnoreCase);

            // Windows file system is case-insensitive, so we need to check for case-insensitive duplicates
            if (!caseInsensitiveDuplicate && newBackupAlreadyExists)
            {
                MessageUtils.SetErrorMessage(lblError, "A backup with the new name already exists.");
            }
            else
            {
                try
                {
                    // Check if the new name is the same as the old name (case-insensitive)
                    if (caseInsensitiveDuplicate)
                    {
                        // Use a temporary name to avoid case-insensitive conflict
                        string tempName = $"{newBackupPath}_temp";
                        Directory.Move(oldBackupPath, tempName);
                        Directory.Move(tempName, newBackupPath);
                    }
                    else
                    {
                        Directory.Move(oldBackupPath, newBackupPath);
                    }
                    MessageUtils.SetInfoMessage(lblError, "Backup renamed successfully.");
                }
                catch (Exception ex)
                {
                    MessageUtils.SetErrorMessage(lblError, $"Error renaming backup: {ex.Message}");
                }
            }
        }

        public override string GetLatestBackupName(Label lblError)
        {
            string revertDirName = Path.GetFileName(revertLocationWithPath);
            string? latestBackupName = Directory.GetDirectories(Game.UserDirectory)
                                                .Where(d => !d.EndsWith(Game.GameDirectory) && !d.EndsWith(revertDirectory))
                                                .OrderByDescending(d => d)
                                                .FirstOrDefault();
            if (latestBackupName != null)
            {
                latestBackupName = Path.GetFileName(latestBackupName); // Get only the directory name without the path
            }
            else
            {
                MessageUtils.SetInfoMessage(lblError, $"Latest Backup directory for {Game.Name} not found: {Game.UserDirectory}");
            }
            return latestBackupName;
        }

        public override void RevertGame(Label lblError)
        {
            try
            {
                string sourceDirectory = revertLocationWithPath;
                string targetDirectory = gameSaveLocation;

                if (Directory.Exists(sourceDirectory))
                {
                    DirectoryCopy(sourceDirectory, targetDirectory, true);
                    Directory.Delete(sourceDirectory, true);
                    MessageUtils.SetInfoMessage(lblError, "Revert completed successfully.");
                }
                else
                {
                    MessageUtils.SetErrorMessage(lblError, $"Backup directory {sourceDirectory} not found for game {Game.Name}.");
                }
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error during revert: {ex.Message}");
            }
        }

        public override void MonitorGameSaveLocation(FileSystemWatcher watcher, Label lblError)
        {
            watcher.Path = gameSaveLocation;
            watcher.Filter = "*";
            watcher.EnableRaisingEvents = true;
        }

        public override void HandleMonitorBackup(string backupName, Label lblError)
        {
            try
            {
                string sourceDirectory = gameSaveLocation;
                string targetDirectory = Path.Combine(Game.UserDirectory, Game.RevertSuffix, backupName);

                DirectoryCopy(sourceDirectory, targetDirectory, true);
                MessageUtils.SetInfoMessage(lblError, "Automatic backup completed successfully.");
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error during automatic backup: {ex.Message}");
            }
        }

        public override void DeleteBackup(string backupName, Label lblError)
        {
            try
            {
                string backupPath = Path.Combine(Game.UserDirectory, backupName);
                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                    MessageUtils.SetInfoMessage(lblError, "Backup deleted successfully.");
                }
                else
                {
                    MessageUtils.SetErrorMessage(lblError, $"Backup directory not found: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error deleting backup: {ex.Message}");
            }
        }

        public override MonitoringPoint getMonitorPointInfo(Label lblError)
        {
            MonitoringPoint monitorPoint = new MonitoringPoint();
            string latestBackupName = GetLatestBackupName(lblError);

            if (!string.IsNullOrEmpty(latestBackupName))
            {
                string latestBackupPath = Path.Combine(Game.UserDirectory, latestBackupName, gameFileName);
                monitorPoint.LastBackupTime = File.GetLastWriteTime(latestBackupPath);
            } else {
                monitorPoint.LastBackupTime = DateTime.MinValue;
            }
            
            monitorPoint.GameSaveTime = File.GetLastWriteTime(Path.Combine(gameSaveLocation, gameFileName));
            monitorPoint.hasRevertDirectory = Directory.Exists(revertLocationWithPath);

            return monitorPoint;
        }
    }
    // ********************************************************************************************************************
    // BackupStrategy for SubordinateUserFile
    // ********************************************************************************************************************

    public class SubordinateUserFileBackupStrategy : BackupStrategy
    {
        public SubordinateUserFileBackupStrategy(GameConfig game) : base(game) { }

        public string gameFileName {
            get {
                return Game.SaveFile?.Name ?? string.Empty;
            }
        }
        
        public string gameSaveLocation{
            get {
                return Path.Combine(Game.ParentDirectory, gameFileName);
            }
        }

        public string userFullDirectory{
            get {
                return string.IsNullOrEmpty(Game.GameDirectory) ? Game.UserDirectory : Path.Combine(Game.UserDirectory, Game.GameDirectory);
            }
        }

        public string revertLocation{
            get {
                return $"{gameFileName}.{Game.RevertSuffix}";
            }
        }

        public string revertLocationWithPath{
            get {
                return Path.Combine(Game.UserDirectory, Game.GameDirectory, revertLocation);
            }
        }

        public override void LoadBackups(List<SavedGame> backups, Label lblError)
        {
            backups.Clear();
 
            if (Directory.Exists(userFullDirectory))
            {
                var backupFiles = Directory.GetFiles(userFullDirectory)
                                           .Where(f => Path.GetExtension(f).EndsWith(Game.SaveFile?.Extension)) // Exclude the revert location
                                           .OrderByDescending(d => d) // Sort by file name in descending order
                                           .Take(Game.MaxBackups); // Limit the number of backups displayed
                foreach (var backup in backupFiles)
                {
                    backups.Add(new SavedGame(Game, Path.GetFileNameWithoutExtension(backup)));
                }
                MessageUtils.SetInfoMessage(lblError, "Backups loaded successfully.");
            }
            else
            {
                MessageUtils.SetErrorMessage(lblError, $"Backup directory for {Game.Name} not found: {userFullDirectory}");
            }
        }

        public override void BackupGame(string newSaveName, Label lblError)
        {
            try
            {
                // Calculate the fully path-qualified source to be backed up
                string sourceFile = gameSaveLocation;

                // Calculate the fully path-qualified target for the new backup
                string targetFile = Path.Combine(userFullDirectory, newSaveName);

                // Create the backup
                File.Copy(sourceFile, targetFile, true);

                // Remove the revert directory if present
                if (File.Exists(revertLocationWithPath))
                {
                    File.Delete(revertLocationWithPath);
                };

                MessageUtils.SetInfoMessage(lblError, "Backup completed successfully.");
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error during backup: {ex.Message}");
            }
        }

        public override void RestoreGame(string restoreFromBackup, Label lblError)
        {
            try
            {
                // Calculate the source file to be restored
                string sourceFile = Path.Combine(userFullDirectory, restoreFromBackup);

                // Calculate the location of the game save file
                string targetFile = gameSaveLocation;

                // If possible, backup the game file for revert
                if (File.Exists(targetFile))
                {
                    // Backup the game file for revert
                    File.Copy(targetFile, revertLocationWithPath, true);
                    MessageUtils.SetInfoMessage(lblError, "Revert for backup taken.");
                }
                else
                {
                    MessageUtils.SetInfoMessage(lblError, $"Game file {targetFile} not found for game {Game.Name}.");
                }

                // Restore the game file
                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, targetFile, true);
                    MessageUtils.SetInfoMessage(lblError, "Restore completed successfully.");
                }
                else
                {
                    MessageUtils.SetErrorMessage(lblError, $"Backup file {sourceFile} not found for game {Game.Name}.");
                }
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error during restore: {ex.Message}");
            }
        }

        public override void RenameBackup(string oldBackupName, string newBackupName, Label lblError)
        {
            string oldBackupPath = Path.Combine(userFullDirectory, oldBackupName);
            string newBackupPath = Path.Combine(userFullDirectory, newBackupName);
            
            bool newBackupAlreadyExists = File.Exists(newBackupPath);
            bool caseInsensitiveDuplicate = string.Equals(oldBackupPath, newBackupPath, StringComparison.OrdinalIgnoreCase);

            // Windows file system is case-insensitive, so we need to check for case-insensitive duplicates
            if (!caseInsensitiveDuplicate && newBackupAlreadyExists)
            {
                MessageUtils.SetErrorMessage(lblError, "A backup with the new name already exists.");
            }
            else
            {
                try
                {
                    // Check if the new name is the same as the old name (case-insensitive)
                    if (caseInsensitiveDuplicate)
                    {
                        // Use a temporary name to avoid case-insensitive conflict
                        string tempName = $"{newBackupPath}_temp";
                        File.Move(oldBackupPath, tempName);
                        File.Move(tempName, newBackupPath);
                    }
                    else
                    {
                        File.Move(oldBackupPath, newBackupPath);
                    }
                    MessageUtils.SetInfoMessage(lblError, "Backup renamed successfully.");
                }
                catch (Exception ex)
                {
                    MessageUtils.SetErrorMessage(lblError, $"Error renaming backup: {ex.Message}");
                }
            }
        }

        public override string GetLatestBackupName(Label lblError)
        {
            string? latestBackupFileName = Directory.GetFiles(userFullDirectory)
                                                    .OrderByDescending(d => d)
                                                    .FirstOrDefault(f => Path.GetExtension(f).EndsWith(Game.SaveFile?.Extension)); // Exclude the revert location
            if (latestBackupFileName != null)
            {
                latestBackupFileName = Path.GetFileName(latestBackupFileName); // Get only the filename with extension
            }
            // else
            // {
            //     MessageUtils.SetErrorMessage(lblError, $"Latest Backup for {Game.Name} not found: {userFullDirectory}");
            // }
            return latestBackupFileName;
        }

        public override void RevertGame(Label lblError)
        {
            try
            {
                // Calculate the location of the game save file (TARGET)
                string targetFile = gameSaveLocation;           
                
                // Calculate the location of the backup file to be reverted (SOURCE)
                string sourceFile = revertLocationWithPath;

                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, targetFile, true);
                    File.Delete(sourceFile);
                    MessageUtils.SetInfoMessage(lblError, "Revert completed successfully.");
                }
                else
                {
                    MessageUtils.SetErrorMessage(lblError, $"Backup file {sourceFile} not found for game {Game.Name}.");
                }
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error during revert: {ex.Message}");
            }
        }

        public override void MonitorGameSaveLocation(FileSystemWatcher watcher, Label lblError)
        {
            watcher.Path = Game.ParentDirectory;
            watcher.Filter = $"{Game.SaveFile?.Prefix}.*" ?? "*";
            watcher.EnableRaisingEvents = true;
        }

        public override void HandleMonitorBackup(string backupName, Label lblError)
        {
            try
            {
                string sourceFile = gameSaveLocation;
                string targetFile = Path.Combine(Game.UserDirectory, Game.RevertSuffix, $"{backupName}.{Game.SaveFile?.Extension}");

                File.Copy(sourceFile, targetFile, true);
                MessageUtils.SetInfoMessage(lblError, "Automatic backup completed successfully.");
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error during automatic backup: {ex.Message}");
            }
        }

        public override void DeleteBackup(string backupName, Label lblError)
        {
            try
            {
                string backupPath = Path.Combine(userFullDirectory, backupName);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    MessageUtils.SetInfoMessage(lblError, "Backup deleted successfully.");
                }
                else
                {
                    MessageUtils.SetErrorMessage(lblError, $"Backup file not found: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                MessageUtils.SetErrorMessage(lblError, $"Error deleting backup: {ex.Message}");
            }
        }

        public override MonitoringPoint getMonitorPointInfo(Label lblError)
        {
            MonitoringPoint monitorPoint = new MonitoringPoint();
            string latestBackupName = GetLatestBackupName(lblError);
            if (!string.IsNullOrEmpty(latestBackupName))
            {
                string latestBackupPath = Path.Combine(userFullDirectory, latestBackupName);
                monitorPoint.LastBackupTime = File.GetLastWriteTime(latestBackupPath);
            } else {
                monitorPoint.LastBackupTime = DateTime.MinValue;
            }

            monitorPoint.GameSaveTime = File.GetLastWriteTime(gameSaveLocation);
            monitorPoint.hasRevertDirectory = File.Exists(revertLocationWithPath);

            return monitorPoint;
        }
    }
}

