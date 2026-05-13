using ProjektMooPing.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ProjektMooPing.Services
{
    public static class SaveService
    {
        private static string _filePath = Path.Combine(FileSystem.AppDataDirectory, "mooping_save.json");

        // Save
        public static void SaveGame(PlayerProfile profile)
        {
            try
            {
                string json = JsonSerializer.Serialize(profile);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex) { Console.WriteLine($"Save failed: {ex.Message}"); }
        }

        // Load
        public static PlayerProfile LoadGame()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<PlayerProfile>(json);
                }
            }
            catch (Exception ex) { Console.WriteLine($"Load failed: {ex.Message}"); }

            return new PlayerProfile();
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
            catch (Exception ex) { Console.WriteLine($"Delete failed: {ex.Message}"); }
        }
    }
}
