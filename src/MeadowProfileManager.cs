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
        public int InternalProfileNumber; // 5, 6, 7, ... (offset +4)
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
        // ============================================================
        // ARCHIVO 1: Datos de ropa de perfiles extendidos (meadowcustom.dat)
        // ============================================================
        private static string RootPath => $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}dressmyslugcat{Path.DirectorySeparatorChar}";
        private static string SaveFile => RootPath + "meadowcustom.dat";
        
        // ============================================================
        // ARCHIVO 2: Asignaciones SteamID -> Perfil (dmsxmeadow.txt)
        // ============================================================
        private static string AssignmentsRootPath => $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}dmsxmeadow{Path.DirectorySeparatorChar}";
        private static string AssignmentsFile => AssignmentsRootPath + "dmsxmeadow.txt";
        
        private static MeadowDatabase _database;
        public static MeadowDatabase Database => _database ??= Load();
        
        public static int CurrentProfileNumber = 1; // 1-99 (se mapea a 5-103 internamente)
        public static bool IsMeadowModeActive = false;
        
        private const int PROFILE_OFFSET = 4; // Los perfiles 1-4 son de DMS, nosotros usamos 5+
        
        // ============================================================
        // MAPA DE ASIGNACIONES SteamID -> ProfileNumber (en memoria)
        // ============================================================
        private static Dictionary<string, int> _assignments = new Dictionary<string, int>();
        private static bool _assignmentsLoaded = false;
        
        public static int GetInternalProfile(int displayNumber) => displayNumber + PROFILE_OFFSET;
        public static int GetDisplayNumber(int internalNumber) => internalNumber - PROFILE_OFFSET;
        
        // ============================================================
        // CARGAR / GUARDAR PERFILES EXTENDIDOS (meadowcustom.dat)
        // ============================================================
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
        
        // ============================================================
        // CARGAR / GUARDAR ASIGNACIONES (dmsxmeadow.txt)
        // ============================================================
        private static void LoadAssignments()
        {
            if (_assignmentsLoaded) return;
            _assignmentsLoaded = true;
            _assignments.Clear();
            
            try
            {
                if (File.Exists(AssignmentsFile))
                {
                    var lines = File.ReadAllLines(AssignmentsFile);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        // Saltar líneas vacías y comentarios
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                        
                        var parts = trimmed.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int profileNumber))
                        {
                            var steamId = parts[0].Trim();
                            if (!string.IsNullOrEmpty(steamId))
                            {
                                _assignments[steamId] = profileNumber;
                                Plugin.Logger.LogInfo($"Loaded assignment: '{steamId}' -> profile {profileNumber}");
                            }
                        }
                    }
                }
                else
                {
                    Plugin.Logger.LogInfo($"No assignments file found at {AssignmentsFile}, creating new one");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error loading assignments: {ex.Message}");
            }
        }
        
        public static void SaveAssignments()
        {
            try
            {
                if (!Directory.Exists(AssignmentsRootPath))
                {
                    Directory.CreateDirectory(AssignmentsRootPath);
                }
                
                var lines = new List<string>();
                lines.Add("# SteamID:ProfileNumber");
                lines.Add("# Format: STEAM_0:1:12345678:5");
                lines.Add("# or 76561198000000000:6");
                lines.Add("");
                
                foreach (var kvp in _assignments)
                {
                    lines.Add($"{kvp.Key}:{kvp.Value}");
                }
                
                File.WriteAllLines(AssignmentsFile, lines);
                Plugin.Logger.LogInfo($"Assignments saved: {AssignmentsFile} ({_assignments.Count} entries)");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error saving assignments: {ex.Message}");
            }
        }
        
        // ============================================================
        // OPERACIONES CON PERFILES
        // ============================================================
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
                Plugin.Logger.LogInfo($"Loaded meadow profile {displayNumber} (internal: {profile.InternalProfileNumber})");
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
            
            profile.Customization = customization.Copy();
            profile.LastUpdated = DateTime.Now;
            Save();
            
            Plugin.Logger.LogInfo($"Saved meadow profile {CurrentProfileNumber} (internal: {profile.InternalProfileNumber})");
        }
        
        public static DressMySlugcat.Customization GetProfileCustomization(int displayNumber)
        {
            var profile = GetOrCreateProfile(displayNumber);
            return profile?.Customization;
        }
        
        // ============================================================
        // OPERACIONES CON ASIGNACIONES SteamID
        // ============================================================
        public static void SetSteamID(int displayNumber, string steamID)
        {
            LoadAssignments();
            
            // Si SteamID está vacío, eliminar la asignación
            if (string.IsNullOrEmpty(steamID))
            {
                // Buscar y eliminar cualquier asignación existente para este perfil
                string keyToRemove = null;
                foreach (var kvp in _assignments)
                {
                    if (kvp.Value == displayNumber)
                    {
                        keyToRemove = kvp.Key;
                        break;
                    }
                }
                if (keyToRemove != null)
                {
                    _assignments.Remove(keyToRemove);
                    Plugin.Logger.LogInfo($"Removed assignment for profile {displayNumber}");
                }
                SaveAssignments();
                return;
            }
            
            // Si el SteamID ya existe en otra asignación, eliminarlo primero
            if (_assignments.ContainsKey(steamID))
            {
                var existingProfile = _assignments[steamID];
                if (existingProfile != displayNumber)
                {
                    Plugin.Logger.LogWarning($"SteamID '{steamID}' was assigned to profile {existingProfile}, reassigning to {displayNumber}");
                }
            }
            
            _assignments[steamID] = displayNumber;
            SaveAssignments();
            Plugin.Logger.LogInfo($"Assigned SteamID '{steamID}' -> profile {displayNumber}");
        }
        
        public static string GetSteamID(int displayNumber)
        {
            LoadAssignments();
            
            foreach (var kvp in _assignments)
            {
                if (kvp.Value == displayNumber)
                {
                    return kvp.Key;
                }
            }
            return "";
        }
        
        public static int GetProfileBySteamID(string steamID)
        {
            LoadAssignments();
            
            if (string.IsNullOrEmpty(steamID)) return -1;
            
            if (_assignments.TryGetValue(steamID, out int profileNumber))
            {
                return profileNumber;
            }
            return -1;
        }
        
        public static DressMySlugcat.Customization GetCustomizationBySteamID(string steamId)
        {
            LoadAssignments();
            
            if (string.IsNullOrEmpty(steamId)) return null;
            
            if (_assignments.TryGetValue(steamId, out int profileNumber))
            {
                return GetProfileCustomization(profileNumber);
            }
            return null;
        }
        
        // ============================================================
        // DIAGNÓSTICO: Mostrar todas las asignaciones en log
        // ============================================================
        public static void LogAllAssignments()
        {
            LoadAssignments();
            
            Plugin.Logger.LogInfo($"=== ASSIGNMENTS ({_assignments.Count}) ===");
            foreach (var kvp in _assignments)
            {
                Plugin.Logger.LogInfo($"  {kvp.Key} -> profile {kvp.Value}");
            }
            Plugin.Logger.LogInfo("=== END ASSIGNMENTS ===");
        }
    }
}