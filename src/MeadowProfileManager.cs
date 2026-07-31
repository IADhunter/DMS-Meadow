using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            // PRIMERO: borrar cualquier entrada vieja de este perfil
            var oldKeys = new List<string>();
            foreach (var kvp in _assignments)
                if (kvp.Value == displayNumber) oldKeys.Add(kvp.Key);
            foreach (var k in oldKeys) _assignments.Remove(k);

            // Después, si el nuevo SteamID no está vacío, asignarlo
            if (!string.IsNullOrEmpty(steamID))
            {
                // Si el SteamID ya existe en otra entrada, la sobreescribimos (reasignación)
                _assignments[steamID] = displayNumber;
            }

            SaveAssignments();
            Plugin.Logger.LogInfo($"SteamID assignment updated: profile {displayNumber} -> '{steamID}'");
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
        // MÉTODOS PARA EL MENÚ REMIX
        // ============================================================

        // ============================================================
        // OBTENER TODOS LOS PERFILES (UNIÓN DE AMBAS FUENTES)
        // ============================================================
        public static List<int> GetAllProfileNumbers()
        {
            var result = new HashSet<int>();

            try
            {
                // Perfiles de meadowcustom.dat
                if (Database.Profiles != null)
                {
                    foreach (int internalNum in Database.Profiles.Keys)
                    {
                        int displayNum = GetDisplayNumber(internalNum);
                        if (displayNum >= 1 && displayNum <= Database.MaxProfiles)
                        {
                            result.Add(displayNum);
                        }
                    }
                }

                // Perfiles con asignación SteamID
                LoadAssignments();
                foreach (var kvp in _assignments)
                {
                    if (kvp.Value >= 1 && kvp.Value <= Database.MaxProfiles)
                    {
                        result.Add(kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error getting all profile numbers: {ex.Message}");
            }

            // FIX CS1061: ToList() disponible con using System.Linq;
            return result.ToList();
        }

        // ============================================================
        // ELIMINAR PERFIL (de meadowcustom.dat)
        // ============================================================
        public static void DeleteProfile(int displayNumber)
        {
            try
            {
                int internalNum = GetInternalProfile(displayNumber);
                if (Database.Profiles != null && Database.Profiles.ContainsKey(internalNum))
                {
                    Database.Profiles.Remove(internalNum);
                    Save();
                    Plugin.Logger.LogInfo($"Deleted profile {displayNumber} from meadowcustom.dat");
                }
                else
                {
                    Plugin.Logger.LogInfo($"Profile {displayNumber} not found in meadowcustom.dat");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error deleting profile {displayNumber}: {ex.Message}");
            }
        }

        // ============================================================
        // ELIMINAR ASIGNACIÓN STEAMID (del txt)
        // ============================================================
        public static void RemoveAssignment(int displayNumber)
        {
            try
            {
                LoadAssignments();

                var keysToRemove = new List<string>();
                foreach (var kvp in _assignments)
                {
                    if (kvp.Value == displayNumber)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (string key in keysToRemove)
                {
                    _assignments.Remove(key);
                    Plugin.Logger.LogInfo($"Removed assignment for profile {displayNumber}: '{key}'");
                }

                if (keysToRemove.Count > 0)
                {
                    SaveAssignments();
                }
                else
                {
                    Plugin.Logger.LogInfo($"No assignment found for profile {displayNumber}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error removing assignment for profile {displayNumber}: {ex.Message}");
            }
        }

        // ============================================================
        // VERIFICAR SI UN PERFIL EXISTE
        // ============================================================
        public static bool ProfileExists(int displayNumber)
        {
            try
            {
                int internalNum = GetInternalProfile(displayNumber);
                if (Database.Profiles != null && Database.Profiles.ContainsKey(internalNum))
                {
                    return true;
                }

                LoadAssignments();
                foreach (var kvp in _assignments)
                {
                    if (kvp.Value == displayNumber)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error checking profile existence: {ex.Message}");
            }

            return false;
        }

        // ============================================================
        // ELIMINAR TODOS LOS PERFILES HUÉRFANOS
        // ============================================================
        public static int DeleteOrphanProfiles()
        {
            int deleted = 0;
            try
            {
                LoadAssignments();

                // Obtener todos los perfiles con asignación
                var assignedProfiles = new HashSet<int>();
                foreach (var kvp in _assignments)
                {
                    assignedProfiles.Add(kvp.Value);
                }

                // Recorrer perfiles de meadowcustom.dat
                if (Database.Profiles != null)
                {
                    var toRemove = new List<int>();
                    foreach (int internalNum in Database.Profiles.Keys)
                    {
                        int displayNum = GetDisplayNumber(internalNum);
                        if (!assignedProfiles.Contains(displayNum))
                        {
                            toRemove.Add(internalNum);
                        }
                    }

                    foreach (int internalNum in toRemove)
                    {
                        Database.Profiles.Remove(internalNum);
                        deleted++;
                        Plugin.Logger.LogInfo($"Deleted orphan profile {GetDisplayNumber(internalNum)}");
                    }

                    if (deleted > 0)
                    {
                        Save();
                    }
                }

                Plugin.Logger.LogInfo($"Deleted {deleted} orphan profiles");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error deleting orphan profiles: {ex.Message}");
            }

            return deleted;
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

        // ============================================================
        // DIAGNÓSTICO: Mostrar todos los perfiles en log
        // ============================================================
        public static void LogAllProfiles()
        {
            var allProfiles = GetAllProfileNumbers();
            Plugin.Logger.LogInfo($"=== PROFILES ({allProfiles.Count}) ===");
            foreach (int profileNum in allProfiles.OrderBy(p => p))
            {
                string steamId = GetSteamID(profileNum);
                bool hasData = Database.Profiles != null && Database.Profiles.ContainsKey(GetInternalProfile(profileNum));
                Plugin.Logger.LogInfo($"  Profile {profileNum}: SteamID='{steamId}', HasData={hasData}");
            }
            Plugin.Logger.LogInfo("=== END PROFILES ===");
        }
    }
}