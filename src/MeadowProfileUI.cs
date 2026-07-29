using Menu;
using Menu.Remix.MixedUI;
using System;
using UnityEngine;

namespace DMSxMeadow
{
    public class MeadowProfileUI
    {
        private DressMySlugcat.FancyMenu _fancyMenu;
        private SimpleButton _meadowModeButton;
        private SimpleButton _profileNumberButton;
        private OpTextBox _steamIdField;
        private MenuLabel _statusLabel;
        private MenuLabel _steamLabel;
        private MenuLabel _profileLabel;
        
        private bool _uiAdded = false;
        
        public MeadowProfileUI(DressMySlugcat.FancyMenu fancyMenu)
        {
            _fancyMenu = fancyMenu;
        }
        
        public void Initialize()
        {
            if (_uiAdded) return;
            
            try
            {
                Plugin.Logger.LogInfo("MeadowProfileUI.Initialize() started");
                
                // ============================================================
                // 1. Buscar textBoxBorder (es PUBLIC en DMS)
                // ============================================================
                var textBoxBorderField = _fancyMenu.GetType()
                    .GetField("textBoxBorder", 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                
                if (textBoxBorderField == null)
                {
                    Plugin.Logger.LogError("textBoxBorderField is NULL!");
                    return;
                }
                
                var textBoxBorder = textBoxBorderField?.GetValue(_fancyMenu) as RoundedRect;
                if (textBoxBorder == null)
                {
                    Plugin.Logger.LogError("textBoxBorder is NULL!");
                    return;
                }
                
                Plugin.Logger.LogInfo($"textBoxBorder found: pos=({textBoxBorder.pos.x}, {textBoxBorder.pos.y}), size=({textBoxBorder.size.x}, {textBoxBorder.size.y})");
                
                // ============================================================
                // 2. Obtener cantidad de jugadores (es PUBLIC en DMS)
                // ============================================================
                var playerButtonsField = _fancyMenu.GetType()
                    .GetField("playerButtons", 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                
                int playerCount = 4; // Default
                if (playerButtonsField != null)
                {
                    var playerButtons = playerButtonsField.GetValue(_fancyMenu) as Array;
                    if (playerButtons != null)
                    {
                        playerCount = playerButtons.Length;
                    }
                }
                
                Plugin.Logger.LogInfo($"playerCount: {playerCount}");
                
                // ============================================================
                // 3. Calcular posición visible
                // ============================================================
                float leftAnchor = (1366f - _fancyMenu.manager.rainWorld.options.ScreenSize.x) / 2f;
                
                // Posición: al lado del último botón de perfil
                float startX = textBoxBorder.pos.x + (65f * playerCount) + 10f - leftAnchor;
                float yPos = textBoxBorder.pos.y - 40f;
                
                // CORRECCIÓN: Si la posición calculada es negativa o fuera de rango, usar posición fija
                if (startX < 0 || startX > 1366f)
                {
                    Plugin.Logger.LogWarning($"startX={startX} fuera de rango, usando posición fija");
                    startX = 100f;
                    yPos = 100f;
                }
                
                Plugin.Logger.LogInfo($"Button position: X={startX}, Y={yPos}");
                Plugin.Logger.LogInfo($"leftAnchor: {leftAnchor}");
                
                // ============================================================
                // 4. Crear botón MEADOW
                // ============================================================
                _meadowModeButton = new SimpleButton(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "MEADOW",
                    "MEADOW_TOGGLE",
                    new Vector2(startX, yPos),
                    new Vector2(80f, 30f)
                );
                _fancyMenu.pages[0].subObjects.Add(_meadowModeButton);
                Plugin.Logger.LogInfo($"MEADOW button added at ({startX}, {yPos})");
                
                // ============================================================
                // 5. Crear controles adicionales
                // ============================================================
                _profileLabel = new MenuLabel(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "Profile:",
                    new Vector2(startX, yPos + 35f),
                    new Vector2(60f, 20f),
                    false
                );
                _fancyMenu.pages[0].subObjects.Add(_profileLabel);
                
                _profileNumberButton = new SimpleButton(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "1",
                    "PROFILE_CHANGE",
                    new Vector2(startX + 65f, yPos + 35f),
                    new Vector2(70f, 30f)
                );
                _fancyMenu.pages[0].subObjects.Add(_profileNumberButton);
                _profileNumberButton.inactive = true;
                
                _steamLabel = new MenuLabel(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "Steam:",
                    new Vector2(startX, yPos + 70f),
                    new Vector2(60f, 20f),
                    false
                );
                _fancyMenu.pages[0].subObjects.Add(_steamLabel);
                
                var dummyOI = new DressMySlugcat.DMSOptions();
                var steamConfig = new Configurable<string>(
                    dummyOI,
                    "meadow_steamid",
                    "",
                    null
                );
                _steamIdField = new OpTextBox(
                    steamConfig,
                    new Vector2(startX + 65f, yPos + 70f),
                    100f
                );
                _steamIdField.allowSpace = false;
                _steamIdField.maxLength = 20;
                _fancyMenu.pages[0].Container.AddChild(_steamIdField.myContainer);
                
                _statusLabel = new MenuLabel(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "",
                    new Vector2(startX, yPos + 105f),
                    new Vector2(250f, 20f),
                    false
                );
                _fancyMenu.pages[0].subObjects.Add(_statusLabel);
                
                _uiAdded = true;
                Plugin.Logger.LogInfo("Meadow profile UI initialized SUCCESSFULLY!");
                
                // Eventos
                _steamIdField.OnValueChanged += OnSteamIdChanged;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error initializing meadow UI: {ex.Message}");
                Plugin.Logger.LogError(ex.StackTrace);
            }
        }
        
        private void OnSteamIdChanged(UIconfig sender, string oldValue, string newValue)
        {
            try
            {
                if (!MeadowProfileManager.IsMeadowModeActive) return;
                
                int profileNumber = MeadowProfileManager.CurrentProfileNumber;
                MeadowProfileManager.SetSteamID(profileNumber, newValue);
                _statusLabel.text = $"Steam ID saved";
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error saving Steam ID: {ex.Message}");
            }
        }
        
        private void ChangeProfileNumber()
        {
            if (!MeadowProfileManager.IsMeadowModeActive) return;
            
            int current = MeadowProfileManager.CurrentProfileNumber;
            int next = current + 1;
            if (next > 99) next = 1;
            
            SaveCurrentProfile();
            
            MeadowProfileManager.SetCurrentProfile(next);
            _profileNumberButton.menuLabel.text = next.ToString();
            
            LoadProfile(next);
            
            string steamId = MeadowProfileManager.GetSteamID(next);
            _steamIdField.value = steamId;
            
            _statusLabel.text = $"Loaded profile {next}";
            
            Plugin.Logger.LogInfo($"Switched to meadow profile {next}");
        }
        
        private void SaveCurrentProfile()
        {
            try
            {
                var customization = DressMySlugcat.Customization.For(
                    _fancyMenu.selectedSlugcat, 
                    _fancyMenu.selectedPlayerIndex
                );
                
                if (customization != null)
                {
                    MeadowProfileManager.SaveCurrentProfile(customization);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error saving profile: {ex.Message}");
            }
        }
        
        private void LoadProfile(int displayNumber)
        {
            try
            {
                var customization = MeadowProfileManager.GetProfileCustomization(displayNumber);
                if (customization != null)
                {
                    var currentCust = DressMySlugcat.Customization.For(
                        _fancyMenu.selectedSlugcat, 
                        _fancyMenu.selectedPlayerIndex
                    );
                    
                    if (currentCust != null)
                    {
                        currentCust.CustomTail.Length = customization.CustomTail.Length;
                        currentCust.CustomTail.Wideness = customization.CustomTail.Wideness;
                        currentCust.CustomTail.Roundness = customization.CustomTail.Roundness;
                        currentCust.CustomTail.Lift = customization.CustomTail.Lift;
                        currentCust.CustomTail.Color = customization.CustomTail.Color;
                        currentCust.CustomTail.CustTailShape = customization.CustomTail.CustTailShape;
                        currentCust.CustomTail.AsymTail = customization.CustomTail.AsymTail;
                        
                        currentCust.CustomSprites.Clear();
                        foreach (var sprite in customization.CustomSprites)
                        {
                            currentCust.CustomSprites.Add(new DressMySlugcat.CustomSprite
                            {
                                Sprite = sprite.Sprite,
                                SpriteSheetID = sprite.SpriteSheetID,
                                ColorHex = sprite.ColorHex,
                                Enforce = sprite.Enforce
                            });
                        }
                        
                        var dummyField = _fancyMenu.GetType()
                            .GetField("slugcatDummy", 
                                System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.Instance);
                        
                        var dummy = dummyField?.GetValue(_fancyMenu);
                        if (dummy != null)
                        {
                            var updateMethod = dummy.GetType().GetMethod("UpdateSprites", 
                                System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.Instance);
                            updateMethod?.Invoke(dummy, null);
                        }
                        
                        var updateControlsMethod = _fancyMenu.GetType()
                            .GetMethod("UpdateControls", 
                                System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.Instance);
                        updateControlsMethod?.Invoke(_fancyMenu, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error loading profile: {ex.Message}");
            }
        }
        
        public void ToggleMeadowMode()
        {
            MeadowProfileManager.IsMeadowModeActive = !MeadowProfileManager.IsMeadowModeActive;
            
            bool active = MeadowProfileManager.IsMeadowModeActive;
            
            if (active)
            {
                _profileNumberButton.inactive = false;
                _statusLabel.text = $"Meadow ON - Profile {MeadowProfileManager.CurrentProfileNumber}";
                
                int profileNumber = MeadowProfileManager.CurrentProfileNumber;
                _profileNumberButton.menuLabel.text = profileNumber.ToString();
                string steamId = MeadowProfileManager.GetSteamID(profileNumber);
                _steamIdField.value = steamId;
                LoadProfile(profileNumber);
                
                Plugin.Logger.LogInfo("Meadow mode activated");
            }
            else
            {
                _profileNumberButton.inactive = true;
                _statusLabel.text = "";
                
                SaveCurrentProfile();
                
                var dummyField = _fancyMenu.GetType()
                    .GetField("slugcatDummy", 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                
                var dummy = dummyField?.GetValue(_fancyMenu);
                if (dummy != null)
                {
                    var updateMethod = dummy.GetType().GetMethod("UpdateSprites", 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                    updateMethod?.Invoke(dummy, null);
                }
                
                var updateControlsMethod = _fancyMenu.GetType()
                    .GetMethod("UpdateControls", 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                updateControlsMethod?.Invoke(_fancyMenu, null);
                
                Plugin.Logger.LogInfo("Meadow mode deactivated");
            }
            
            _fancyMenu.PlaySound(SoundID.MENU_Switch_Page_Out);
        }
        
        public void HandleSignal(string message)
        {
            if (message == "MEADOW_TOGGLE")
            {
                ToggleMeadowMode();
            }
            else if (message == "PROFILE_CHANGE")
            {
                ChangeProfileNumber();
            }
        }
        
        public void AutoSave()
        {
            if (MeadowProfileManager.IsMeadowModeActive)
            {
                SaveCurrentProfile();
            }
        }
    }
}