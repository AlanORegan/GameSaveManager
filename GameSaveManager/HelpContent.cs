namespace GameSaveManager
{
    public static class HelpContent
    {
        public static string MainDialog =>
@"Game Save Manager - Main Dialog Help

The main dialog allows you to manage your games and their save backups. You can add, edit, delete, backup, restore, revert, and rename game backups. The interface displays a list of games on the left and their backups on the right. The status bar at the bottom shows monitoring and backup status for the selected game.

Actions:
- Add Game
- Edit Game
- Delete Game
- Backup Game
- Restore Game
- Revert Game
- Rename Backup

Select an action below for more details.";

        public static string AddGame =>
@"Add Game

Use this dialog to add a new game configuration. You must fill in all required fields. Each field is validated to ensure correct input.

Fields:
- Name: The display name for the game.
- Parent Directory: The folder where the game stores its save files.
- User Directory: The folder where backups will be stored.
- Strategy: The backup strategy (PeerDirectory or SubordinateUserFile).
- Game Directory: The subfolder within the parent directory where the game stores saves.
- Name Format: Format for naming backups.
- Date Format: Date/time format for backup names.
- Version Format: Version string format.
- Prefix: Prefix for backup names.
- Parts: Additional parts for backup naming.
- Separator: Separator character(s) for backup naming.
- Max Backups: Maximum number of backups to keep.
- Revert Suffix: Suffix for revert/restore operations.
- Save Prefix: (SubordinateUserFile only) Prefix for the save file.
- Save Extension: (SubordinateUserFile only) Extension for the save file.";

        public static string EditGame => AddGame;

        public static string DeleteGame =>
@"Delete Game

Removes the selected game configuration and all its associated backups from the manager. This action cannot be undone.";

        public static string BackupGame =>
@"Backup Game

Creates a new backup of the selected game's save files. You can specify a name for the backup. The backup will be stored in the user directory.";

        public static string RestoreGame =>
@"Restore Game

Restores the selected backup to the game's save directory. If no backup is selected, the most recent backup will be restored.";

        public static string RevertGame =>
@"Revert Game

Reverts the game save to the state before the last restore operation, using the revert backup.";

        public static string RenameBackup =>
@"Rename Backup

Allows you to rename an existing backup for the selected game. The new name must be unique and valid.";

        public static string FieldDescriptions =>
@"Field Descriptions

- Name: The display name for the game.
- Parent Directory: The folder where the game stores its save files.
- User Directory: The folder where backups will be stored.
- Strategy: The backup strategy (PeerDirectory or SubordinateUserFile).
- Game Directory: The subfolder within the parent directory where the game stores saves. Leave blank if saves are stored directly in the parent directory.
- Name Format: Format for naming backups. Only allowed characters: PGsDVTRE, each capital letter once, 's' only once between capitals.
- Date Format: Date/time format for backup names (e.g., yyyy-MM-dd HH'h'mm'm'ss).
- Version Format: Version string format (e.g., 0.0 or v1.23).
- Prefix: Prefix for backup names, up to 20 valid filename characters.
- Parts: Combination of -+^& for backup naming.
- Separator: Up to 3 valid filename characters used as a separator.
- Max Backups: Maximum number of backups to keep (positive integer).
- Revert Suffix: Suffix for revert/restore operations (cannot be empty).
- Save Prefix: (SubordinateUserFile only) Prefix for the save file.
- Save Extension: (SubordinateUserFile only) Extension for the save file (up to 3 valid filename characters).";
    }
}
