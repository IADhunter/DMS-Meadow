using System;
using System.Collections.Generic;

namespace DMSxMeadow
{
    [Serializable]
    public class MeadowSaveData
    {
        public int MaxProfiles = 16;
        public Dictionary<string, int> SteamAssignments = new Dictionary<string, int>();
        public DateTime LastUpdated = DateTime.Now;
        public string Version = "2.0.0";
    }
}