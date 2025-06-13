using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

namespace GameSaveManager
{
    // ********************************************************************************************************************
    // * CLASS: GameConfigManager
    // * This class is used to manage the game configurations. It loads and saves the game configurations from a JSON file.
    // ********************************************************************************************************************
    public static class GameConfigManager
    {
        private static readonly string gameConfigsFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameConfigs.json");

        public static List<GameConfig> LoadGameConfigs()
        {
            if (File.Exists(gameConfigsFileName))
            {
                try
                {
                    var json = File.ReadAllText(gameConfigsFileName);
                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        Converters = { new CustomStringConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                    };
                    var rootConfig = JsonSerializer.Deserialize<RootConfig>(json, options);
                    return rootConfig?.Games ?? new List<GameConfig>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading config: {ex.Message}");
                    return new List<GameConfig>();
                }
            }
            else
            {
                Console.WriteLine($"No config file found at: {gameConfigsFileName}");
                return new List<GameConfig>();
            }
        }

        public static void SaveGameConfigs(List<GameConfig> games)
        {
            try
            {
                var rootConfig = new RootConfig { Games = games };
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    Converters = { new CustomStringConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                };
                var json = JsonSerializer.Serialize(rootConfig, options);
                File.WriteAllText(gameConfigsFileName, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }

    // ********************************************************************************************************************
    // * CLASS: RootConfig
    // * This class represents the configuration of all game within the json file.
    // ********************************************************************************************************************
    public class RootConfig
    {
        public List<GameConfig> Games { get; set; }
    }

    
    // ********************************************************************************************************************
    // * CLASS: CustomStringConverter
    // * This class is used to convert a string to a JSON string.
    // ********************************************************************************************************************
    public class CustomStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
