using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.CodeDom;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Runtime.Serialization;

namespace GameSaveManager
{
    // ********************************************************************************************************************
    // STRUCT: SaveFile
    // ********************************************************************************************************************
    public struct SaveFile
    {
        private string _prefix;
        public string Prefix 
        { 
            get => _prefix; 
            set
            {
                if (string.IsNullOrEmpty(value) || GameSaveManager.SaveFile.IsValidPrefix(value))
                {
                    _prefix = value;
                }
                else
                {
                    throw new ArgumentException("Invalid Prefix. It can be up to 20 characters that are valid in a filename, excluding the directory level separator '/'.");
                }
            }
        }

        private string _extension;
        public string Extension
        {
            get => _extension;
            set
            {
                if (IsValidExtension(value))
                {
                    _extension = value;
                }
                else
                {
                    throw new ArgumentException("Invalid Extension. It can be up to 3 characters that are valid in a filename.");
                }
            }
        }

        [JsonIgnore]
        public string Name 
        {
            get
            {
                return $"{Prefix}.{Extension}";
            }
        }

        public static bool IsValidPrefix(string prefix)
        {
            string pattern = @"^[\w\-. ](?:(?!\. )[\w\-. ]){0,18}[^\. ]$";
            return Regex.IsMatch(prefix, pattern);
        }

        public static bool IsValidExtension(string extension)
        {
            string pattern = @"^[\w\-. ]{0,3}$";
            return Regex.IsMatch(extension, pattern);
        }
    }

    // ********************************************************************************************************************
    // CLASS: GameConfig
    // ********************************************************************************************************************
    public class GameConfig
    {
        // Constructor
        public GameConfig()
        {
            monitor = new MonitoringManager(this);
        }

        public string Name { get; set; }
        // ParentDirectory: is the parent folder on the local machine where the game itself stores the SavedGame
        public string ParentDirectory { get; set; }
        // UserDirectory: is the parent folder on the local machine where the user chooses to store the SavedGames (backups)
        public string UserDirectory { get; set; }
        // GameDirectory: is the subfolder within the ParentDirectory where the game itself stores the SavedGame
        public string GameDirectory { get; set; } 
        private string _strategyType;
        public string StrategyType
        {
            get => _strategyType;
            set
            {
                if (_strategyType != value)
                {
                    _strategyType = value;
                    Strategy = value switch
                    {
                        BackupStrategy.PeerDirectory => new PeerDirectoryBackupStrategy(this),
                        BackupStrategy.SubordinateUserFile => new SubordinateUserFileBackupStrategy(this),
                        _ => throw new NotSupportedException("Unsupported backup strategy.")
                    };
                }
            }
        }
        [JsonIgnore]
        public BackupStrategy Strategy { get; private set; } // Public property with a private setter

        private string _nameFormat;
        public string NameFormat
        {
            get => _nameFormat;
            set
            {
                if (IsValidNameFormat(value))
                {
                    _nameFormat = value;
                }
                else
                {
                    throw new ArgumentException("Invalid NameFormat. It can only contain the characters PGsDVTRE, each capital letter can only occur once, and 's' can be present only once between any two capitals.");
                }
            }
        }
        public static bool IsValidNameFormat(string format)
        {
            // Set pattern without E (extension) for PeerDirectory strategy
            string pattern = @"^(?:(?:P|G|D|V|T|R|E|s| )+)$";

            if (!System.Text.RegularExpressions.Regex.IsMatch(format, pattern) || format.EndsWith('s'))
            {
                return false; // invalid name format
            }

            var capitals = new HashSet<char>();
            foreach (char c in format)
            {
                if (char.IsUpper(c))
                {
                    if (!capitals.Add(c))
                    {
                        return false; // Duplicate capital letter found
                    }
                }
            }
            return true;
        }

        private string _dateFormat;
        public string DateFormat
        {
            get => _dateFormat;
            set
            {
                if (IsValidDateFormat(value))
                {
                    _dateFormat = value;
                }
                else
                {
                    throw new ArgumentException("Invalid DateFormat. It must be a valid format string for DateTime.");
                }
            }
        }

