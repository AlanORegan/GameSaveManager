using System;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace GameSaveManager
{
    public class GameManagerSettings
    {
        public WindowPosition WindowPosition { get; set; } = new WindowPosition { X = 100, Y = 100, Width = 800, Height = 600, ScreenIndex = 0 };
        public string LastSelectedGame { get; set; } // Add this property

        private static readonly string gameManagerSettingsFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameManagerSettings.json");

        public static GameManagerSettings Load()
        {
            if (File.Exists(gameManagerSettingsFileName))
            {
                try
                {
                    var json = File.ReadAllText(gameManagerSettingsFileName);
                    return JsonSerializer.Deserialize<GameManagerSettings>(json) ?? new GameManagerSettings();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error loading manager settings: {ex.Message}");
                }
            }
            return new GameManagerSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(gameManagerSettingsFileName, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving manager settings: {ex.Message}");
            }
        }
    }

    public class WindowPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ScreenIndex { get; set; } // New property to remember the screen index
    }
}