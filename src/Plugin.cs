using BepInEx;
using BepInEx.Logging;
using System;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace DMSxMeadow
{
    [BepInPlugin("dmsxmeadow", "DMS x Meadow", "2.0.0")]
    [BepInDependency("dressmyslugcat", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("henpemaz.rainmeadow", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;
        public static new ManualLogSource Logger;
        
        private Hook customizationHook;
        
        public void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            
            try
            {
                Logger.LogInfo("Initializing DMS x Meadow v2.0...");
                
                MeadowProfileManager.Load();
                InitializeHooks();
                FancyMenuHookHandler.Initialize();
                
                Logger.LogInfo("DMS x Meadow initialized successfully!");
                Logger.LogInfo($"Save path: {Application.persistentDataPath}/dressmyslugcat/meadowcustom.dat");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Initialization error: {ex.Message}");
                Logger.LogError(ex.StackTrace);
            }
        }
        
        private void InitializeHooks()
        {
            try
            {
                // Hookear Customization.For(Player, bool)
                MethodInfo originalFor = typeof(DressMySlugcat.Customization)
                    .GetMethod("For", new Type[] { typeof(Player), typeof(bool) });
                
                if (originalFor != null)
                {
                    MethodInfo hookFor = typeof(Plugin)
                        .GetMethod("Customization_For_Hook", BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (hookFor != null)
                    {
                        customizationHook = new Hook(originalFor, hookFor);
                        Logger.LogInfo("Customization.For hook applied");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Hook application error: {ex.Message}");
            }
        }
        
        // ============================================================
        // HOOK PARA APLICAR SKINS DE MEADOW POR STEAMID
        // ============================================================
        private static DressMySlugcat.Customization Customization_For_Hook(
            Func<Player, bool, DressMySlugcat.Customization> orig,
            Player player,
            bool mergeDefaults)
        {
            try
            {
                if (player?.abstractCreature != null)
                {
                    if (RainMeadow.OnlinePhysicalObject.map.TryGetValue(
                        player.abstractCreature, out var onlineEntity))
                    {
                        var owner = onlineEntity.owner;
                        if (owner != null && !owner.isMe && owner.id != null)
                        {
                            string steamId = owner.id.ToString();
                            
                            var customization = MeadowProfileManager.GetCustomizationBySteamID(steamId);
                            if (customization != null)
                            {
                                Logger.LogInfo($"Found meadow profile for SteamID: {steamId}");
                                var result = customization.Copy();
                                result.PlayerNumber = 0;
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Hook error: {ex.Message}");
            }
            
            return orig(player, mergeDefaults);
        }
        
        public void OnDestroy()
        {
            customizationHook?.Dispose();
            FancyMenuHookHandler.Dispose();
            Logger.LogInfo("DMS x Meadow unloaded");
        }
    }
}