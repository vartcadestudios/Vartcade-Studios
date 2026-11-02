using TDTS.Data;

namespace TDTS.Bots
{
    public enum MoveKind { Scavenge, Trade, PlayCard, Peek, Steal, Swap, Pass }

    /// <summary>One thing a bot could do, how much it wants to, and why.</summary>
    public class BotMove
    {
        public MoveKind kind;
        public int handIndex = -1;        // Trade (giving), PlayCard, Peek, Swap (giving)
        public PlayerData target;         // Trade, Steal, Swap, and cards that need a target
        public int targetCardIndex = -1;  // Trade (wanted), Steal (taken)
        public float score;
        public string reason = "";

        public override string ToString()
        {
            string who = target != null ? $" {target.playerName}" : "";
            return $"{kind}{who} ({score:0.0}): {reason}";
        }
    }
}
