using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace DMSxMeadow
{
    [Serializable]
    public class MeadowProfileData
    {
        public int InternalProfileNumber; // 5, 6, 7, ...
        public string SteamID = "";
        public DressMySlugcat.Customization Customization;
        public DateTime LastUpdated = DateTime.Now;
    }

    [Serializable]
    public class MeadowDatabase
    {
        public Dictionary<int, MeadowProfileData> Profiles = new Dictionary<int, MeadowProfileData>();
        public int MaxProfiles = 99;
        public DateTime LastUpdated = DateTime.Now;
    }

    public static class MeadowProfileManager
    {
        private static string RootPath => $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}dressmyslugcat{Path.DirectorySeparatorChar}";
        private static string SaveFile => RootPath + "meadowcustom.dat";
        
        private static MeadowDatabase _database;
        public static MeadowDatabase Database => _database ??= Load();
        
        public static int CurrentProfileNumber = 1; // 1-99 (se mapea a 5-103 internamente)
        public static bool IsMeadowModeActive = false;
        
        private const int PROFILE_OFFSET = 4; // Los perfiles 1-4 son de DMS, nosotros usamos 5+
        
        public static int GetInternalProfile(int displayNumber) => displayNumber + PROFILE_OFFSET;
        public static int GetDisplayNumber(int internalNumber) => internalNumber - PROFILE_OFFSET;
        
        public static MeadowDatabase Load()
        {
            try
            {
                if (File.Exists(SaveFile))
                {
                    using (var fs = new FileStream(SaveFile, FileMode.Open))
                    {
                        var formatter = new BinaryFormatter();
                        var loaded = (MeadowDatabase)formatter.Deserialize(fs);
                        if (loaded != null)
                        {
                            Plugin.Logger.LogInfo($"Loaded {loaded.Profiles.Count} meadow profiles");
                            return loaded;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error loading meadow profiles: {ex.Message}");
            }
            
            var newDb = new MeadowDatabase();
            Save(newDb);
            return newDb;
        }
        
        public static void Save(MeadowDatabase db = null)
        {
            try
            {
                var toSave = db ?? _database;
                if (toSave == null) return;
                
                toSave.LastUpdated = DateTime.Now;
                
                if (!Directory.Exists(RootPath))
                {
                    Directory.CreateDirectory(RootPath);
                }
                
                using (var fs = new FileStream(SaveFile, FileMode.Create))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fs, toSave);
                }
                
                Plugin.Logger.LogInfo($"Meadow profiles saved: {SaveFile}");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error saving meadow profiles: {ex.Message}");
            }
        }
        
        public static MeadowProfileData GetOrCreateProfile(int displayNumber)
        {
            if (displayNumber < 1 || displayNumber > Database.MaxProfiles)
            {
                Plugin.Logger.LogWarning($"Profile {displayNumber} out of range (1-{Database.MaxProfiles})");
                return null;
            }
            
            int internalNumber = GetInternalProfile(displayNumber);
            
            if (Database.Profiles == null)
            {
                Database.Profiles = new Dictionary<int, MeadowProfileData>();
            }
            
            if (!Database.Profiles.TryGetValue(internalNumber, out var profile))
            {
                profile = new MeadowProfileData
                {
                    InternalProfileNumber = internalNumber,
                    SteamID = "",
                    Customization = null
                };
                Database.Profiles[internalNumber] = profile;
                Save();
            }
            return profile;
        }
        
        public static void SetCurrentProfile(int displayNumber)
        {
            if (displayNumber < 1 || displayNumber > Database.MaxProfiles)
            {
                Plugin.Logger.LogWarning($"Profile {displayNumber} out of range");
                return;
            }
            
            CurrentProfileNumber = displayNumber;
            var profile = GetOrCreateProfile(displayNumber);
            
            if (profile.Customization != null)
            {
                Plugin.Logger.LogInfo($"Loaded meadow profile {displayNumber} (internal: {profile.InternalProfileNumber}) with SteamID: {profile.SteamID}");
            }
            else
            {
                Plugin.Logger.LogInfo($"New meadow profile {displayNumber} created (internal: {profile.InternalProfileNumber})");
            }
        }
        
        public static void SaveCurrentProfile(DressMySlugcat.Customization customization)
        {
            if (!IsMeadowModeActive) return;
            if (customization == null) return;
            
            var profile = GetOrCreateProfile(CurrentProfileNumber);
            if (profile == null) return;
            
            // Guardar copia
            profile.Customization = customization.Copy();
            profile.LastUpdated = DateTime.Now;
            Save();
            
            Plugin.Logger.LogInfo($"Saved meadow profile {CurrentProfileNumber} (internal: {profile.InternalProfileNumber})");
        }
        
        public static void SetSteamID(int displayNumber, string steamID)
        {
            var profile = GetOrCreateProfile(displayNumber);
            if (profile == null) return;
            
            profile.SteamID = steamID ?? "";
            Save();
        }
        
        public static string GetSteamID(int displayNumber)
        {
            var profile = GetOrCreateProfile(displayNumber);
            return profile?.SteamID ?? "";
        }
        
        public static DressMySlugcat.Customization GetProfileCustomization(int displayNumber)
        {
            var profile = GetOrCreateProfile(displayNumber);
            return profile?.Customization;
        }
        
        // Método para obtener la customización por SteamID (para el hook)
        public static DressMySlugcat.Customization GetCustomizationBySteamID(string steamId)
        {
            foreach (var kvp in Database.Profiles)
            {
                if (kvp.Value.SteamID == steamId && kvp.Value.Customization != null)
                {
                    return kvp.Value.Customization;
                }
            }
            return null;
        }
    }
}