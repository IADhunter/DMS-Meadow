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
        private bool isInit = false;

        public void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            try
            {
                Logger.LogInfo("Initializing DMS x Meadow v2.0...");

                On.RainWorld.OnModsInit += OnModsInit;

                Logger.LogInfo("DMS x Meadow initialized successfully!");
                Logger.LogInfo($"Profiles save path: {Application.persistentDataPath}/dressmyslugcat/meadowcustom.dat");
                Logger.LogInfo($"Assignments save path: {Application.persistentDataPath}/dmsxmeadow/dmsxmeadow.txt");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Initialization error: {ex.Message}");
                Logger.LogError(ex.StackTrace);
            }
        }

        private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            try
            {
                if (isInit) return;
                isInit = true;

                Logger.LogInfo("Registering DMSxMeadow Options...");
                MachineConnector.SetRegisteredOI("dmsxmeadow", DMSxMeadowOptions.Instance);

                MeadowProfileManager.Load();
                MeadowProfileManager.LogAllAssignments();

                InitializeHooks();
                FancyMenuHookHandler.Initialize();

                Logger.LogInfo("DMS x Meadow fully initialized!");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in OnModsInit: {ex.Message}");
                Logger.LogError(ex.StackTrace);
            }
        }

        private void InitializeHooks()
        {
            try
            {
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

                        if (owner != null && owner.id != null)
                        {
                            string steamId;
                            if (owner.id is RainMeadow.SteamMatchmakingManager.SteamPlayerId steamPlayerId)
                            {
                                steamId = steamPlayerId.steamID.m_SteamID.ToString();
                                Logger.LogInfo($"Detected SteamPlayerId: {steamId} (isMe: {owner.isMe})");
                            }
                            else
                            {
                                steamId = owner.id.ToString();
                                Logger.LogInfo($"Detected non-Steam PlayerId: {steamId} (isMe: {owner.isMe})");
                            }

                            // Obtener el slugcat exacto de este jugador
                            string slugcatName = player.slugcatStats.name.value;

                            var customization = MeadowProfileManager.GetCustomizationBySteamID(steamId, slugcatName);
                            if (customization != null)
                            {
                                Logger.LogInfo($"✅ Found meadow profile for SteamID: {steamId}, slugcat: {slugcatName} (isMe: {owner.isMe})");
                                var result = customization.Copy();
                                result.PlayerNumber = 0;

                                Logger.LogInfo($"   - Tail.Length: {result.CustomTail.Length}");
                                Logger.LogInfo($"   - CustomSprites: {result.CustomSprites.Count}");

                                return result;
                            }
                            else
                            {
                                Logger.LogInfo($"No meadow profile found for SteamID: {steamId}, slugcat: {slugcatName} (isMe: {owner.isMe})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Hook error: {ex.Message}");
                Logger.LogError(ex.StackTrace);
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