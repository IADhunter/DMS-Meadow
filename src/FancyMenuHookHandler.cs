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
        private static Dictionary<DressMySlugcat.FancyMenu, MeadowProfileUI> _uiInstances = new Dictionary<DressMySlugcat.FancyMenu, MeadowProfileUI>();
        private static bool _uiAdded = false;
        
        public static void Initialize()
        {
            try
            {
                // ============================================================
                // HOOK 1: ProcessManager.Update (para detectar FancyMenu)
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
                // HOOK 2: FancyMenu.Singal (para manejar eventos de la UI)
                // ============================================================
                MethodInfo signalMethod = typeof(DressMySlugcat.FancyMenu).GetMethod("Singal");
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
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error initializing FancyMenu hooks: {ex.Message}");
            }
        }
        
        // ============================================================
        // HOOK DE Update - Se ejecuta cada frame
        // ============================================================
        private static void Update_Hook(
            Action<ProcessManager, float> orig,
            ProcessManager self,
            float deltaTime)
        {
            orig(self, deltaTime);
            
            try
            {
                // Verificar si currentMainLoop es FancyMenu
                if (self.currentMainLoop is DressMySlugcat.FancyMenu fancyMenu)
                {
                    if (!_uiInstances.ContainsKey(fancyMenu))
                    {
                        Plugin.Logger.LogInfo("FancyMenu detected in Update hook - creating UI");
                        var ui = new MeadowProfileUI(fancyMenu);
                        ui.Initialize();
                        _uiInstances[fancyMenu] = ui;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in Update hook: {ex.Message}");
            }
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
            // Manejar mensajes de Meadow
            if (message == "MEADOW_TOGGLE" || message == "PROFILE_CHANGE")
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
            
            // Auto-guardar cuando se cambian sprites en modo Meadow
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
            _uiInstances.Clear();
            Plugin.Logger.LogInfo("FancyMenu hooks disposed");
        }
    }
}