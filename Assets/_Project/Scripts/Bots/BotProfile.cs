using System;
using UnityEngine;

namespace TDTS.Bots
{
    public enum BotStrategy
    {
        Random,     // Picks any legal move. The baseline the other strategies are compared against.
        Passive,    // Scavenges and peeks, trades away known Trash, rarely steals or skips anyone.
        Aggressive, // Steals, swaps and skips, and goes after whoever looks closest to surviving.
        HumanLike   // Balanced, plays the obvious good moves, sometimes misjudges and forgets things.
    }

    /// <summary>
    /// How a bot weighs its options. Each preset is just a set of numbers, so new
    /// strategies are easy to add and to compare in the Balance Simulator.
    /// </summary>
    [Serializable]
    public class BotProfile
    {
        public BotStrategy strategy = BotStrategy.HumanLike;

        [Tooltip("How much it likes Steal, Swap, Skips and Swap Deck against other players.")]
        [Range(0f, 2f)] public float aggression = 1f;

        [Tooltip("How much it likes spending its Day on a trade, and how readily it accepts one.")]
        [Range(0f, 2f)] public float tradeWillingness = 1f;

        [Tooltip("How much it values looking at its own cards (peeking, the Flashlight).")]
        [Range(0f, 2f)] public float caution = 1f;

        [Tooltip("Random wobble added to every score. 0 = always the best move it can see.")]
        [Range(0f, 3f)] public float noise = 0.5f;

        [Tooltip("Chance each Day of forgetting something it saw about a card that isn't in its hand.")]
        [Range(0f, 1f)] public float forgetChance = 0.1f;

        [Tooltip("Always knows the backs of the cards in its hand it has peeked at.")]
        public bool trackOwnPeeks = true;

        [Tooltip("Keeps track of cards it has seen after they move to other players.")]
        public bool trackOthers = true;

        public static BotProfile For(BotStrategy strategy) => strategy switch
        {
            BotStrategy.Random => new BotProfile
            {
                strategy = strategy, aggression = 1f, tradeWillingness = 1f, caution = 1f, noise = 0f, forgetChance = 1f
            },
            BotStrategy.Passive => new BotProfile
            {
                strategy = strategy, aggression = 0.2f, tradeWillingness = 1.2f, caution = 1.5f, noise = 0.3f, forgetChance = 0f
            },
            BotStrategy.Aggressive => new BotProfile
            {
                strategy = strategy, aggression = 1.8f, tradeWillingness = 0.5f, caution = 0.6f, noise = 0.3f, forgetChance = 0f
            },
            _ => new BotProfile
            {
                strategy = BotStrategy.HumanLike, aggression = 1f, tradeWillingness = 1f, caution = 1f, noise = 1f, forgetChance = 0.15f
            },
        };

        public string Label => strategy == BotStrategy.HumanLike ? "Human-like" : strategy.ToString();

        /// <summary>
        /// The same strategy played by a casual player: it doesn't follow cards once they leave its hand,
        /// forgets what it peeked (its own hand too) about a third of the time each Day, and misjudges more.
        /// </summary>
        public BotProfile Forgetful()
        {
            var copy = (BotProfile)MemberwiseClone();
            copy.trackOwnPeeks = false;
            copy.trackOthers = false;
            copy.forgetChance = System.Math.Max(forgetChance, 0.35f);
            if (strategy != BotStrategy.Random) copy.noise = System.Math.Max(noise, 1f);
            return copy;
        }
    }
}
