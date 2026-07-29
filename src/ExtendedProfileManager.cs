using System;
using System.Collections.Generic;
using System.Linq;

namespace DMSxMeadow
{
    public static class ExtendedProfileManager
    {
        public static void ExtendProfileSystem()
        {
            try
            {
                int maxProfiles = MeadowSaveManager.Data.MaxProfiles;
                Plugin.Logger.LogInfo($"Extending profile system to {maxProfiles} profiles");
                
                EnsureDMSProfiles(maxProfiles);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"Error extending profiles: {ex.Message}");
            }
        }
        
        private static void EnsureDMSProfiles(int maxProfiles)
        {
            var validSlugcats = GetValidSlugcatNames();
            bool needsSave = false;
            
            foreach (var slugcat in validSlugcats)
            {
                for (int i = 0; i < maxProfiles; i++)
                {
                    bool exists = DressMySlugcat.SaveManager.Customizations
                        .Any(x => x.Slugcat == slugcat && x.PlayerNumber == i);
                    
                    if (!exists)
                    {
                        var defaultCust = new DressMySlugcat.Customization
                        {
                            Slugcat = slugcat,
                            PlayerNumber = i
                        };
                        
                        var defaults = DressMySlugcat.SpriteDefinitions
                            .GetSlugcatDefault(slugcat, 0);
                        
                        if (defaults != null)
                        {
                            defaultCust.CustomTail.Length = defaults.CustomTail.Length;
                            defaultCust.CustomTail.Wideness = defaults.CustomTail.Wideness;
                            defaultCust.CustomTail.Roundness = defaults.CustomTail.Roundness;
                            defaultCust.CustomTail.Lift = defaults.CustomTail.Lift;
                            defaultCust.CustomTail.Color = defaults.CustomTail.Color;
                            
                            defaultCust.CustomSprites = defaults.CustomSprites
                                .Select(x => new DressMySlugcat.CustomSprite
                                {
                                    Sprite = x.Sprite,
                                    SpriteSheetID = x.SpriteSheetID,
                                    ColorHex = x.ColorHex,
                                    Enforce = x.Enforce
                                }).ToList();
                        }
                        
                        DressMySlugcat.SaveManager.Customizations.Add(defaultCust);
                        needsSave = true;
                    }
                }
            }
            
            if (needsSave)
            {
                DressMySlugcat.SaveManager.Save();
                Plugin.Logger.LogInfo("DMS profiles extended and saved");
            }
        }
        
        private static List<string> GetValidSlugcatNames()
        {
            try
            {
                return DressMySlugcat.Utils.ValidSlugcatNames;
            }
            catch
            {
                return new List<string> { "White", "Yellow", "Red", "Slugpup", "Artificer", "Rivulet", "Spear", "Saint", "Inv" };
            }
        }
        
        public static bool IsProfileValid(int profileNumber)
        {
            return profileNumber >= 1 && profileNumber <= MeadowSaveManager.Data.MaxProfiles;
        }
        
        public static int GetNextAvailableProfile()
        {
            var usedProfiles = MeadowSaveManager.Data.SteamAssignments.Values.ToHashSet();
            
            for (int i = 1; i <= MeadowSaveManager.Data.MaxProfiles; i++)
            {
                if (!usedProfiles.Contains(i))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}