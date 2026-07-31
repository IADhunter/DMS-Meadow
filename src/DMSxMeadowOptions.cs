using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace DMSxMeadow
{
    public class DMSxMeadowOptions : OptionInterface
    {
        public static DMSxMeadowOptions Instance = new DMSxMeadowOptions();

        private OpScrollBox _scrollBox;
        private OpTextBox _searchBox;
        private OpSimpleButton _modeButton;
        private bool _searchBySteamId = true;
        private List<ProfileRow> _currentRows = new List<ProfileRow>();

        private Configurable<string> _searchConfig;

        // ============================================================
        // CACHE PARA EL MÉTODO Unload (Reflection)
        // ============================================================
        private static MethodInfo _unloadMethod;

        private class ProfileRow
        {
            public int ProfileNumber;
            public string SteamId;
            public OpLabel Label;
            public OpSimpleButton DeleteButton;
        }

        public override void Initialize()
        {
            base.Initialize();
            var opTab = new OpTab(this, "Meadow Profiles");
            Tabs = new[] { opTab };

            // ============================================================
            // TÍTULO DE LA SECCIÓN
            // ============================================================
            float row1Y = 515f;
            float searchRowOffsetX = -21f;
            float titleY = row1Y + 55f;

            var titleLabel = new OpLabel(
                new Vector2(20f + searchRowOffsetX + 124f, titleY),
                new Vector2(350f, 20f),
                "PROFILE MANAGER",
                FLabelAlignment.Center,
                false
            );
            opTab.AddItems(titleLabel);

            // ============================================================
            // FILA SUPERIOR: CAMPO DE BÚSQUEDA + MODO
            // ============================================================

            if (_searchConfig == null)
            {
                _searchConfig = this.config.Bind<string>("searchQuery", "", new ConfigurableInfo("Search query"));
            }

            _searchBox = new OpTextBox(_searchConfig, new Vector2(20f + searchRowOffsetX, row1Y), 200f);
            _searchBox.OnValueChanged += (sender, oldV, newV) => RefreshList();
            opTab.AddItems(_searchBox);

            _modeButton = new OpSimpleButton(
                new Vector2(230f + searchRowOffsetX, row1Y),
                new Vector2(140f, 24f),
                _searchBySteamId ? "Buscar: Player ID" : "Buscar: Perfil #"
            );
            _modeButton.OnClick += (_) =>
            {
                _searchBySteamId = !_searchBySteamId;
                _modeButton.text = _searchBySteamId ? "Buscar: Player ID" : "Buscar: Perfil #";
                RefreshList();
            };
            opTab.AddItems(_modeButton);

            // ============================================================
            // SCROLL BOX DE PERFILES
            // ============================================================
            float scrollY = 30f;
            float scrollHeight = 460f;
            
            _scrollBox = new OpScrollBox(
                new Vector2(0f, scrollY),
                new Vector2(600f, scrollHeight),
                100f,
                false,
                true,
                true
            )
            {
                colorEdge = MenuColorEffect.rgbMediumGrey,
                colorFill = MenuColorEffect.rgbBlack,
                fillAlpha = 0.3f
            };
            opTab.AddItems(_scrollBox);

            if (_unloadMethod == null)
            {
                _unloadMethod = typeof(UIelement).GetMethod("Unload",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            // ============================================================
            // LIMPIEZA AUTOMÁTICA DE HUÉRFANOS
            // ============================================================
            MeadowProfileManager.DeleteOrphanProfiles();
            RefreshList();
        }

        // ============================================================
        // HELPER: UNLOAD ELEMENT (elimina gráficos de pantalla)
        // ============================================================
        private void UnloadElement(UIelement element)
        {
            if (element == null) return;
            try
            {
                _unloadMethod?.Invoke(element, null);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error unloading element: {ex.Message}");
            }
        }

        private void RefreshList()
        {
            try
            {
                // ============================================================
                // 1. ELIMINAR FILAS ACTUALES
                // ============================================================
                foreach (var row in _currentRows)
                {
                    OpScrollBox.RemoveItemsFromScrollBox(row.Label, row.DeleteButton);
                    UnloadElement(row.Label);
                    UnloadElement(row.DeleteButton);
                }
                _currentRows.Clear();

                // ============================================================
                // 2. OBTENER TODOS LOS PERFILES
                // ============================================================
                var allProfiles = MeadowProfileManager.GetAllProfileNumbers();

                string query = _searchBox.value?.Trim() ?? "";
                bool hasQuery = !string.IsNullOrEmpty(query);

                const float ROW_HEIGHT = 26f;
                const float VISIBLE_HEIGHT = 460f;

                // ============================================================
                // 3. FILTRAR Y CONTAR
                // ============================================================
                var matchingProfiles = new List<(int num, string steamId, bool orphan)>();
                int assigned = 0, orphans = 0;

                foreach (int p in allProfiles)
                {
                    string sid = MeadowProfileManager.GetSteamID(p);
                    bool isOrphan = string.IsNullOrEmpty(sid);
                    if (isOrphan) orphans++; else assigned++;

                    bool matches = !hasQuery || (_searchBySteamId
                        ? sid.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                        : p.ToString().Contains(query));

                    if (matches) matchingProfiles.Add((p, sid, isOrphan));
                }

                // ============================================================
                // 4. ORDENAR DE MENOR A MAYOR
                // ============================================================
                matchingProfiles = matchingProfiles.OrderBy(x => x.num).ToList();

                // ============================================================
                // 5. CALCULAR ALTURA TOTAL DEL CONTENIDO
                // ============================================================
                float contentHeight = Math.Max(matchingProfiles.Count * ROW_HEIGHT + 20f, VISIBLE_HEIGHT);

                // ============================================================
                // 6. COLOCAR FILAS
                // ============================================================
                float y = contentHeight - ROW_HEIGHT - 10f;

                foreach (var (profileNum, steamId, isOrphan) in matchingProfiles)
                {
                    string display = isOrphan
                        ? $"Perfil {profileNum}  [huérfano]"
                        : $"Perfil {profileNum}  {steamId}";

                    var label = new OpLabel(10f, y, display, false);
                    if (isOrphan)
                    {
                        label.color = Color.gray;
                    }

                    var deleteBtn = new OpSimpleButton(
                        new Vector2(350f, y - 3f),
                        new Vector2(70f, 22f),
                        "Eliminar"
                    );

                    int capturedNum = profileNum;
                    deleteBtn.OnClick += (_) => DeleteProfile(capturedNum);

                    _scrollBox.AddItems(label, deleteBtn);
                    _currentRows.Add(new ProfileRow
                    {
                        ProfileNumber = profileNum,
                        SteamId = steamId,
                        Label = label,
                        DeleteButton = deleteBtn
                    });

                    y -= ROW_HEIGHT;
                }

                // ============================================================
                // 7. ACTUALIZAR SCROLL
                // ============================================================
                _scrollBox.SetContentSize(contentHeight, true);
                _scrollBox.MarkDirty();

                // ============================================================
                // 8. ACTUALIZAR ESTADÍSTICAS
                // ============================================================
                Tabs[0].name = $"Meadow Profiles  ({allProfiles.Count} total, {assigned} asignados, {orphans} huérfanos)";
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error refreshing profile list: {ex.Message}");
                Plugin.Logger.LogError(ex.StackTrace);
            }
        }

        private void DeleteProfile(int profileNumber)
        {
            try
            {
                MeadowProfileManager.DeleteProfile(profileNumber);
                MeadowProfileManager.RemoveAssignment(profileNumber);
                RefreshList();
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error deleting profile {profileNumber}: {ex.Message}");
            }
        }
    }
}