        public static bool IsValidDateFormat(string format)
        {
            try
            {
                DateTime.Now.ToString(format);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private string _versionFormat = string.Empty;
        public string VersionFormat
        {
            get => _versionFormat;
            set
            {
                if (IsValidVersionFormat(value))
                {
                    _versionFormat = value;
                }
                else
                {
                    throw new ArgumentException("Invalid VersionFormat. It must be of the form 'v999.99' or '999.99', where the decimal part is optional.");
                }
            }
        }

        public static bool IsValidVersionFormat(string format)
        {
            string pattern = @"^v?\d{1,3}(\.\d{1,2})?$";
            return Regex.IsMatch(format, pattern);
        }
        
        // SavePrefix: for strategy of
        //      PeerDirectory: the prefix of the backup directories
        //      SubordinateUserFile: the prefix of the backup file
        private string _savePrefix;
        public string SavePrefix
        {
            get => _savePrefix;
            set
            {
                if (string.IsNullOrEmpty(value) || GameSaveManager.SaveFile.IsValidPrefix(value))
                {
                    _savePrefix = value;
                }
                else
                {
                    throw new ArgumentException("Invalid SavePrefix. It must be up to 20 characters that are valid in a filename, excluding the directory level separator '/'.");
                }
            }
        }

        private string _parts;
        public string Parts
        {
            get => _parts;
            set
            {
                if (IsValidParts(value))
                {
                    _parts = value;
                }
                else
                {
                    throw new ArgumentException("Invalid Parts. It can contain any combination of -+^& in any order, all of which are optional.");
                }
            }
        }

        public static bool IsValidParts(string parts)
        {
            string pattern = @"^[\-\+\^&]*$";
            return Regex.IsMatch(parts, pattern);
        }

        private string _separator;
        public string Separator
        {
            get => _separator;
            set
            {
                if (IsValidSeparator(value))
                {
                    _separator = value;
                }
                else
                {
                    throw new ArgumentException("Invalid Separator. It can be up to 3 character that are valid in a filename.");
                }
            }
        }

        public static bool IsValidSeparator(string separator)
        {
            string pattern = @"^^[\w\-. ]{0,3}$";
            return Regex.IsMatch(separator, pattern);
        }

        public int MaxBackups { get; set; }
        public string RevertSuffix { get; set; }
        
        public MonitoringManager monitor;
        
        // Property duplicated here from MonitoringManager for ease of persistence in json
        public MonitoringMode monitoringMode
        {
            get => monitor.monitoringMode;
            set => monitor.monitoringMode = value;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (monitor == null)
            {
                monitor = new MonitoringManager(this);
            }
            monitor.monitoringMode = monitoringMode;
        }

        public SaveFile? SaveFile { get; set; }

        // ********************************************************************************************************************
        // GameConfig Methods
        // ********************************************************************************************************************

        public void GainFocus(PropertyChangedEventHandler propertyChangedHandler)
        {
            monitor.refreshMonitoringPoint(null);
            monitor.Start(propertyChangedHandler);
        }

        public void LoseFocus(PropertyChangedEventHandler propertyChangedHandler)
        {
            monitor.Stop(propertyChangedHandler);
        }

        // Print the GameConfig object to the console
        public void PrintToConsole()
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // Allow unsafe characters
                Converters = { new CustomStringConverter() }
            };
            string jsonString = JsonSerializer.Serialize(this, options);
            Console.WriteLine(jsonString);
        }

        public SavedGame GetNewBackup(string newSaveName, Label lblError)
        {
            return new SavedGame(this, newSaveName, lblError);
        }

        public void RevertGame(Label lblError)
        {
            Strategy.RevertGame(lblError);
        }

        public void BackupGame(SavedGame backup, string newTag, Label lblError)
        {
            // Reset Reuse counter
            backup.Reuse = 0;

            // Update the Date using public property
            backup.Date = backup.CurrentDate;

            // Increment Version
            backup.IncrementVersion();
            
            // Update the Tag
            backup.Tag = newTag;

            // Perform the backup
            Strategy.BackupGame(backup.Name, lblError);
        }

        public void RestoreGame(SavedGame backup, Label lblError)
        {
            // Store the old backup name
            string oldBackupName = backup.Name;

            // Restore from the backup
            Strategy.RestoreGame(backup.Name, lblError);

            // Increment reuse counter
            backup.Reuse++;
            bool reuseOverflow = backup.Reuse == 100;
              
            // Check if the backups need to be renamed (if using Reuse counter)
            if (NameFormat.Contains('R'))
            {
                if (reuseOverflow)
                {
                    // Make a new backup so the overflow of reuse is recorded
                    BackupGame(backup, backup.Tag, lblError);
                }
                else
                {
                    // Rename the backup to update reuse
                    Strategy.RenameBackup(oldBackupName, backup.Name, lblError);
                } 
            }
        }

        public void RenameBackup(SavedGame backup, string newTag, Label lblError)
        {
            string oldBackupName = backup.Name;
            backup.Tag = newTag;
            Strategy.RenameBackup(oldBackupName, backup.Name, lblError);
        }

        public void DeleteBackup(SavedGame backup, Label lblError)
        {
            Strategy.DeleteBackup(backup.Name, lblError);
        }

        public void LoadBackups(List<SavedGame> backups, Label lblError)
        {
            Strategy.LoadBackups(backups, lblError);
        }

        public GameConfig CloneWithCopyName()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new CustomStringConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };
            string json = JsonSerializer.Serialize(this, options);
            var clone = JsonSerializer.Deserialize<GameConfig>(json, options);
            clone.Name = this.Name + " - Copy";
            return clone;
        }
    }
}