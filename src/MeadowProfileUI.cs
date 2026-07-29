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
                var textBoxBorderField = _fancyMenu.GetType()
                    .GetField("textBoxBorder", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                
                var textBoxBorder = textBoxBorderField?.GetValue(_fancyMenu) as RoundedRect;
                if (textBoxBorder == null) return;
                
                float leftAnchor = (1366f - _fancyMenu.manager.rainWorld.options.ScreenSize.x) / 2f;
                
                // Obtener la cantidad de jugadores que tiene DMS
                var playerButtonsField = _fancyMenu.GetType()
                    .GetField("playerButtons", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                
                var playerButtons = playerButtonsField?.GetValue(_fancyMenu) as SelectOneButton[];
                int playerCount = playerButtons?.Length ?? 4;
                
                // Calcular posición: al lado derecho de los botones de perfil
                // Los botones están en: textBoxBorder.pos + new Vector2(startPos + (65 * i), -40)
                // El último botón está en: textBoxBorder.pos.x + (65 * (playerCount - 1))
                float startX = textBoxBorder.pos.x + (65 * playerCount) + 10f;
                float yPos = textBoxBorder.pos.y - 40f;
                
                // ========================================================
                // Botón MEADOW MODE - al lado de los botones de perfil
                // ========================================================
                _meadowModeButton = new SimpleButton(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "MEADOW",
                    "MEADOW_TOGGLE",
                    new Vector2(startX, yPos),
                    new Vector2(80f, 30f)  // Más pequeño para que quepa
                );
                _fancyMenu.pages[0].subObjects.Add(_meadowModeButton);
                
                // ========================================================
                // Etiqueta "Profile:" - debajo del botón MEADOW
                // ========================================================
                _profileLabel = new MenuLabel(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "Profile:",
                    new Vector2(startX, yPos - 30f),
                    new Vector2(60f, 20f),
                    false
                );
                _fancyMenu.pages[0].subObjects.Add(_profileLabel);
                
                // ========================================================
                // Botón para mostrar y cambiar el número de perfil
                // ========================================================
                _profileNumberButton = new SimpleButton(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "1",
                    "PROFILE_CHANGE",
                    new Vector2(startX + 65f, yPos),
                    new Vector2(60f, 30f)
                );
                _fancyMenu.pages[0].subObjects.Add(_profileNumberButton);
                _profileNumberButton.inactive = true;
                
                // ========================================================
                // Etiqueta "Steam:" - debajo del perfil
                // ========================================================
                _steamLabel = new MenuLabel(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "Steam:",
                    new Vector2(startX, yPos - 60f),
                    new Vector2(60f, 20f),
                    false
                );
                _fancyMenu.pages[0].subObjects.Add(_steamLabel);
                
                // ========================================================
                // Campo SteamID
                // ========================================================
                var dummyOI = new DressMySlugcat.DMSOptions();
                var steamConfig = new Configurable<string>(
                    dummyOI,
                    "meadow_steamid",
                    "",
                    null
                );
                _steamIdField = new OpTextBox(
                    steamConfig,
                    new Vector2(startX + 65f, yPos - 60f),
                    150f
                );
                _steamIdField.allowSpace = false;
                _steamIdField.maxLength = 20;
                // OpTextBox se añade al Container, no a subObjects
                _fancyMenu.pages[0].Container.AddChild(_steamIdField.myContainer);
                
                // ========================================================
                // Etiqueta de estado - debajo del SteamID
                // ========================================================
                _statusLabel = new MenuLabel(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "",
                    new Vector2(startX, yPos - 95f),
                    new Vector2(250f, 20f),
                    false
                );
                _fancyMenu.pages[0].subObjects.Add(_statusLabel);
                
                _uiAdded = true;
                Plugin.Logger.LogInfo("Meadow profile UI initialized");
                
                // Eventos
                _steamIdField.OnValueChanged += OnSteamIdChanged;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error initializing meadow UI: {ex.Message}");
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
                                System.Reflection.BindingFlags.NonPublic | 
                                System.Reflection.BindingFlags.Instance);
                        
                        var dummy = dummyField?.GetValue(_fancyMenu);
                        if (dummy != null)
                        {
                            var updateMethod = dummy.GetType().GetMethod("UpdateSprites", 
                                System.Reflection.BindingFlags.NonPublic | 
                                System.Reflection.BindingFlags.Instance);
                            updateMethod?.Invoke(dummy, null);
                        }
                        
                        var updateControlsMethod = _fancyMenu.GetType()
                            .GetMethod("UpdateControls", 
                                System.Reflection.BindingFlags.NonPublic | 
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
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                
                var dummy = dummyField?.GetValue(_fancyMenu);
                if (dummy != null)
                {
                    var updateMethod = dummy.GetType().GetMethod("UpdateSprites", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    updateMethod?.Invoke(dummy, null);
                }
                
                var updateControlsMethod = _fancyMenu.GetType()
                    .GetMethod("UpdateControls", 
                        System.Reflection.BindingFlags.NonPublic | 
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