using System.Collections.Generic;

namespace TDTS.Data
{
    /// <summary>
    /// Per-player record: bunker seating, outcome, and the cards currently held.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public string playerName;
        public bool isSeated;
        public List<CardData> hand = new List<CardData>();

        public PlayerData(string name)
        {
            playerName = name;
            isSeated = false;
        }
    }
}