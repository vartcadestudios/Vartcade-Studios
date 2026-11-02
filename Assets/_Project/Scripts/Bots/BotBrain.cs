using System.Collections.Generic;
using System.Linq;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.Bots
{
    /// <summary>
    /// Decides a bot's moves. It lists every move it could make, scores each one with the
    /// considerations below (weighted by its BotProfile), and ranks them best first:
    ///   - holding on to the Bunker Key, and going after it when it knows where it is
    ///   - getting to 3 real Supply
    ///   - remembering the backs it has seen, in its hand and in other hands (BotMemory)
    ///   - trading only when it's worth the whole Day
    ///   - stealing and swapping, aimed at whoever looks closest to surviving
    ///   - playing action cards when they help it
    /// It never reads a back it hasn't seen, so it plays by the same information a person has.
    /// </summary>
    public class BotBrain
    {
        // How much a card is worth to the bot, by what it knows the back is
        private const float KeyValue = 10f;
        private const float NeededSupplyValue = 3f;
        private const float ExtraSupplyValue = 1f;
        private const float TrashValue = -1f;
        private const float NightCardValue = 1.5f;
        private const float UnknownValue = 1.2f;
        private const float MorningFrontBonus = 0.4f;
        private const float PassScore = -5f;

        public BotProfile Profile { get; }
        public BotMemory Memory { get; } = new BotMemory();

        private readonly System.Random rng;

        // Players who refused its trade this turn, so it doesn't keep asking them
        private readonly HashSet<PlayerData> refusedThisTurn = new HashSet<PlayerData>();

        public BotBrain(BotProfile profile, System.Random rng)
        {
            Profile = profile;
            this.rng = rng;
        }

        /// <summary>Called when its turn starts.</summary>
        public void NewTurn() => refusedThisTurn.Clear();

        /// <summary>Called when a player turns down its trade.</summary>
        public void NoteTradeRefused(PlayerData partner) => refusedThisTurn.Add(partner);

        // ---------- What a card is worth ----------

        private bool KnowsBack(CardData card, PlayerData me) =>
            (card.peeked && me.hand.Contains(card)) || Memory.Knows(card);

        private int KnownSupply(PlayerData me) => me.hand.Count(c => c.IsRealSupply && KnowsBack(c, me));

        /// <summary>What this card is worth to me, from what I know about it.</summary>
        public float ValueOf(CardData card, PlayerData me)
        {
            float value;
            if (!KnowsBack(card, me))
            {
                value = UnknownValue;
            }
            else
            {
                value = card.back switch
                {
                    BackType.BunkerKey => KeyValue,
                    BackType.Supply => KnownSupply(me) > GameManager.SupplyToSurvive ? ExtraSupplyValue : NeededSupplyValue,
                    BackType.NightAction => NightCardValue,
                    _ => TrashValue,
                };
            }
            if (card.HasMorningFront) value += MorningFrontBonus;
            return value;
        }

        /// <summary>How close a player looks to surviving, from what I know about their cards.</summary>
        private float HandValue(PlayerData player, PlayerData me, CardData leaving = null) =>
            player.hand.Where(c => c != leaving).Sum(c => ValueOf(c, me));

        // ---------- Choosing a move ----------

        /// <summary>Every move it could make right now, best first. The caller tries them in order.</summary>
        public List<BotMove> RankMoves(IBotTable table, PlayerData me)
        {
            Memory.RememberOwnHand(me);

            var moves = new List<BotMove>();
            if (table.State == GameState.DayPhase) AddDayMoves(table, me, moves);
            else if (table.State == GameState.NightPhase) AddNightMoves(table, me, moves);
            moves.Add(new BotMove { kind = MoveKind.Pass, score = PassScore, reason = "nothing else worked" });

            foreach (BotMove move in moves)
            {
                if (Profile.strategy == BotStrategy.Random && move.kind != MoveKind.Pass)
                    move.score = (float)rng.NextDouble();
                else
                    move.score += (float)(rng.NextDouble() * 2 - 1) * Profile.noise;
            }

            return moves.OrderByDescending(m => m.score).ToList();
        }

        private IEnumerable<PlayerData> Others(IBotTable table, PlayerData me) =>
            table.Players.Where(p => p != me && p.hand.Count > 0);

        private void AddDayMoves(IBotTable table, PlayerData me, List<BotMove> moves)
        {
            bool tradingOnly = table.CurrentDayRule == DayRule.TradingOnly;
            int need = System.Math.Max(0, GameManager.SupplyToSurvive - KnownSupply(me));

            // Scavenge: a fresh card is worth more the more Supply it still needs
            if (!tradingOnly && table.DeckCount > 0)
            {
                moves.Add(new BotMove
                {
                    kind = MoveKind.Scavenge,
                    score = UnknownValue + need * 0.4f,
                    reason = need > 0 ? $"still needs {need} more real Supply" : "a free card",
                });
            }

            // Trade: a whole Day for one card, so only when it gains something clear
            if (me.hand.Count > 0)
            {
                foreach (PlayerData partner in Others(table, me))
                {
                    if (table.EyesClosed && table.IsHuman(partner)) continue; // they couldn't see it
                    if (refusedThisTurn.Contains(partner)) continue;

                    int give = IndexOfMin(me.hand, c => ValueOf(c, me));
                    int want = IndexOfMax(partner.hand, c => ValueOf(c, me));
                    CardData giving = me.hand[give], wanting = partner.hand[want];
                    float gain = ValueOf(wanting, me) - ValueOf(giving, me);

                    moves.Add(new BotMove
                    {
                        kind = MoveKind.Trade, handIndex = give, target = partner, targetCardIndex = want,
                        score = gain * Profile.tradeWillingness - 0.5f + (tradingOnly ? 2f : 0f),
                        reason = Memory.Knows(wanting) || KnowsBack(giving, me)
                            ? $"trade its {Describe(giving, me)} for {partner.playerName}'s {Describe(wanting, me)}"
                            : $"trade with {partner.playerName}",
                    });
                }
            }

            // Morning cards: only worth playing when the effect beats what the card itself might be
            if (tradingOnly) return;
            for (int i = 0; i < me.hand.Count; i++)
            {
                CardData card = me.hand[i];
                if (!card.HasMorningFront) continue;
                float cost = ValueOf(card, me) - MorningFrontBonus; // playing it throws the back away

                switch (card.morningAction)
                {
                    case MorningActionType.SwapDeck:
                        foreach (PlayerData other in Others(table, me))
                        {
                            float gain = HandValue(other, me) - HandValue(me, me, card);
                            moves.Add(new BotMove
                            {
                                kind = MoveKind.PlayCard, handIndex = i, target = other,
                                score = gain * (0.5f + Profile.aggression * 0.5f) - cost,
                                reason = $"Swap Deck: {other.playerName}'s hand looks better than its own",
                            });
                        }
                        break;

                    case MorningActionType.EveryoneSwitches:
                        PlayerData giver = SeatBefore(table, me);
                        moves.Add(new BotMove
                        {
                            kind = MoveKind.PlayCard, handIndex = i,
                            score = HandValue(giver, me) - HandValue(me, me, card) - cost,
                            reason = $"Everyone Switches: it would get {giver.playerName}'s hand",
                        });
                        break;

                    case MorningActionType.SkipNextPlayer:
                        moves.Add(new BotMove
                        {
                            kind = MoveKind.PlayCard, handIndex = i,
                            score = 1f * Profile.aggression - cost,
                            reason = $"Skip Next Player: slow down {SeatAfter(table, me).playerName}",
                        });
                        break;

                    case MorningActionType.EveryonePlusOne:
                        moves.Add(new BotMove
                        {
                            kind = MoveKind.PlayCard, handIndex = i,
                            score = (need > 0 ? 1.2f : 0.6f) - cost,
                            reason = "Everyone +1 Card: one more card toward its Supply",
                        });
                        break;

                    case MorningActionType.ReverseTurn:
                        moves.Add(new BotMove
                        {
                            kind = MoveKind.PlayCard, handIndex = i,
                            score = 0.3f * Profile.aggression - cost,
                            reason = "Reverse Turn: shake up the order",
                        });
                        break;
                }
            }
        }

        private void AddNightMoves(IBotTable table, PlayerData me, List<BotMove> moves)
        {
            // R4: the last action of the night must be a peek if it hasn't peeked yet
            bool mustPeek = table.ActionsLeft == 1 && table.PeeksThisTurn == 0 && me.hand.Count > 0;

            for (int i = 0; i < me.hand.Count; i++)
            {
                bool unknown = !KnowsBack(me.hand[i], me);
                if (!unknown && !mustPeek) continue;
                moves.Add(new BotMove
                {
                    kind = MoveKind.Peek, handIndex = i,
                    score = (unknown ? 1f : 0.1f) * Profile.caution + (table.PeeksThisTurn == 0 ? 2f : 0f),
                    reason = unknown ? $"doesn't know what its {me.hand[i].FrontName} really is" : "has to peek tonight",
                });
            }
            if (mustPeek) return;

            PlayerData leader = Others(table, me).OrderByDescending(p => HandValue(p, me)).FirstOrDefault();

            if (!table.StoleOrSwappedThisTurn)
            {
                foreach (PlayerData victim in Others(table, me))
                {
                    // Steal: it picks which card to take, so it takes the one it values most
                    int best = IndexOfMax(victim.hand, c => ValueOf(c, me));
                    CardData taking = victim.hand[best];
                    float leaderBonus = victim == leader ? 0.5f : 0f;
                    moves.Add(new BotMove
                    {
                        kind = MoveKind.Steal, target = victim, targetCardIndex = best,
                        score = (ValueOf(taking, me) + leaderBonus) * Profile.aggression,
                        reason = Memory.Knows(taking)
                            ? $"steal {victim.playerName}'s {taking.FrontName}: it remembers that's {taking.BackName}"
                            : victim == leader ? $"steal from {victim.playerName}, who looks closest to surviving" : $"steal from {victim.playerName}",
                    });

                    // Swap: get rid of its worst card for one of theirs at random
                    if (me.hand.Count > 0)
                    {
                        int worst = IndexOfMin(me.hand, c => ValueOf(c, me));
                        float theirs = victim.hand.Average(c => ValueOf(c, me));
                        moves.Add(new BotMove
                        {
                            kind = MoveKind.Swap, handIndex = worst, target = victim,
                            score = (theirs - ValueOf(me.hand[worst], me)) * (0.5f + Profile.aggression * 0.5f),
                            reason = $"swap away its {Describe(me.hand[worst], me)} to {victim.playerName}",
                        });
                    }
                }
            }

            // Night cards it has peeked at
            for (int i = 0; i < me.hand.Count; i++)
            {
                CardData card = me.hand[i];
                if (!card.HasNightBack || !card.peeked || leader == null) continue;

                switch (card.nightAction)
                {
                    case NightActionType.Flashlight:
                        PlayerData mostUnknown = Others(table, me).OrderByDescending(p => p.hand.Count(c => !Memory.Knows(c))).First();
                        moves.Add(new BotMove
                        {
                            kind = MoveKind.PlayCard, handIndex = i, target = mostUnknown,
                            score = 0.8f * Profile.caution,
                            reason = $"Flashlight: find out what {mostUnknown.playerName} is holding",
                        });
                        break;

                    case NightActionType.TargetedNightSkip:
                    case NightActionType.TargetedDaySkip:
                        moves.Add(new BotMove
                        {
                            kind = MoveKind.PlayCard, handIndex = i, target = leader,
                            score = 1f * Profile.aggression,
                            reason = $"{CardData.NameOf(card.nightAction)} on {leader.playerName}, who looks closest to surviving",
                        });
                        break;
                }
            }
        }

        // ---------- Answering a trade ----------

        /// <summary>Accept when what it gets is worth more to it than what it gives.</summary>
        public bool WantsTrade(PlayerData me, CardData offered, CardData wanted)
        {
            Memory.RememberOwnHand(me);
            if (Profile.strategy == BotStrategy.Random) return rng.NextDouble() < 0.5;

            float gain = ValueOf(offered, me) - ValueOf(wanted, me);
            float wobble = (float)(rng.NextDouble() * 2 - 1) * Profile.noise;
            return gain * Profile.tradeWillingness + wobble > 0.2f;
        }

        /// <summary>Called at the start of each Day.</summary>
        public void OnNewDay(PlayerData me) => Memory.Forget(Profile.forgetChance, me, rng);

        // ---------- Helpers ----------

        private string Describe(CardData card, PlayerData me) =>
            KnowsBack(card, me) ? $"{card.FrontName} ({card.BackName})" : card.FrontName;

        private static PlayerData SeatAfter(IBotTable table, PlayerData me) => SeatOffset(table, me, 1);
        private static PlayerData SeatBefore(IBotTable table, PlayerData me) => SeatOffset(table, me, -1);

        private static PlayerData SeatOffset(IBotTable table, PlayerData me, int step)
        {
            int n = table.Players.Count;
            int dir = table.TurnOrderReversed ? -1 : 1;
            int i = 0;
            for (; i < n; i++) if (table.Players[i] == me) break;
            return table.Players[((i + step * dir) % n + n) % n];
        }

        private static int IndexOfMax(List<CardData> cards, System.Func<CardData, float> value)
        {
            int best = 0;
            for (int i = 1; i < cards.Count; i++) if (value(cards[i]) > value(cards[best])) best = i;
            return best;
        }

        private static int IndexOfMin(List<CardData> cards, System.Func<CardData, float> value)
        {
            int best = 0;
            for (int i = 1; i < cards.Count; i++) if (value(cards[i]) < value(cards[best])) best = i;
            return best;
        }
    }
}
