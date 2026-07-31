using System;
using System.Collections.Generic;
using System.Reflection;
using Menu;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace DMSxMeadow
{
    public static class FancyMenuHookHandler
    {
        private static Hook signalHook;
        private static Hook updateHook;
        private static Hook getSelectedHook;
        private static Hook setSelectedHook;
        private static Hook shutdownHook;
        private static Hook customizationFor3ArgHook;
        private static Dictionary<DressMySlugcat.FancyMenu, MeadowProfileUI> _uiInstances = new Dictionary<DressMySlugcat.FancyMenu, MeadowProfileUI>();

        private static DressMySlugcat.FancyMenu _currentFancyMenu;
        private static DressMySlugcat.Customization _liveMeadowCustomization;

        public static void Initialize()
        {
            try
            {
                Type fancyMenuType = typeof(DressMySlugcat.FancyMenu);

                // ============================================================
                // HOOK 1: ProcessManager.Update
                // ============================================================
                MethodInfo updateMethod = typeof(ProcessManager).GetMethod("Update");
                if (updateMethod != null)
                {
                    MethodInfo hookMethod = typeof(FancyMenuHookHandler)
                        .GetMethod("Update_Hook",
                            BindingFlags.NonPublic | BindingFlags.Static);

                    if (hookMethod != null)
                    {
                        updateHook = new Hook(updateMethod, hookMethod);
                        Plugin.Logger.LogInfo("ProcessManager.Update hooked");
                    }
                }

                // ============================================================
                // HOOK 2: FancyMenu.Singal
                // ============================================================
                MethodInfo signalMethod = fancyMenuType.GetMethod("Singal");
                if (signalMethod != null)
                {
                    MethodInfo hookSignal = typeof(FancyMenuHookHandler)
                        .GetMethod("Singal_Hook",
                            BindingFlags.NonPublic | BindingFlags.Static);

                    if (hookSignal != null)
                    {
                        signalHook = new Hook(signalMethod, hookSignal);
                        Plugin.Logger.LogInfo("FancyMenu.Singal hooked");
                    }
                }

                // ============================================================
                // HOOK 3: GetCurrentlySelectedOfSeries
                // ============================================================
                MethodInfo getSelMethod = fancyMenuType.GetMethod("GetCurrentlySelectedOfSeries");
                if (getSelMethod != null)
                {
                    MethodInfo hookGetSel = typeof(FancyMenuHookHandler)
                        .GetMethod("GetSelected_Hook",
                            BindingFlags.NonPublic | BindingFlags.Static);

                    if (hookGetSel != null)
                    {
                        getSelectedHook = new Hook(getSelMethod, hookGetSel);
                        Plugin.Logger.LogInfo("GetCurrentlySelectedOfSeries hooked");
                    }
                }

                // ============================================================
                // HOOK 4: SetCurrentlySelectedOfSeries
                // ============================================================
                MethodInfo setSelMethod = fancyMenuType.GetMethod("SetCurrentlySelectedOfSeries");
                if (setSelMethod != null)
                {
                    MethodInfo hookSetSel = typeof(FancyMenuHookHandler)
                        .GetMethod("SetSelected_Hook",
                            BindingFlags.NonPublic | BindingFlags.Static);

                    if (hookSetSel != null)
                    {
                        setSelectedHook = new Hook(setSelMethod, hookSetSel);
                        Plugin.Logger.LogInfo("SetCurrentlySelectedOfSeries hooked");
                    }
                }

                // ============================================================
                // HOOK 5: FancyMenu.ShutDownProcess
                // ============================================================
                MethodInfo shutdownMethod = fancyMenuType.GetMethod("ShutDownProcess");
                if (shutdownMethod != null)
                {
                    MethodInfo hookShutdown = typeof(FancyMenuHookHandler)
                        .GetMethod("ShutDownProcess_Hook",
                            BindingFlags.NonPublic | BindingFlags.Static);

                    if (hookShutdown != null)
                    {
                        shutdownHook = new Hook(shutdownMethod, hookShutdown);
                        Plugin.Logger.LogInfo("FancyMenu.ShutDownProcess hooked (security)");
                    }
                }

                // ============================================================
                // HOOK 6: Customization.For(string, int, bool) - NUEVO
                // ============================================================
                MethodInfo for3Arg = typeof(DressMySlugcat.Customization)
                    .GetMethod("For", new Type[] { typeof(string), typeof(int), typeof(bool) });

                if (for3Arg != null)
                {
                    MethodInfo hookFor3Arg = typeof(FancyMenuHookHandler)
                        .GetMethod("Customization_For3Arg_Hook",
                            BindingFlags.NonPublic | BindingFlags.Static);

                    if (hookFor3Arg != null)
                    {
                        customizationFor3ArgHook = new Hook(for3Arg, hookFor3Arg);
                        Plugin.Logger.LogInfo("Customization.For(string,int,bool) hooked (Meadow live object)");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error initializing FancyMenu hooks: {ex.Message}");
            }
        }

        // ============================================================
        // HOOK DE Customization.For(string, int, bool)
        // ============================================================
        private static DressMySlugcat.Customization Customization_For3Arg_Hook(
            Func<string, int, bool, DressMySlugcat.Customization> orig,
            string slugcatName,
            int playerNumber,
            bool mergeDefaults)
        {
            if (MeadowProfileManager.IsMeadowModeActive
                && _currentFancyMenu != null
                && slugcatName == _currentFancyMenu.selectedSlugcat
                && playerNumber == _currentFancyMenu.selectedPlayerIndex)
            {
                if (_liveMeadowCustomization == null)
                {
                    _liveMeadowCustomization = new DressMySlugcat.Customization
                    {
                        Slugcat = slugcatName,
                        PlayerNumber = playerNumber
                    };
                    Plugin.Logger.LogInfo($"Created live Meadow customization object for {slugcatName}:{playerNumber}");
                }

                if (_liveMeadowCustomization.Slugcat != slugcatName)
                    _liveMeadowCustomization.Slugcat = slugcatName;
                if (_liveMeadowCustomization.PlayerNumber != playerNumber)
                    _liveMeadowCustomization.PlayerNumber = playerNumber;

                return _liveMeadowCustomization;
            }

            return orig(slugcatName, playerNumber, mergeDefaults);
        }

        // ============================================================
        // HOOK DE Update
        // ============================================================
        private static void Update_Hook(
            Action<ProcessManager, float> orig,
            ProcessManager self,
            float deltaTime)
        {
            orig(self, deltaTime);

            try
            {
                if (self.currentMainLoop is DressMySlugcat.FancyMenu fancyMenu)
                {
                    _currentFancyMenu = fancyMenu;
                    if (!_uiInstances.ContainsKey(fancyMenu))
                    {
                        Plugin.Logger.LogInfo("FancyMenu detected as currentMainLoop - creating UI");
                        var ui = new MeadowProfileUI(fancyMenu);
                        ui.Initialize();
                        _uiInstances[fancyMenu] = ui;
                    }
                    else
                    {
                        if (_uiInstances.TryGetValue(fancyMenu, out var ui))
                        {
                            ui.CheckFieldFocusLoss();
                        }
                    }
                }

                if (self.dialog is DressMySlugcat.FancyMenu fancyMenuDialog)
                {
                    _currentFancyMenu = fancyMenuDialog;
                    if (!_uiInstances.ContainsKey(fancyMenuDialog))
                    {
                        Plugin.Logger.LogInfo("FancyMenu detected as dialog (pause menu) - creating UI");
                        var ui = new MeadowProfileUI(fancyMenuDialog);
                        ui.Initialize();
                        _uiInstances[fancyMenuDialog] = ui;
                    }
                    else
                    {
                        if (_uiInstances.TryGetValue(fancyMenuDialog, out var ui))
                        {
                            ui.CheckFieldFocusLoss();
                        }
                    }
                }

                foreach (var process in self.sideProcesses)
                {
                    if (process is DressMySlugcat.FancyMenu fancyMenuSide)
                    {
                        _currentFancyMenu = fancyMenuSide;
                        if (!_uiInstances.ContainsKey(fancyMenuSide))
                        {
                            Plugin.Logger.LogInfo("FancyMenu detected in sideProcesses - creating UI");
                            var ui = new MeadowProfileUI(fancyMenuSide);
                            ui.Initialize();
                            _uiInstances[fancyMenuSide] = ui;
                        }
                        else
                        {
                            if (_uiInstances.TryGetValue(fancyMenuSide, out var ui))
                            {
                                ui.CheckFieldFocusLoss();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in Update hook: {ex.Message}");
            }
        }

        // ============================================================
        // HOOK DE GetCurrentlySelectedOfSeries
        // ============================================================
        private static int GetSelected_Hook(
            Func<DressMySlugcat.FancyMenu, string, int> orig,
            DressMySlugcat.FancyMenu self,
            string series)
        {
            if (series == "MEADOW_SERIES")
            {
                return MeadowProfileManager.IsMeadowModeActive ? 0 : -1;
            }

            if (MeadowProfileManager.IsMeadowModeActive && series.StartsWith("PLAYER_"))
            {
                return -1;
            }

            return orig(self, series);
        }

        // ============================================================
        // HOOK DE SetCurrentlySelectedOfSeries
        // ============================================================
        private static void SetSelected_Hook(
            Action<DressMySlugcat.FancyMenu, string, int> orig,
            DressMySlugcat.FancyMenu self,
            string series,
            int to)
        {
            if (series == "MEADOW_SERIES")
            {
                if (_uiInstances.TryGetValue(self, out var ui))
                {
                    ui.ToggleMeadowMode();
                }
                return;
            }

            if (series.StartsWith("PLAYER_") && MeadowProfileManager.IsMeadowModeActive)
            {
                Plugin.Logger.LogInfo($"Player button clicked while Meadow active - deactivating Meadow first");
                if (_uiInstances.TryGetValue(self, out var ui))
                {
                    ui.DeactivateMeadowMode();
                }
            }

            orig(self, series, to);
        }

        // ============================================================
        // HOOK DE ShutDownProcess
        // ============================================================
        private static void ShutDownProcess_Hook(
            Action<DressMySlugcat.FancyMenu> orig,
            DressMySlugcat.FancyMenu self)
        {
            try
            {
                if (_uiInstances.TryGetValue(self, out var ui))
                {
                    if (MeadowProfileManager.IsMeadowModeActive)
                    {
                        Plugin.Logger.LogWarning("Shutting down FancyMenu while Meadow active - forcing deactivation!");
                        ui.ForceDeactivateMeadowMode();
                    }

                    _uiInstances.Remove(self);
                    _currentFancyMenu = null;
                    Plugin.Logger.LogInfo("Removed FancyMenu instance from UI cache");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in ShutDownProcess hook: {ex.Message}");
            }

            orig(self);
        }

        // ============================================================
        // HOOK DE Singal - CON AUTO-SAVE EN CADA CAMBIO
        // ============================================================
        private static void Singal_Hook(
            Action<DressMySlugcat.FancyMenu, MenuObject, string> orig,
            DressMySlugcat.FancyMenu fancyMenu,
            MenuObject sender,
            string message)
        {
            if (message == "MEADOW_TOGGLE" || message == "PROFILE_SET")
            {
                try
                {
                    if (_uiInstances.TryGetValue(fancyMenu, out var ui))
                    {
                        ui.HandleSignal(message);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error handling meadow signal: {ex.Message}");
                }
            }

            // ============================================================
            // GUARDAR INMEDIATAMENTE EN CUALQUIER CAMBIO DE PERSONALIZACIÓN
            // ============================================================
            if (MeadowProfileManager.IsMeadowModeActive &&
                (message.StartsWith("SPRITE_SELECTOR_") ||
                 message.StartsWith("SPRITE_CUSTOMIZER_") ||
                 message == "TAIL_CUSTOMIZER" ||
                 message == "CUST_PASTE" ||
                 message == "CUST_DEFAULTS"))
            {
                try
                {
                    if (_uiInstances.TryGetValue(fancyMenu, out var ui))
                    {
                        ui.SaveCurrentProfile();
                        Plugin.Logger.LogInfo($"Auto-saved meadow profile after: {message}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error auto-saving: {ex.Message}");
                }
            }

            orig(fancyMenu, sender, message);
        }

        // ============================================================
        // LIMPIEZA
        // ============================================================
        public static void Dispose()
        {
            updateHook?.Dispose();
            signalHook?.Dispose();
            getSelectedHook?.Dispose();
            setSelectedHook?.Dispose();
            shutdownHook?.Dispose();
            customizationFor3ArgHook?.Dispose();
            _uiInstances.Clear();
            _currentFancyMenu = null;
            _liveMeadowCustomization = null;
            Plugin.Logger.LogInfo("FancyMenu hooks disposed");
        }
    }
}