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
        private static Dictionary<DressMySlugcat.FancyMenu, MeadowProfileUI> _uiInstances = new Dictionary<DressMySlugcat.FancyMenu, MeadowProfileUI>();
        
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
                // HOOK 5: FancyMenu.ShutDownProcess - PARCHE DE SEGURIDAD
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
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error initializing FancyMenu hooks: {ex.Message}");
            }
        }
        
        // ============================================================
        // HOOK DE Update - CON LLAMADA A CheckFieldFocusLoss
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
                    if (!_uiInstances.ContainsKey(fancyMenu))
                    {
                        Plugin.Logger.LogInfo("FancyMenu detected in Update hook - creating UI");
                        var ui = new MeadowProfileUI(fancyMenu);
                        ui.Initialize();
                        _uiInstances[fancyMenu] = ui;
                    }
                    else
                    {
                        // ============================================================
                        // LLAMAR CADA FRAME PARA DETECTAR PÉRDIDA DE FOCO
                        // ============================================================
                        if (_uiInstances.TryGetValue(fancyMenu, out var ui))
                        {
                            ui.CheckFieldFocusLoss();
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
        // HOOK DE ShutDownProcess - PARCHE DE SEGURIDAD
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
        // HOOK DE Singal
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
                        ui.AutoSave();
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
        // Limpieza
        // ============================================================
        public static void Dispose()
        {
            updateHook?.Dispose();
            signalHook?.Dispose();
            getSelectedHook?.Dispose();
            setSelectedHook?.Dispose();
            shutdownHook?.Dispose();
            _uiInstances.Clear();
            Plugin.Logger.LogInfo("FancyMenu hooks disposed");
        }
    }
}