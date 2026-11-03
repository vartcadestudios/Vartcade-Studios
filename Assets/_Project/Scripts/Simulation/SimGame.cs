using System;
using System.Collections.Generic;
using System.Linq;
using TDTS.Bots;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.Simulation
{
    /// <summary>One seat's outcome at Sundown.</summary>
    public class SeatResult
    {
        public BotStrategy strategy;
        public bool survived;
        public bool heldKey;
        public int realSupply;
        public string reason;
    }

    /// <summary>Everything recorded about one simulated game.</summary>
    public class SimGameResult
    {
        public readonly List<SeatResult> seats = new List<SeatResult>();
        public readonly List<(string card, int seat)> cardPlays = new List<(string, int)>();
        public readonly Dictionary<MoveKind, int>[] movesBySeat;
        public readonly List<RondoResult> rondo = new List<RondoResult>();
        public int sundownDay;
        public int tradesOffered;
        public int tradesAccepted;
        public int stalledTurns; // turns that ended early because no move went through

        public SimGameResult(int seatCount)
        {
            movesBySeat = new Dictionary<MoveKind, int>[seatCount];
            for (int i = 0; i < seatCount; i++) movesBySeat[i] = new Dictionary<MoveKind, int>();
        }
    }

    /// <summary>
    /// A whole game of Ten Days to Sundown played by bots, with no scene, UI or waiting.
    /// The rules mirror GameManager (Day and Night turns, trades, peek/steal/swap with one
    /// Steal or Swap a night and a peek required, all action cards, the Rondo, Sundown seating),
    /// plus the test switches in SimSettings. If you change a rule in GameManager, change it here too.
    /// </summary>
    public class SimGame : IBotTable
    {
        private readonly SimSettings settings;
        private readonly Random rng;
        private readonly List<PlayerData> players = new List<PlayerData>();
        private readonly Dictionary<PlayerData, int> seatOf = new Dictionary<PlayerData, int>();
        private readonly Dictionary<PlayerData, BotBrain> brains = new Dictionary<PlayerData, BotBrain>();
        private readonly List<CardData> deck = new List<CardData>();
        private readonly List<CardData> discardPile = new List<CardData>();
        private readonly List<CardData> removedFromGame = new List<CardData>();
        private readonly List<RondoResult> rondoUsed = new List<RondoResult>();
        private readonly HashSet<PlayerData> hadTurn = new HashSet<PlayerData>();
        private readonly SimGameResult result;

        private GameState state = GameState.Init;
        private DayRule dayRule = DayRule.Normal;
        private int currentDay = 1;
        private int sundownDay = 10;
        private int turnDirection = 1;
        private int activeIndex;
        private int actionsLeft;
        private int peeksThisTurn;
        private bool stoleOrSwapped;
        private CardData stolenThisTurn;

        public SimGame(SimSettings settings, int seed)
        {
            this.settings = settings;
            rng = new Random(seed);
            result = new SimGameResult(settings.seats.Length);
        }

        // ---------- IBotTable ----------

        public IReadOnlyList<PlayerData> Players => players;
        public bool IsHuman(PlayerData player) => false;
        public GameState State => state;
        public DayRule CurrentDayRule => dayRule;
        public int CurrentDay => currentDay;
        public int SundownDay => sundownDay;
        public int DeckCount => deck.Count;
        public bool TurnOrderReversed => turnDirection < 0;
        public int ActionsLeft => actionsLeft;
        public int PeeksThisTurn => peeksThisTurn;
        public bool StoleOrSwappedThisTurn => stoleOrSwapped;
        public bool EyesClosed => state == GameState.NightPhase || (state == GameState.DayPhase && dayRule == DayRule.Blackout);

        // ---------- Running a game ----------

        public SimGameResult Run()
        {
            SetUp();

            while (true)
            {
                state = GameState.DayPhase;
                dayRule = DayRule.Normal;
                foreach (var pair in brains) pair.Value.OnNewDay(pair.Key);
                if (settings.rondo && currentDay >= GameManager.RondoStartDay) SpinRondo();
                PlayPhase();

                state = GameState.NightPhase;
                PlayPhase();

                if (currentDay >= sundownDay) break;
                currentDay++;
            }

            state = GameState.Sundown;
            DecideSurvivors();

            result.sundownDay = sundownDay;
            for (int i = 0; i < players.Count; i++) result.seats.Add(null); // filled in seat order below
            foreach (PlayerData p in players)
            {
                int seat = seatOf[p];
                result.seats[seat] = new SeatResult
                {
                    strategy = settings.seats[seat],
                    survived = p.isSeated,
                    heldKey = p.HoldsKey,
                    realSupply = p.RealSupplyCount,
                    reason = p.resultReason,
                };
            }
            return result;
        }

        /// <summary>Spec setup S1 to S5.</summary>
        private void SetUp()
        {
            for (int seat = 0; seat < settings.seats.Length; seat++)
            {
                var p = new PlayerData($"{settings.seats[seat]} {seat + 1}");
                players.Add(p);
                seatOf[p] = seat;
                brains[p] = new BotBrain(BotProfile.For(settings.seats[seat]), rng);
            }

            DeckBuilder.Shuffle(players, rng);
            deck.AddRange(DeckBuilder.BuildDrawPile(settings.BuildRecipe(), rng, removedFromGame));

            for (int round = 0; round < GameManager.StartingHandSize; round++)
                foreach (PlayerData p in players)
                    GiveCard(p, TakeTopCard());
        }

        private void PlayPhase()
        {
            hadTurn.Clear();
            activeIndex = turnDirection > 0 ? 0 : players.Count - 1;

            while (true)
            {
                PlayTurn(players[activeIndex]);

                int next = -1;
                for (int step = 1; step < players.Count && next < 0; step++)
                {
                    int i = ((activeIndex + step * turnDirection) % players.Count + players.Count) % players.Count;
                    if (!hadTurn.Contains(players[i])) next = i;
                }
                if (next < 0) return;
                activeIndex = next;
            }
        }

        private void PlayTurn(PlayerData p)
        {
            hadTurn.Add(p);
            stolenThisTurn = null;
            stoleOrSwapped = false;
            peeksThisTurn = 0;

            BotBrain brain = brains[p];
            brain.NewTurn();

            bool isDay = state == GameState.DayPhase;
            if ((isDay && p.daySkip) || (!isDay && p.nightSkip))
            {
                if (isDay) p.daySkip = false;
                else p.nightSkip = false;
                return;
            }

            actionsLeft = isDay ? settings.dayActions : settings.nightActions;
            if (isDay && settings.alwaysDraw && deck.Count > 0) GiveCard(p, TakeTopCard());

            // A guard against a turn that never ends; a real one ends in a handful of moves
            for (int attempt = 0; actionsLeft > 0 && attempt < 40; attempt++)
            {
                if (!HasAnyMove(p)) return;
                if (settings.allCardsVisible) ShowEveryBack(brain);

                bool acted = false;
                foreach (BotMove move in brain.RankMoves(this, p))
                {
                    if (move.kind == MoveKind.Pass) break;
                    if (!TryMove(p, move)) continue;

                    Count(p, move.kind);
                    acted = true;
                    break;
                }
                if (!acted)
                {
                    result.stalledTurns++;
                    return;
                }
            }
        }

        private void ShowEveryBack(BotBrain brain)
        {
            foreach (PlayerData p in players)
                foreach (CardData card in p.hand)
                    brain.Memory.Saw(card);
        }

        private void Count(PlayerData p, MoveKind kind)
        {
            var moves = result.movesBySeat[seatOf[p]];
            moves[kind] = moves.TryGetValue(kind, out int n) ? n + 1 : 1;
        }

        /// <summary>Same as GameManager.HasAnyMove: there's no Pass while a move is possible.</summary>
        private bool HasAnyMove(PlayerData player)
        {
            bool othersHaveCards = players.Any(p => p != player && p.hand.Count > 0);
            if (state == GameState.DayPhase)
            {
                bool cardsAllowed = dayRule != DayRule.TradingOnly;
                return (cardsAllowed && deck.Count > 0)
                    || (player.hand.Count > 0 && othersHaveCards)
                    || (cardsAllowed && player.hand.Any(c => c.HasMorningFront));
            }
            return (othersHaveCards && !stoleOrSwapped) || player.hand.Count > 0;
        }

        // ---------- Moves ----------

        private bool TryMove(PlayerData p, BotMove m)
        {
            bool isDay = state == GameState.DayPhase;
            bool isNight = state == GameState.NightPhase;

            // R4: the last Night action must be a peek if there hasn't been one
            bool mustPeek = isNight && actionsLeft == 1 && peeksThisTurn == 0 && p.hand.Count > 0;
            if (mustPeek && m.kind != MoveKind.Peek) return false;

            switch (m.kind)
            {
                case MoveKind.Scavenge:
                    if (!isDay || dayRule == DayRule.TradingOnly || deck.Count == 0) return false;
                    GiveCard(p, TakeTopCard());
                    SpendAction();
                    return true;

                case MoveKind.Trade:
                    return isDay && Trade(p, m);

                case MoveKind.PlayCard:
                    return PlayCard(p, m.handIndex, m.target);

                case MoveKind.Peek:
                    if (!isNight || !ValidIndex(p, m.handIndex)) return false;
                    CardData peeked = p.hand[m.handIndex];
                    peeked.peeked = true;
                    brains[p].Memory.Saw(peeked);
                    peeksThisTurn++;
                    SpendAction();
                    return true;

                case MoveKind.Steal:
                    if (!isNight || stoleOrSwapped || m.target == null || m.target == p || !ValidIndex(m.target, m.targetCardIndex)) return false;
                    CardData stolen = m.target.hand[m.targetCardIndex];
                    m.target.hand.RemoveAt(m.targetCardIndex);
                    GiveCard(p, stolen);
                    stolenThisTurn = stolen;
                    stoleOrSwapped = true;
                    SpendAction();
                    return true;

                case MoveKind.Swap:
                    if (!isNight || stoleOrSwapped || m.target == null || m.target == p || m.target.hand.Count == 0 || !ValidIndex(p, m.handIndex)) return false;
                    CardData mine = p.hand[m.handIndex];
                    p.hand.RemoveAt(m.handIndex);
                    CardData theirs = TakeRandomCard(m.target);
                    GiveCard(p, theirs);
                    GiveCard(m.target, mine);
                    stoleOrSwapped = true;
                    SpendAction();
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>P1/P2: offer, the partner's brain answers straight away. A refusal keeps the action.</summary>
        private bool Trade(PlayerData from, BotMove m)
        {
            PlayerData to = m.target;
            if (to == null || to == from || !ValidIndex(from, m.handIndex) || !ValidIndex(to, m.targetCardIndex)) return false;

            CardData offered = from.hand[m.handIndex];
            CardData wanted = to.hand[m.targetCardIndex];
            result.tradesOffered++;

            if (!brains[to].WantsTrade(to, offered, wanted))
            {
                brains[from].NoteTradeRefused(to);
                return false;
            }

            from.hand.Remove(offered);
            to.hand.Remove(wanted);
            GiveCard(to, offered);
            GiveCard(from, wanted);
            result.tradesAccepted++;
            SpendAction();
            return true;
        }

        /// <summary>P4 / P10 and the effects G19 to G26.</summary>
        private bool PlayCard(PlayerData player, int handIndex, PlayerData target)
        {
            if (!ValidIndex(player, handIndex)) return false;
            CardData card = player.hand[handIndex];
            bool isDay = state == GameState.DayPhase;

            bool allowed = (isDay && card.HasMorningFront && dayRule != DayRule.TradingOnly) ||
                           (state == GameState.NightPhase && card.HasNightBack && card.peeked);
            if (!allowed) return false;

            bool canTargetSelf = GameManager.CanTargetSelf(card, isDay);
            if (!GameManager.NeedsTarget(card, isDay)) target = null;
            else if (canTargetSelf && target == null) target = player;
            else if (target == null || (target == player && !canTargetSelf)) return false;

            player.hand.RemoveAt(handIndex);
            discardPile.Add(card);

            result.cardPlays.Add((isDay ? card.FrontName : card.BackName, seatOf[player]));

            if (isDay) ResolveMorningCard(card.morningAction, player, target);
            else ResolveNightCard(card, player, target);

            SpendAction();
            return true;
        }

        private void ResolveMorningCard(MorningActionType action, PlayerData player, PlayerData target)
        {
            switch (action)
            {
                case MorningActionType.SwapDeck:
                    (player.hand, target.hand) = (target.hand, player.hand);
                    foreach (var c in player.hand) c.peeked = false;
                    foreach (var c in target.hand) c.peeked = false;
                    break;

                case MorningActionType.ReverseTurn:
                    turnDirection = -turnDirection;
                    break;

                case MorningActionType.SkipNextPlayer:
                    players[(activeIndex + turnDirection + players.Count) % players.Count].daySkip = true;
                    break;

                case MorningActionType.EveryonePlusOne:
                    int start = players.IndexOf(player);
                    for (int step = 0; step < players.Count && deck.Count > 0; step++)
                        GiveCard(players[((start + step * turnDirection) % players.Count + players.Count) % players.Count], TakeTopCard());
                    break;

                case MorningActionType.EveryoneSwitches:
                    var hands = players.Select(p => p.hand).ToList();
                    for (int i = 0; i < players.Count; i++)
                    {
                        int to = (i + turnDirection + players.Count) % players.Count;
                        players[to].hand = hands[i];
                        foreach (var c in hands[i]) c.peeked = false;
                    }
                    break;
            }
        }

        private void ResolveNightCard(CardData card, PlayerData player, PlayerData target)
        {
            switch (card.nightAction)
            {
                case NightActionType.Flashlight:
                    // R10: one use, lights every card in the chosen hand (yours included)
                    foreach (CardData c in target.hand)
                    {
                        brains[player].Memory.Saw(c);
                        if (target == player) c.peeked = true;
                    }
                    break;
                case NightActionType.TargetedNightSkip:
                    target.nightSkip = true;
                    break;
                case NightActionType.TargetedDaySkip:
                    target.daySkip = true;
                    break;
            }
        }

        private void SpendAction() => actionsLeft = Math.Max(0, actionsLeft - 1);

        // ---------- Rondo (G5 to G13) ----------

        private void SpinRondo()
        {
            var options = ((RondoResult[])Enum.GetValues(typeof(RondoResult))).Where(r => !rondoUsed.Contains(r)).ToList();
            if (options.Count == 0) return;

            RondoResult spun = options[rng.Next(options.Count)];
            rondoUsed.Add(spun);
            result.rondo.Add(spun);

            switch (spun)
            {
                case RondoResult.Purge:
                    for (int i = 0; i < GameManager.PurgeCount && deck.Count > 0; i++) removedFromGame.Add(TakeTopCard());
                    break;
                case RondoResult.TradingOnly:
                    dayRule = DayRule.TradingOnly;
                    break;
                case RondoResult.HungerDisaster:
                    LoseOneCardEach(c => c.IsRealSupply && c.backSupply == SupplyKind.Food);
                    break;
                case RondoResult.Tornado:
                    var pool = players.SelectMany(p => p.hand).ToList();
                    var sizes = players.Select(p => p.hand.Count).ToList();
                    DeckBuilder.Shuffle(pool, rng);
                    int next = 0;
                    for (int i = 0; i < players.Count; i++)
                    {
                        players[i].hand = new List<CardData>();
                        for (int n = 0; n < sizes[i]; n++) GiveCard(players[i], pool[next++]);
                    }
                    break;
                case RondoResult.DoomsdayClock:
                    sundownDay = Math.Max(currentDay, sundownDay - 1);
                    break;
                case RondoResult.Blackout:
                    dayRule = DayRule.Blackout;
                    break;
                case RondoResult.Outbreak:
                    LoseOneCardEach(c => c.IsRealSupply &&
                        (c.backSupply == SupplyKind.Medicine || c.backSupply == SupplyKind.Entertainment));
                    break;
            }
        }

        private void LoseOneCardEach(Func<CardData, bool> matches)
        {
            foreach (PlayerData p in players)
            {
                var candidates = p.hand.Where(matches).ToList();
                if (candidates.Count == 0) continue;
                CardData card = candidates[rng.Next(candidates.Count)];
                p.hand.Remove(card);
                discardPile.Add(card);
            }
        }

        // ---------- Sundown (G18, R1, R2) — same as GameManager.DecideSurvivors ----------

        private void DecideSurvivors()
        {
            foreach (var p in players)
            {
                p.isSeated = false;
                p.resultReason = "not enough Supply";
            }

            int seatsLeft = 2;

            PlayerData keyHolder = players.FirstOrDefault(p => p.HoldsKey);
            if (keyHolder != null)
            {
                keyHolder.isSeated = true;
                keyHolder.resultReason = "Key";
                seatsLeft--;
            }

            var qualifiers = players
                .Where(p => !p.isSeated && p.RealSupplyCount >= GameManager.SupplyToSurvive)
                .OrderByDescending(p => p.RealSupplyCount)
                .ToList();

            int index = 0;
            while (seatsLeft > 0 && index < qualifiers.Count)
            {
                int supply = qualifiers[index].RealSupplyCount;
                var tied = qualifiers.Skip(index).TakeWhile(p => p.RealSupplyCount == supply).ToList();

                if (tied.Count <= seatsLeft)
                {
                    foreach (var p in tied)
                    {
                        p.isSeated = true;
                        p.resultReason = "Supply";
                    }
                    seatsLeft -= tied.Count;
                }
                else
                {
                    foreach (var p in tied) p.resultReason = "tied for the last seat";
                    break;
                }
                index += tied.Count;
            }

            for (int i = index; i < qualifiers.Count; i++)
                if (!qualifiers[i].isSeated && qualifiers[i].resultReason != "tied for the last seat")
                    qualifiers[i].resultReason = "bunker full";
        }

        // ---------- Cards ----------

        private CardData TakeTopCard()
        {
            CardData card = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
            return card;
        }

        private static void GiveCard(PlayerData to, CardData card)
        {
            card.peeked = false;
            to.hand.Add(card);
        }

        private CardData TakeRandomCard(PlayerData from)
        {
            int i = rng.Next(from.hand.Count);
            CardData card = from.hand[i];
            from.hand.RemoveAt(i);
            return card;
        }

        private static bool ValidIndex(PlayerData p, int index) => index >= 0 && index < p.hand.Count;
    }
}
