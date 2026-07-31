using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.IO;

namespace DressMySlugcatMeadowCompat
{
    [BepInPlugin("dmsmeadowcompat", "Dress My Slugcat Meadow Compat", "1.0.0")]
    [BepInDependency("dressmyslugcat", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("henpemaz.rainmeadow", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private Hook dmsHook;
        public static Dictionary<string, string> DiccionarioSkins = new Dictionary<string, string>();

        public void OnEnable()
        {
            try
            {
                MethodInfo[] methods = typeof(DressMySlugcat.Customization).GetMethods(BindingFlags.Public | BindingFlags.Static);
                MethodInfo metodoOriginal = methods.FirstOrDefault(m => m.Name == "For" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(Player));
                MethodInfo miMetodoHook = typeof(Plugin).GetMethod("Customization_For_Hook", BindingFlags.NonPublic | BindingFlags.Static);

                if (metodoOriginal != null && miMetodoHook != null)
                {
                    dmsHook = new Hook(metodoOriginal, miMetodoHook);
                }
            }
            catch (Exception)
            {
                return;
            }

            CargarConfiguracionSkins();
        }

        public void OnDisable()
        {
            try
            {
                if (dmsHook != null)
                {
                    dmsHook.Dispose();
                    dmsHook = null;
                }
            }
            catch (Exception)
            {
            
            }
        }

        private static DressMySlugcat.Customization Customization_For_Hook(Func<Player, bool, DressMySlugcat.Customization> orig, Player player, bool mergeDefaults)
        {
            if (player != null && player.abstractCreature != null)
            {
                if (RainMeadow.OnlinePhysicalObject.map.TryGetValue(player.abstractCreature, out var OnlineEntity))
                {
                    var owner = OnlineEntity.owner;
                    if (owner != null && !owner.isMe && owner.id != null)
                    {
                        string steamId = owner.id.ToString();
                        if (DiccionarioSkins.TryGetValue(steamId, out string assignedProfileStr))
                        {
                            if (int.TryParse(assignedProfileStr, out int assignedProfile))
                            {
                                string slugcatClass = player.SlugCatClass.ToString();
                                if (slugcatClass == "MeadowOnline") slugcatClass = "White";

                                var customization = DressMySlugcat.SaveManager.Customizations.FirstOrDefault(x => x != null && x.Slugcat == slugcatClass && x.PlayerNumber == assignedProfile - 1);

                                if (customization != null)
                                {
                                    var resultCopy = customization.Copy();

                                    var customization2 = DressMySlugcat.SpriteDefinitions.GetSlugcatDefault(slugcatClass, assignedProfile)?.Copy();
                                    if (customization2 != null)
                                    {
                                        if (!resultCopy.CustomTail.IsCustom)
                                        {
                                            resultCopy.CustomTail.Length = customization2.CustomTail.Length;
                                            resultCopy.CustomTail.Wideness = customization2.CustomTail.Wideness;
                                            resultCopy.CustomTail.Roundness = customization2.CustomTail.Roundness;
                                            resultCopy.CustomTail.Lift = customization2.CustomTail.Lift;
                                        }

                                        if (resultCopy.CustomTail.Color == default(UnityEngine.Color))
                                        {
                                            resultCopy.CustomTail.Color = customization2.CustomTail.Color;
                                        }

                                        foreach (DressMySlugcat.CustomSprite sprite in customization2.CustomSprites)
                                        {
                                            if (!resultCopy.CustomSprites.Any(x => x.Sprite == sprite.Sprite))
                                            {
                                                resultCopy.CustomSprites.Add(sprite);
                                            }
                                        }
                                    }

                                    resultCopy.PlayerNumber = 0;
                                    return resultCopy;
                                }
                            }
                        }
                    }
                }
            }
            return orig(player, mergeDefaults);
        }

        private void CargarConfiguracionSkins()
        {
            try
            {
                string path = Path.Combine(Path.GetDirectoryName(Info.Location), "dms_meadow_skins.txt");

                if (!File.Exists(path))
                {
                    List<String> exampleLines = new List<string>
                    {
                        "# Skin configuration for Dress My Slugcat + Rain Meadow",
                        "# Format -> STEAM_NAME:DMS_PROFILE_NUMBER",
                        "# Example:",
                        "SteamUserName:2",
                        "omegaboom123:3"
                    };
                    File.WriteAllLines(path, exampleLines);
                    return;
                }

                string[] lines = File.ReadAllLines(path);
                DiccionarioSkins.Clear();

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#")) 
                        continue;

                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                    {
                        string steamIdStr = parts[0].Trim();
                        string assignedProfileStr = parts[1].Trim();

                        if (!DiccionarioSkins.ContainsKey(steamIdStr))
                        {
                            DiccionarioSkins.Add(steamIdStr, assignedProfileStr);
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
