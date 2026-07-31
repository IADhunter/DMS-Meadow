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
        private SelectOneButton _meadowModeButton;
        private OpTextBox _steamIdField;
        private OpTextBox _profileNumberField;
        private SimpleButton _profileSetButton;
        private MenuLabel _statusLabel;
        private MenuLabel _steamLabel;
        private MenuLabel _profileLabel;

        private MenuTabWrapper _tabWrapper;
        private bool _uiAdded = false;

        private bool _profileFieldWasHeld = false;
        private bool _steamFieldWasHeld = false;
        private string _lastConfirmedSteamId = "";
        private string _lastConfirmedProfileNumber = "";
        private string _pendingProfileInput = "1";
        private string _lastKnownSlugcat = "";

        // Cache para reflexión (se inicializa una sola vez)
        private static System.Reflection.FieldInfo _uiConfigValueField;

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

                float leftAnchor = (1366f - _fancyMenu.manager.rainWorld.options.ScreenSize.x) / 2f;

                float offsetX = -50f;
                float offsetY = 15f;

                float baseStartX = textBoxBorder.pos.x + (65f * playerCount) + 10f - leftAnchor;
                float baseYPos = textBoxBorder.pos.y - 40f;

                float startX = baseStartX + offsetX;
                float yPos = baseYPos + offsetY;

                if (startX < 0 || startX > 1366f)
                {
                    Plugin.Logger.LogWarning($"startX={startX} fuera de rango, usando posición fija");
                    startX = 100f;
                    yPos = 100f;
                }

                Plugin.Logger.LogInfo($"Button position: X={startX}, Y={yPos} (offset X:{offsetX}, Y:{offsetY})");

                _tabWrapper = new MenuTabWrapper(_fancyMenu, _fancyMenu.pages[0]);
                _fancyMenu.pages[0].subObjects.Add(_tabWrapper);
                Plugin.Logger.LogInfo("MenuTabWrapper created and added to page");

                var meadowArray = new SelectOneButton[1];
                _meadowModeButton = new SelectOneButton(
                    _fancyMenu,
                    _fancyMenu.pages[0],
                    "MEADOW",
                    "MEADOW_SERIES",
                    new Vector2(baseStartX, baseYPos),
                    new Vector2(80f, 30f),
                    meadowArray,
                    0
                );
                meadowArray[0] = _meadowModeButton;
                _fancyMenu.pages[0].subObjects.Add(_meadowModeButton);
                Plugin.Logger.LogInfo($"MEADOW SelectOneButton added at ({baseStartX}, {baseYPos})");

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
                _profileNumberField.greyedOut = true;
                new UIelementWrapper(_tabWrapper, _profileNumberField);
                Plugin.Logger.LogInfo($"Profile number field added at ({startX + 65f}, {yPos + 35f + yOffset})");

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
                _steamIdField.allowSpace = true;
                _steamIdField.maxLength = 30;
                _steamIdField.greyedOut = true;
                _steamIdField.value = "unassigned";
                new UIelementWrapper(_tabWrapper, _steamIdField);
                Plugin.Logger.LogInfo($"Steam ID field added at ({startX + 65f}, {yPos + 35f + yOffset})");

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

                _steamIdField.OnValueChanged += OnSteamIdChangedDebug;
                _profileNumberField.OnValueChanged += OnProfileNumberChanged;

                _lastKnownSlugcat = _fancyMenu.selectedSlugcat;

                // Cachear el campo _value de UIconfig para usarlo en CheckPasteInput
                if (_uiConfigValueField == null)
                {
                    _uiConfigValueField = typeof(UIconfig).GetField("_value",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);
                    if (_uiConfigValueField == null)
                    {
                        Plugin.Logger.LogWarning("Could not find UIconfig._value field - paste will use fallback");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error initializing meadow UI: {ex.Message}");
                Plugin.Logger.LogError(ex.StackTrace);
            }
        }

        private void OnSteamIdChangedDebug(UIconfig sender, string oldValue, string newValue)
        {
            Plugin.Logger.LogInfo($"Steam ID field changed (debug): '{oldValue}' -> '{newValue}'");
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

            if (string.IsNullOrEmpty(newValue)) return;

            if (!int.TryParse(newValue, out int n) || n < 1 || n > 99)
            {
                _profileNumberField.value = oldValue;
                return;
            }

            _pendingProfileInput = newValue;
            Plugin.Logger.LogInfo($"Pending profile input updated to: '{_pendingProfileInput}'");
        }

        public void CheckSlugcatChange()
        {
            if (!MeadowProfileManager.IsMeadowModeActive) return;
            string currentSlugcat = _fancyMenu.selectedSlugcat;
            if (currentSlugcat != _lastKnownSlugcat)
            {
                Plugin.Logger.LogInfo($"Slugcat changed to '{currentSlugcat}' with Meadow active - reloading profile for this slugcat");
                _lastKnownSlugcat = currentSlugcat;
                LoadProfile(MeadowProfileManager.CurrentProfileNumber);
            }
        }

        private void SetProfileNumber()
        {
            Plugin.Logger.LogInfo($"SetProfileNumber called - MeadowMode: {MeadowProfileManager.IsMeadowModeActive}, pending: '{_pendingProfileInput}'");

            if (!MeadowProfileManager.IsMeadowModeActive)
            {
                _statusLabel.text = "Meadow mode is OFF";
                return;
            }

            string input = _pendingProfileInput;

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

            if (profileNumber == MeadowProfileManager.CurrentProfileNumber)
            {
                _statusLabel.text = $"Already on profile {profileNumber}";
                Plugin.Logger.LogInfo($"SetProfileNumber: already on profile {profileNumber}, skipping reload");
                return;
            }

            SaveCurrentProfile();

            MeadowProfileManager.SetCurrentProfile(profileNumber);
            _profileNumberField.value = profileNumber.ToString();
            _pendingProfileInput = profileNumber.ToString();

            LoadProfile(profileNumber);

            string steamId = MeadowProfileManager.GetSteamID(profileNumber);
            _steamIdField.value = string.IsNullOrEmpty(steamId) ? "unassigned" : steamId;

            _lastConfirmedSteamId = steamId;
            _lastConfirmedProfileNumber = profileNumber.ToString();

            _statusLabel.text = $"Loaded profile {profileNumber}";
            Plugin.Logger.LogInfo($"Switched to meadow profile {profileNumber}");
        }

        public void SaveCurrentProfile()
        {
            try
            {
                if (!MeadowProfileManager.IsMeadowModeActive) return;

                var customization = GetLiveCustomization();
                if (customization != null)
                {
                    string slugcatName = _fancyMenu.selectedSlugcat;
                    MeadowProfileManager.SaveCurrentProfile(slugcatName, customization);
                    Plugin.Logger.LogInfo($"Saved current meadow profile {MeadowProfileManager.CurrentProfileNumber} for slugcat '{slugcatName}'");
                }
                else
                {
                    Plugin.Logger.LogWarning("SaveCurrentProfile: live customization is null!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error saving profile: {ex.Message}");
            }
        }

        private DressMySlugcat.Customization GetLiveCustomization()
        {
            try
            {
                return DressMySlugcat.Customization.For(
                    _fancyMenu.selectedSlugcat,
                    _fancyMenu.selectedPlayerIndex,
                    false
                );
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error getting live customization: {ex.Message}");
                return null;
            }
        }

        private void LoadProfile(int displayNumber)
        {
            try
            {
                string slugcatName = _fancyMenu.selectedSlugcat;
                var customization = MeadowProfileManager.GetProfileCustomization(displayNumber, slugcatName);
                var live = GetLiveCustomization();
                if (live == null)
                {
                    Plugin.Logger.LogWarning("LoadProfile: live object is null!");
                    return;
                }

                if (customization != null)
                {
                    Plugin.Logger.LogInfo($"Loading meadow profile {displayNumber} for slugcat '{slugcatName}' into live object");

                    live.CustomTail.Length = customization.CustomTail.Length;
                    live.CustomTail.Wideness = customization.CustomTail.Wideness;
                    live.CustomTail.Roundness = customization.CustomTail.Roundness;
                    live.CustomTail.Lift = customization.CustomTail.Lift;
                    live.CustomTail.Color = customization.CustomTail.Color;
                    live.CustomTail.CustTailShape = customization.CustomTail.CustTailShape;
                    live.CustomTail.AsymTail = customization.CustomTail.AsymTail;

                    live.CustomSprites.Clear();
                    foreach (var sprite in customization.CustomSprites)
                    {
                        live.CustomSprites.Add(new DressMySlugcat.CustomSprite
                        {
                            Sprite = sprite.Sprite,
                            SpriteSheetID = sprite.SpriteSheetID,
                            ColorHex = sprite.ColorHex,
                            Enforce = sprite.Enforce
                        });
                    }

                    Plugin.Logger.LogInfo($"Loaded meadow profile {displayNumber} for '{slugcatName}': Tail.Length={live.CustomTail.Length}, Sprites={live.CustomSprites.Count}");
                }
                else
                {
                    Plugin.Logger.LogInfo($"Profile {displayNumber} has no data for slugcat '{slugcatName}' - using defaults");
                    live.CustomSprites.Clear();
                    live.CustomTail = new DressMySlugcat.CustomTail();
                }

                _lastKnownSlugcat = slugcatName;
                RefreshDummyAndControls();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error loading profile: {ex.Message}");
            }
        }

        private void RefreshDummyAndControls()
        {
            try
            {
                var dummyField = _fancyMenu.GetType()
                    .GetField("slugcatDummy",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance);

                var dummy = dummyField?.GetValue(_fancyMenu);
                if (dummy != null)
                {
                    var updateMethod = dummy.GetType().GetMethod("UpdateSprites",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (updateMethod != null)
                    {
                        Plugin.Logger.LogInfo("Calling UpdateSprites via reflection (NonPublic)");
                        updateMethod.Invoke(dummy, null);
                    }
                    else
                    {
                        Plugin.Logger.LogWarning("UpdateSprites method not found!");
                    }
                }
                else
                {
                    Plugin.Logger.LogWarning("RefreshDummyAndControls: dummy is null!");
                }

                var updateControlsMethod = _fancyMenu.GetType()
                    .GetMethod("UpdateControls",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance);
                updateControlsMethod?.Invoke(_fancyMenu, null);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error refreshing dummy: {ex.Message}");
            }
        }

        public void CheckFieldFocusLoss()
        {
            try
            {
                bool profileHeld = GetHeld(_profileNumberField);
                if (_profileFieldWasHeld && !profileHeld)
                {
                    string currentProfile = MeadowProfileManager.CurrentProfileNumber.ToString();
                    if (_profileNumberField.value != currentProfile)
                    {
                        Plugin.Logger.LogInfo($"Profile field lost focus, restoring display to {currentProfile}");
                        _profileNumberField.value = currentProfile;
                        _statusLabel.text = "Profile change cancelled";
                    }
                }
                _profileFieldWasHeld = profileHeld;

                bool steamHeld = GetHeld(_steamIdField);

                if (!_steamFieldWasHeld && steamHeld)
                {
                    if (_steamIdField.value == "unassigned")
                    {
                        _steamIdField.value = "";
                        Plugin.Logger.LogInfo("Steam field gained focus - cleared 'unassigned' placeholder");
                    }
                }

                if (_steamFieldWasHeld && !steamHeld)
                {
                    string currentSteamId = MeadowProfileManager.GetSteamID(MeadowProfileManager.CurrentProfileNumber);
                    string fieldValue = _steamIdField.value;
                    string cleanValue = (fieldValue == "unassigned") ? "" : fieldValue;

                    if (cleanValue != currentSteamId)
                    {
                        Plugin.Logger.LogInfo($"Steam field lost focus, saving new value: '{cleanValue}'");
                        MeadowProfileManager.SetSteamID(MeadowProfileManager.CurrentProfileNumber, cleanValue);
                        _lastConfirmedSteamId = cleanValue;
                        _statusLabel.text = "Steam ID saved";

                        if (!string.IsNullOrEmpty(cleanValue))
                        {
                            SaveCurrentProfile();
                        }
                    }

                    string displayValue = string.IsNullOrEmpty(cleanValue) ? "unassigned" : cleanValue;
                    _steamIdField.value = displayValue;
                }
                _steamFieldWasHeld = steamHeld;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error in CheckFieldFocusLoss: {ex.Message}");
            }
        }

        private bool GetHeld(OpTextBox field)
        {
            if (field == null) return false;

            try
            {
                var heldProp = typeof(UIfocusable).GetProperty("held",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (heldProp != null)
                {
                    return (bool)(heldProp.GetValue(field) ?? false);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error getting Held property: {ex.Message}");
            }

            return false;
        }

        public void ActivateMeadowMode()
        {
            if (MeadowProfileManager.IsMeadowModeActive)
            {
                Plugin.Logger.LogWarning("ActivateMeadowMode called but already active!");
                return;
            }

            Plugin.Logger.LogInfo("Activating Meadow mode");

            MeadowProfileManager.IsMeadowModeActive = true;

            _profileNumberField.greyedOut = false;
            _steamIdField.greyedOut = false;
            _profileSetButton.inactive = false;

            int profileNumber = MeadowProfileManager.CurrentProfileNumber;
            _profileNumberField.value = profileNumber.ToString();
            _pendingProfileInput = profileNumber.ToString();

            string steamId = MeadowProfileManager.GetSteamID(profileNumber);
            _steamIdField.value = string.IsNullOrEmpty(steamId) ? "unassigned" : steamId;

            _lastConfirmedSteamId = steamId;
            _lastConfirmedProfileNumber = profileNumber.ToString();

            _lastKnownSlugcat = _fancyMenu.selectedSlugcat;
            LoadProfile(profileNumber);
            _statusLabel.text = $"Meadow ON - Profile {profileNumber}";

            RefreshDummyAndControls();

            Plugin.Logger.LogInfo($"Meadow mode activated");
        }

        public void DeactivateMeadowMode()
        {
            if (!MeadowProfileManager.IsMeadowModeActive)
            {
                Plugin.Logger.LogWarning("DeactivateMeadowMode called but not active!");
                return;
            }

            Plugin.Logger.LogInfo("Deactivating Meadow mode");

            SaveCurrentProfile();

            _profileNumberField.greyedOut = true;
            _steamIdField.greyedOut = true;
            _profileSetButton.inactive = true;

            _lastConfirmedSteamId = "";
            _lastConfirmedProfileNumber = "";
            _profileFieldWasHeld = false;
            _steamFieldWasHeld = false;
            _lastKnownSlugcat = "";

            MeadowProfileManager.IsMeadowModeActive = false;
            _statusLabel.text = "";

            RefreshDummyAndControls();

            Plugin.Logger.LogInfo("Meadow mode deactivated (no native profiles were touched)");
        }

        public void ToggleMeadowMode()
        {
            if (!MeadowProfileManager.IsMeadowModeActive)
            {
                ActivateMeadowMode();
                _fancyMenu.PlaySound(SoundID.MENU_Switch_Page_Out);
            }
        }

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

        public void ForceDeactivateMeadowMode()
        {
            if (!MeadowProfileManager.IsMeadowModeActive) return;

            Plugin.Logger.LogWarning("ForceDeactivateMeadowMode called - ensuring cleanup!");

            SaveCurrentProfile();

            _profileNumberField.greyedOut = true;
            _steamIdField.greyedOut = true;
            _profileSetButton.inactive = true;

            _lastConfirmedSteamId = "";
            _lastConfirmedProfileNumber = "";
            _profileFieldWasHeld = false;
            _steamFieldWasHeld = false;
            _lastKnownSlugcat = "";

            MeadowProfileManager.IsMeadowModeActive = false;
            _statusLabel.text = "";

            RefreshDummyAndControls();

            Plugin.Logger.LogInfo("ForceDeactivateMeadowMode completed");
        }

        // ============================================================
        // Manejo de Ctrl+V para pegar desde portapapeles
        // ============================================================
        public void CheckPasteInput()
        {
            if (!MeadowProfileManager.IsMeadowModeActive) return;
            if (_steamIdField == null) return;

            bool steamHeld = GetHeld(_steamIdField);
            if (!steamHeld) return;

            if (Input.GetKeyDown(KeyCode.V) &&
                (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                string clipboard = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clipboard))
                {
                    string clean = clipboard.Trim();

                    // ============================================================
                    // Saltar el setter de OpTextBox que filtra caracteres.
                    // Escribimos directamente en el campo _value de UIconfig.
                    // ============================================================
                    if (_uiConfigValueField != null)
                    {
                        _uiConfigValueField.SetValue(_steamIdField, clean);
                        _steamIdField.Change(); // Actualiza el label visualmente
                        Plugin.Logger.LogInfo($"Pasted into Steam ID field (bypass filter): '{clean}'");
                        _statusLabel.text = "Steam ID pasted - press ENTER or click away to save";
                    }
                    else
                    {
                        // Fallback: usar el setter normal (puede filtrar)
                        _steamIdField.value = clean;
                        Plugin.Logger.LogInfo($"Pasted into Steam ID field (fallback): '{clean}'");
                        _statusLabel.text = "Steam ID pasted (fallback)";
                    }
                }
            }
        }
    }
}