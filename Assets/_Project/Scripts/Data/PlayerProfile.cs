using System;
using System.Collections.Generic;
using UnityEngine;

namespace TDTS.Data
{
    /// <summary>
    /// Your record on this device: games, wins, streaks and recent results. Saved in PlayerPrefs as
    /// JSON, so it survives restarts but stays on this computer. Each device records only its own
    /// player, at Sundown, so it works the same offline and online.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public const int HistoryLength = 10;
        private const string Key = "profile.v1";

        public int gamesPlayed;
        public int wins;            // made it into the bunker
        public int keyWins;         // got in by holding the Bunker Key
        public int onlineGames;
        public int currentStreak;   // wins in a row, right now
        public int longestStreak;
        public int bestSupply;      // most real Supply you went into Sundown with
        public string firstPlayed = "";
        public string lastPlayed = "";
        public List<bool> recent = new List<bool>(); // newest last; true = made it inside

        public int Losses => gamesPlayed - wins;
        public float WinRate => gamesPlayed == 0 ? 0f : (float)wins / gamesPlayed;

        private static PlayerProfile cached;

        /// <summary>This device's profile, loaded once and kept in memory.</summary>
        public static PlayerProfile Current
        {
            get
            {
                if (cached != null) return cached;
                string json = PlayerPrefs.GetString(Key, "");
                try
                {
                    cached = string.IsNullOrEmpty(json) ? new PlayerProfile() : JsonUtility.FromJson<PlayerProfile>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Profile] Couldn't read the saved profile, starting fresh: {e.Message}");
                    cached = new PlayerProfile();
                }
                cached.recent ??= new List<bool>();
                return cached;
            }
        }

        /// <summary>The result of one finished game. Returns true if it set a new longest streak.</summary>
        public bool Record(bool madeItInside, bool heldKey, int realSupply, bool online)
        {
            gamesPlayed++;
            if (online) onlineGames++;
            if (realSupply > bestSupply) bestSupply = realSupply;

            bool newBest = false;
            if (madeItInside)
            {
                wins++;
                if (heldKey) keyWins++;
                currentStreak++;
                if (currentStreak > longestStreak)
                {
                    longestStreak = currentStreak;
                    newBest = true;
                }
            }
            else
            {
                currentStreak = 0;
            }

            recent.Add(madeItInside);
            if (recent.Count > HistoryLength) recent.RemoveRange(0, recent.Count - HistoryLength);

            string today = DateTime.Now.ToString("d MMM yyyy");
            if (string.IsNullOrEmpty(firstPlayed)) firstPlayed = today;
            lastPlayed = today;

            Save();
            return newBest;
        }

        /// <summary>Wipes the record on this device.</summary>
        public static void Reset()
        {
            cached = new PlayerProfile();
            cached.Save();
        }

        private void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
    }
}
