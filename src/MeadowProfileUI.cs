using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using System;
using UnityEngine;

namespace DMSxMeadow
{
    public class MeadowProfileUI
    {
        private DressMySlugcat.FancyMenu _fancyMenu;
        private SimpleButton _meadowModeButton;
        private OpTextBox _steamIdField;
        private OpTextBox _profileNumberField;
        private SimpleButton _profileSetButton;
        private MenuLabel _statusLabel;
        private MenuLabel _steamLabel;
        private MenuLabel _profileLabel;
        
        // El wrapper que permite que los OpTextBox funcionen fuera de ConfigContainer
        private MenuTabWrapper _tabWrapper;
        
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
                
                int playerCount = 4;
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
                
                float startX = textBoxBorder.pos.x + (65f * playerCount) + 10f - leftAnchor;
                float yPos = textBoxBorder.pos.y - 40f;
                
                if (startX < 0 || startX > 1366f)
                {
                    Plugin.Logger.LogWarning($"startX={startX} fuera de rango, usando posición fija");
                    startX = 100f;
                    yPos = 100f;
                }
                
                Plugin.Logger.LogInfo($"Button position: X={startX}, Y={yPos}");
                
                // ============================================================
                // 4. Crear MenuTabWrapper - Necesario para OpTextBox
                // ============================================================
                _tabWrapper = new MenuTabWrapper(_fancyMenu, _fancyMenu.pages[0]);
                _fancyMenu.pages[0].subObjects.Add(_tabWrapper);
                Plugin.Logger.LogInfo("MenuTabWrapper created and added to page");
                
                // ============================================================
                // 5. Crear botón MEADOW
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
                // 6. Crear controles: Profile (con campo escribible + botón SET)
                // ============================================================
                float yOffset = 0f;
                
                _profileLabel = new MenuLabel(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "Profile:",
                    new Vector2(startX, yPos + 35f + yOffset),
                    new Vector2(60f, 20f),
                    false
                );
                _fancyMenu.pages[0].subObjects.Add(_profileLabel);
                
                // --- Campo para escribir el número de perfil ---
                var dummyOI = new DressMySlugcat.DMSOptions();
                var profileNumberConfig = new Configurable<string>(
                    dummyOI,
                    "meadow_profile_number",
                    "1",
                    null
                );
                _profileNumberField = new OpTextBox(
                    profileNumberConfig,
                    new Vector2(startX + 65f, yPos + 35f + yOffset),
                    60f
                );
                _profileNumberField.allowSpace = false;
                _profileNumberField.maxLength = 3;
                
                // Usar UIelementWrapper en lugar de Container.AddChild
                new UIelementWrapper(_tabWrapper, _profileNumberField);
                Plugin.Logger.LogInfo($"Profile number field added at ({startX + 65f}, {yPos + 35f + yOffset})");
                
                // --- Botón SET ---
                _profileSetButton = new SimpleButton(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "SET",
                    "PROFILE_SET",
                    new Vector2(startX + 130f, yPos + 35f + yOffset),
                    new Vector2(40f, 30f)
                );
                _fancyMenu.pages[0].subObjects.Add(_profileSetButton);
                _profileSetButton.inactive = true;
                
                // ============================================================
                // 7. Steam ID (campo escribible)
                // ============================================================
                yOffset = 35f;
                
                _steamLabel = new MenuLabel(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "Steam:",
                    new Vector2(startX, yPos + 35f + yOffset),
                    new Vector2(60f, 20f),
                    false
                );
                _fancyMenu.pages[0].subObjects.Add(_steamLabel);
                
                var steamConfig = new Configurable<string>(
                    dummyOI,
                    "meadow_steamid",
                    "",
                    null
                );
                _steamIdField = new OpTextBox(
                    steamConfig,
                    new Vector2(startX + 65f, yPos + 35f + yOffset),
                    150f
                );
                _steamIdField.allowSpace = false;
                _steamIdField.maxLength = 20;
                
                // Usar UIelementWrapper en lugar de Container.AddChild
                new UIelementWrapper(_tabWrapper, _steamIdField);
                Plugin.Logger.LogInfo($"Steam ID field added at ({startX + 65f}, {yPos + 35f + yOffset})");
                
                // ============================================================
                // 8. Status Label
                // ============================================================
                yOffset = 70f;
                
                _statusLabel = new MenuLabel(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "",
                    new Vector2(startX, yPos + 35f + yOffset),
                    new Vector2(250f, 20f),
                    false
                );
                _fancyMenu.pages[0].subObjects.Add(_statusLabel);
                
                _uiAdded = true;
                Plugin.Logger.LogInfo("Meadow profile UI initialized SUCCESSFULLY!");
                
