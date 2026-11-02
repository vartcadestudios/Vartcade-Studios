using System.Collections.Generic;
using System.Linq;
using TDTS.Data;

namespace TDTS.Bots
{
    /// <summary>
    /// What one bot has seen. A card's back never changes, so once a bot has peeked at a card
    /// it still knows what it is after the card is stolen, swapped or traded to someone else.
    /// That's how bots track other players' supplies.
    /// </summary>
    public class BotMemory
    {
        private readonly Dictionary<CardData, BackType> knownBacks = new Dictionary<CardData, BackType>();

        public int Count => knownBacks.Count;

        public void Saw(CardData card)
        {
            if (card != null) knownBacks[card] = card.back;
        }

        public bool Knows(CardData card) => card != null && knownBacks.ContainsKey(card);

        /// <summary>Cards in its own hand it has peeked at are always known.</summary>
        public void RememberOwnHand(PlayerData me)
        {
            foreach (CardData card in me.hand)
                if (card.peeked) Saw(card);
        }

        /// <summary>Human-like bots lose track of cards that left their hand.</summary>
        public void Forget(float chance, PlayerData me, System.Random rng)
        {
            if (chance <= 0f) return;
            foreach (CardData card in knownBacks.Keys.ToList())
                if (!me.hand.Contains(card) && rng.NextDouble() < chance)
                    knownBacks.Remove(card);
        }
    }
}
