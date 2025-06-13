using System.Runtime.InteropServices.Marshalling;

namespace GameSaveManager
{
    // ********************************************************************************************************************
    // CLASS: SavedGame
    // ********************************************************************************************************************
    public class SavedGame
    {
        public SavedGame(GameConfig game, string fileName = null, Label lblError = null)
        {
            this.Game = game;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = game.Strategy.GetLatestBackupName(lblError);
            }

            if (fileName != null && fileName != string.Empty)
            {
                ExtractComponentsFromName(fileName);
            }
        }
        private string _savePrefix
        {
            get => this.Game.SavePrefix ?? string.Empty;
        }

        private string _userDirectory
        {
            get => this.Game.UserDirectory ?? string.Empty;
        }


        private string _gameDirectory
        {
            get => this.Game.GameDirectory ?? string.Empty;
        }        
        private string _separator
        {
            get => Game?.Separator ?? string.Empty;
        }
        private string _date
        {
            get => DateTime.Now.ToString(this.Game.DateFormat ?? "yyyy-MM-dd");
        }

        public string CurrentDate 
        { 
            get => _date; 
        }

        private int _versionLength
        {
            get => this.Game.VersionFormat.Length;
        }
        private string _extension
        {
            get => $".{this.Game.SaveFile?.Extension ?? string.Empty}";
        }
        private void ExtractComponentsFromName(string name)
        {
            var nameFormat = Game?.NameFormat ?? string.Empty;
            bool nameContainsReuse = System.Text.RegularExpressions.Regex.IsMatch(name, @"\(\d{2}\)");
            bool nameContainsExtension = name.Contains($".{Game?.SaveFile?.Extension ?? string.Empty}");

            var componentLengths = new Dictionary<char, int>
            {
                { 'P', _savePrefix.Length },
                { 'U', _userDirectory.Length },
                { 'G', _gameDirectory.Length },
                { 's', _separator.Length },
                { 'D', _date.Length },
                { 'V', _versionLength },
                { 'E', nameContainsExtension ? _extension.Length : 0 },
                { ' ', 1 }, // Space between unseparated components of the name
                { 'T', 0 }, // Tag length will be calculated dynamically
                { 'R',  nameContainsReuse ? 4 : (nameFormat.IndexOf('R') > 0 && nameFormat[nameFormat.IndexOf('R') - 1] == ' ') ? -1 : 0 }  // Assuming format (00)
            };

            int offset = 0;
            foreach (char component in nameFormat)
            {
                if (component == 'V')
                {
                    Version = Decimal.Parse(name.Substring(offset, componentLengths[component]));
                }
                else if (component == 'D')
                {
                    Date = name.Substring(offset, componentLengths[component]);
                }
                else if (component == 'T')
                {
                    int remainingLength = 0;
                    foreach (char c in nameFormat.Substring(nameFormat.IndexOf('T') + 1))
                    {
                        remainingLength += componentLengths[c];
                    }
                    int tagLength = name.Length - offset - remainingLength;
                    Tag = name.Substring(offset, tagLength);
                    componentLengths['T'] = tagLength;
                }
                else if (component == 'R')
                {
                    if (componentLengths['R'] > 0)
                    {
                        Reuse = int.Parse(name.Substring(offset + 1, 2)); // Remove brackets
                    }
                    else
                    {
                        Reuse = 0;
                    }
                }
                offset += componentLengths[component];
            }
        }

        public string Name 
        {
            get 
            {
                var version = Version.ToString(Game?.VersionFormat ?? "0.0");
                var reuseFormatted = (Reuse == 0) ? string.Empty : $"({Reuse.ToString("D2")})";

                // Build the name from the format string
                var nameBuilder = new System.Text.StringBuilder();
                for (int i = 0; i < (Game?.NameFormat ?? string.Empty).Length; i++)
                {
                    char component = Game.NameFormat[i];
                    switch (component)
                    {
                        case 'P':
                            nameBuilder.Append(_savePrefix);
                            break;
                        case 'G':
                            nameBuilder.Append(_gameDirectory);
                            break;
                        case 's':
                            nameBuilder.Append(_separator);
                            break;
                        case 'D':
                            nameBuilder.Append(Date);
                            break;
                        case 'V':
                            nameBuilder.Append(version);
                            break;
                        case ' ':
                            nameBuilder.Append(" ");
                            break;                        
                        case 'E':
                            nameBuilder.Append(_extension);
                            break;
                        case 'T':
                            nameBuilder.Append(Tag ?? string.Empty);
                            break;
                        case 'R':
                            if (string.IsNullOrEmpty(reuseFormatted) && i > 0 && Game.NameFormat[i - 1] == ' ')
                            {
                                nameBuilder.Length--; // Remove the previous character from nameBuilder if it is a space in NameFormat
                            }
                            else
                            {
                                nameBuilder.Append(reuseFormatted);
                            }
                            break;
                        default:
                            nameBuilder.Append(component);
                            break;
                    }
                }

                return nameBuilder.ToString();
            }
        }
        public decimal Version { get; set; }
        public string Tag { 
            get => _tag;
            set 
            {
                string oldTag = _tag; // Store the old value
                string newTag = value; // Store the new value

                if (string.IsNullOrEmpty(newTag))
                {
                    throw new ArgumentException("newTag cannot be null or empty.");
                }

                // Check to see if the newTag starts with a special 'parts' character to decide how to change the existing Tag
                char specialPart = newTag[0];
                if (Game.Parts.Contains(specialPart))
                {
                    int lastIndex = oldTag.LastIndexOf(specialPart);
                    if (lastIndex != -1)
                    {
                        // If the existing Tag uses the special 'part' character the newTag starts with
                        // Replace that part of the existing tag, starting with the special 'part' character to the end, with the new tag
                        _tag = oldTag.Substring(0, lastIndex) + newTag;
                    }
                    else
                    {
                        // If the existing Tag does NOT have the special 'part' character the newTag starts with
                        // Append the newTag after a ' '
                        _tag = $"{oldTag} {newTag}";
                    }
                }
                else
                {
                    // If the newTag does not start with a special 'parts' character
                    // Replace the entire Tag with the newTag
                    _tag = newTag;
                }
            }
        }
        private string _tag;
        public int Reuse { get; set; }
        public string Date { get; set; }
        public GameConfig Game { get; private set; } // Reference the Game of this SavedGame       
    }

}