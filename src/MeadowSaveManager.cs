using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace DMSxMeadow
{
    public static class MeadowSaveManager
    {
        private static string RootPath => $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}dressmyslugcat{Path.DirectorySeparatorChar}";
        private static string SaveFile => RootPath + "meadowcustom.dat";
        
        private static MeadowSaveData _data;
        public static MeadowSaveData Data => _data ??= Load();
        
        public static MeadowSaveData Load()
        {
            try
            {
                if (File.Exists(SaveFile))
                {
                    using (var fs = new FileStream(SaveFile, FileMode.Open))
                    {
                        var formatter = new BinaryFormatter();
                        var loaded = (MeadowSaveData)formatter.Deserialize(fs);
                        if (loaded != null)
                        {
                            Plugin.Logger.LogInfo($"Loaded {loaded.SteamAssignments.Count} assignments");
                            return loaded;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error loading save: {ex.Message}");
            }
            
            var newData = new MeadowSaveData();
            Save(newData);
            return newData;
        }
        
        public static void Save(MeadowSaveData data = null)
        {
            try
            {
                if (data == null && _data == null) return;
                
                var toSave = data ?? _data;
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
                
                Plugin.Logger.LogInfo($"Save file updated: {SaveFile}");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error saving: {ex.Message}");
            }
        }
        
        public static void WipeSave()
        {
            try
            {
                if (File.Exists(SaveFile))
                {
                    File.Delete(SaveFile);
                    _data = new MeadowSaveData();
                    Plugin.Logger.LogInfo("Save file wiped");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error wiping save: {ex.Message}");
            }
        }
    }
}