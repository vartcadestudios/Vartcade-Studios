using System.Collections.Generic;

namespace TDTS.Data
{
    /// <summary>
    /// Per-player record: the cards held, skip markers, and the Sundown outcome.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public string playerName;
        // Online: the Unity Gaming Services player id of the person in this seat. Empty for bots and offline.
        public string accountId = "";
        public List<CardData> hand = new List<CardData>();

        // Spec V10: lose your next Day / Night turn
        public bool daySkip;
        public bool nightSkip;

        // Spec V16: set at Sundown
        public bool isSeated;
        public string resultReason = "";

        public PlayerData(string name)
        {
            playerName = name;
            isSeated = false;
        }

        // Spec V15
        public int RealSupplyCount => hand.FindAll(c => c.IsRealSupply).Count;
        public bool HoldsKey => hand.Exists(c => c.IsKey);
    }
}
