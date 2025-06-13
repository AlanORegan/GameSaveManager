namespace GameSaveManager
{
    public interface IBackupStrategy
    {
        // ...existing methods...
        void DeleteBackup(SavedGame backup, Label errorLabel);
    }
}
