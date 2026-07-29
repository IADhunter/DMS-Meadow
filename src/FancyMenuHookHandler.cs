using System;
using System.Reflection;
using Menu;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace DMSxMeadow
{
    public static class FancyMenuHookHandler
    {
        private static Hook signalHook;
        private static bool buttonAdded = false;
        private static MeadowProfileUI _meadowUI;
        
        public static void Initialize()
        {
            try
            {
                Type fancyMenuType = typeof(DressMySlugcat.FancyMenu);
                
                // ============================================================
                // HOOK DEL CONSTRUCTOR - CORREGIDO
                // ============================================================
                // El constructor de FancyMenu es: FancyMenu(ProcessManager, PauseMenu)
                // El hook DEBE tener los mismos parámetros
                
                ConstructorInfo ctor = fancyMenuType.GetConstructor(
                    new Type[] { typeof(ProcessManager), typeof(PauseMenu) });
                
                if (ctor != null)
                {
                    // En lugar de hookear el constructor, hookeamos el método Update de FancyMenu
                    // que se llama constantemente y ahí agregamos el UI
                    MethodInfo updateMethod = fancyMenuType.GetMethod("Update");
                    if (updateMethod != null)
                    {
                        MethodInfo hookUpdate = typeof(FancyMenuHookHandler)
                            .GetMethod("FancyMenu_Update_Hook", 
                                BindingFlags.NonPublic | BindingFlags.Static);
                        
                        if (hookUpdate != null)
                        {
                            var updateHook = new Hook(updateMethod, hookUpdate);
                            Plugin.Logger.LogInfo("FancyMenu.Update hooked");
                        }
                    }
                }
                
                // ============================================================
                // HOOK DEL MÉTODO Singal - ESTE SI FUNCIONA
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
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error initializing FancyMenu hooks: {ex.Message}");
            }
        }
        
        // ============================================================
        // HOOK DE UPDATE - Para agregar UI cuando se abre FancyMenu
        // ============================================================
        private static void FancyMenu_Update_Hook(
            Action<DressMySlugcat.FancyMenu> orig,
            DressMySlugcat.FancyMenu fancyMenu)
        {
            // Llamar al Update original primero
            orig(fancyMenu);
            
            try
            {
                // Agregar UI solo una vez
                if (!buttonAdded && fancyMenu != null)
                {
                    _meadowUI = new MeadowProfileUI(fancyMenu);
                    _meadowUI.Initialize();
                    buttonAdded = true;
                    Plugin.Logger.LogInfo("Meadow UI initialized from Update hook");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error adding UI in Update hook: {ex.Message}");
            }
        }
        
        // ============================================================
        // HOOK DE Singal - Para manejar los clicks en los botones
        // ============================================================
        private static void Singal_Hook(
            Action<DressMySlugcat.FancyMenu, MenuObject, string> orig,
            DressMySlugcat.FancyMenu fancyMenu,
            MenuObject sender,
            string message)
        {
            if (message == "MEADOW_TOGGLE" || message == "PROFILE_CHANGE")
            {
                try
                {
                    if (_meadowUI != null)
                    {
                        _meadowUI.HandleSignal(message);
                    }
                    // No llamamos a orig porque ya manejamos el mensaje
                    return;
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error handling meadow signal: {ex.Message}");
                }
            }
            
            // Auto-guardar en cambios de skin
            if (MeadowProfileManager.IsMeadowModeActive && 
                (message.StartsWith("SPRITE_SELECTOR_") || 
                 message.StartsWith("SPRITE_CUSTOMIZER_") || 
                 message == "TAIL_CUSTOMIZER" ||
                 message == "CUST_PASTE" ||
                 message == "CUST_DEFAULTS"))
            {
                try
                {
                    if (_meadowUI != null)
                    {
                        _meadowUI.AutoSave();
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError($"Error auto-saving: {ex.Message}");
                }
            }
            
            orig(fancyMenu, sender, message);
        }
    }
}