                // Eventos
                _steamIdField.OnValueChanged += OnSteamIdChanged;
                _profileNumberField.OnValueChanged += OnProfileNumberChanged;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error initializing meadow UI: {ex.Message}");
                Plugin.Logger.LogError(ex.StackTrace);
            }
        }
        
        // ============================================================
        // Eventos
        // ============================================================
        private void OnSteamIdChanged(UIconfig sender, string oldValue, string newValue)
        {
            try
            {
                Plugin.Logger.LogInfo($"OnSteamIdChanged called: old='{oldValue}', new='{newValue}'");
                
                if (!MeadowProfileManager.IsMeadowModeActive) return;
                
                // Si el valor es "unassigned", lo tratamos como vacío
                string cleanValue = (newValue == "unassigned") ? "" : newValue;
                int profileNumber = MeadowProfileManager.CurrentProfileNumber;
                MeadowProfileManager.SetSteamID(profileNumber, cleanValue);
                _statusLabel.text = $"Steam ID saved";
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error saving Steam ID: {ex.Message}");
            }
        }
        
        private void OnProfileNumberChanged(UIconfig sender, string oldValue, string newValue)
        {
            try
            {
                Plugin.Logger.LogInfo($"OnProfileNumberChanged called: old='{oldValue}', new='{newValue}'");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in OnProfileNumberChanged: {ex.Message}");
            }
            
            // Solo validamos, no hacemos nada automático
            if (string.IsNullOrEmpty(newValue)) return;
            
            if (!int.TryParse(newValue, out int n) || n < 1 || n > 99)
            {
                // Si es inválido, revertir al valor anterior
                _profileNumberField.value = oldValue;
            }
        }
        
        // ============================================================
        // SET Profile
        // ============================================================
        private void SetProfileNumber()
        {
            Plugin.Logger.LogInfo($"SetProfileNumber called - MeadowMode: {MeadowProfileManager.IsMeadowModeActive}, value: '{_profileNumberField?.value}'");
            
            if (!MeadowProfileManager.IsMeadowModeActive)
            {
                _statusLabel.text = "Meadow mode is OFF";
                return;
            }
            
            string input = _profileNumberField.value;
            
            if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int profileNumber))
            {
                _statusLabel.text = "Invalid profile number";
                return;
            }
            
            if (profileNumber < 1 || profileNumber > 99)
            {
                _statusLabel.text = "Profile must be 1-99";
                return;
            }
            
            // Guardar el perfil actual antes de cambiar
            SaveCurrentProfile();
            
            // Cargar el nuevo perfil
            MeadowProfileManager.SetCurrentProfile(profileNumber);
            _profileNumberField.value = profileNumber.ToString();
            
            // Cargar la skin
            LoadProfile(profileNumber);
            
            // Actualizar Steam ID
            string steamId = MeadowProfileManager.GetSteamID(profileNumber);
            _steamIdField.value = string.IsNullOrEmpty(steamId) ? "unassigned" : steamId;
            
            _statusLabel.text = $"Loaded profile {profileNumber}";
            Plugin.Logger.LogInfo($"Switched to meadow profile {profileNumber}");
        }
        
        // ============================================================
        // Guardar/Cargar perfiles
        // ============================================================
        private void SaveCurrentProfile()
        {
            try
            {
                if (!MeadowProfileManager.IsMeadowModeActive) return;
                
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
                        
                        // Actualizar dummy
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
        
        // ============================================================
        // Toggle Meadow Mode
        // ============================================================
        public void ToggleMeadowMode()
        {
            Plugin.Logger.LogInfo($"ToggleMeadowMode called - Current: {MeadowProfileManager.IsMeadowModeActive}");
            
            MeadowProfileManager.IsMeadowModeActive = !MeadowProfileManager.IsMeadowModeActive;
            
            bool active = MeadowProfileManager.IsMeadowModeActive;
            
            if (active)
            {
                _profileSetButton.inactive = false;
                _statusLabel.text = $"Meadow ON - Profile {MeadowProfileManager.CurrentProfileNumber}";
                
                int profileNumber = MeadowProfileManager.CurrentProfileNumber;
                _profileNumberField.value = profileNumber.ToString();
                
                string steamId = MeadowProfileManager.GetSteamID(profileNumber);
                _steamIdField.value = string.IsNullOrEmpty(steamId) ? "unassigned" : steamId;
                
                LoadProfile(profileNumber);
                
                Plugin.Logger.LogInfo("Meadow mode activated");
            }
            else
            {
                _profileSetButton.inactive = true;
                _statusLabel.text = "";
                
                SaveCurrentProfile();
                
                // Restaurar skin de DMS
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
        
        // ============================================================
        // HandleSignal para FancyMenu.Singal
        // ============================================================
        public void HandleSignal(string message)
        {
            Plugin.Logger.LogInfo($"HandleSignal: {message}");
            
            if (message == "MEADOW_TOGGLE")
            {
                ToggleMeadowMode();
            }
            else if (message == "PROFILE_SET")
            {
                SetProfileNumber();
            }
        }
        
        // ============================================================
        // AutoSave
        // ============================================================
        public void AutoSave()
        {
            if (MeadowProfileManager.IsMeadowModeActive)
            {
                SaveCurrentProfile();
            }
        }
    }
}