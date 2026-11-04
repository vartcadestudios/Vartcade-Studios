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
        public int turnPosition; // 0 = first in the starting turn order

        // Moments players tend to find frustrating
        public int timesStolenFrom;   // someone took one of your cards with Steal
        public int supplyTaken;       // real Supply you lost to someone else's Steal or Swap
        public int keyTaken;          // the Key left your hand because of someone else (or an event)
        public int handScrambled;     // you lost 2+ real Supply at once to Swap Deck, Everyone Switches or Tornado
        public int rondoSupplyLost;   // real Supply a Rondo event took out of your hand
        public int turnsSkipped;      // whole turns lost to a Skip card
        public int turnsWithNoMove;   // turns that ended with actions unused: nothing possible or nothing worked
        public int tradesRefused;     // your trade offers that were turned down
        public int peeksLost;         // cards you had peeked that someone else took or scrambled away
        public bool hopelessLastDay;  // went into the last Day with 0-1 real Supply and no Key
        public bool collapsedAtEnd;   // went into the last Day able to survive, then didn't
        public bool lostWith3Plus;    // had 3+ real Supply but lost a tie or found the bunker full
        public int actions;           // actions you took
        public int othersActions;     // actions everyone else took: time spent waiting
    }

    /// <summary>Everything recorded about one simulated game.</summary>
    public class SimGameResult
    {
        public readonly List<SeatResult> seats = new List<SeatResult>();
        public readonly List<(string card, int seat)> cardPlays = new List<(string, int)>();
        public readonly Dictionary<MoveKind, int>[] movesBySeat;
        public readonly List<RondoResult> rondo = new List<RondoResult>();
        public int sundownDay;
        public int bunkerSeats = 2;
        public int catchUpDraws;
        public string keyFate = ""; // where the Key was at Sundown
        public int tradesOffered;
        public int tradesAccepted;
        public int stalledTurns; // turns that ended early because no move went through
        public int conversions;  // test rule: cards converted into real Supply
        public int forcedDraws;  // test rule: draws a draw rule made someone take

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
    /// plus the test switches in SimSettings (draw rules, resource conversion, a stronger Rondo,
    /// forgetful players). If you change a rule in GameManager, change it here too.
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
        private readonly HashSet<PlayerData> stolenFromTonight = new HashSet<PlayerData>();
        private readonly HashSet<CardData> usedFronts = new HashSet<CardData>();
        private bool freeNightCardUsed;
        private readonly SimGameResult result;

        private GameState state = GameState.Init;
        private DayRule dayRule = DayRule.Normal;
        private int currentDay = 1;
        private int conversionsLeft;
        private int sundownDay = 10;
        private int turnDirection = 1;
        private int activeIndex;
        private int actionsLeft;
        private int peeksThisTurn;
        private bool stoleOrSwapped;
        private CardData stolenThisTurn;

        // Frustration tracking
        private PlayerData keyHolder;
        private Dictionary<PlayerData, (int supply, bool key)> lastDayStart;

        public SimGame(SimSettings settings, int seed)
        {
            this.settings = settings;
            rng = new Random(seed);
            conversionsLeft = settings.conversionSupply;
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
        public int ConversionCost => settings.conversionCost;
        public int ConversionsLeft => conversionsLeft;
        public bool PeekRequired => settings.peekRequired;
        public bool PeekIsFree => peeksThisTurn < settings.freePeeks;
        public bool NeedDifferentKinds => settings.differentKinds;

        /// <summary>What counts toward the 3 needed: different kinds, or any real Supply under the old reading.</summary>
        private int Qualifying(PlayerData p) => settings.differentKinds ? p.SupplyKinds : p.RealSupplyCount;
        private int Rank(PlayerData p) => settings.differentKinds ? p.SupplyRank : p.RealSupplyCount;

        // ---------- Running a game ----------

        public SimGameResult Run()
        {
            SetUp();

            while (true)
            {
                state = GameState.DayPhase;
                dayRule = DayRule.Normal;
                foreach (var pair in brains) pair.Value.OnNewDay(pair.Key);
                if (settings.rondo && currentDay >= (settings.strongRondo ? 4 : GameManager.RondoStartDay)) SpinRondo();
                if (settings.catchUpDraw && currentDay > 1) CatchUpDraw();
                TrackKey(null);
                if (currentDay >= sundownDay && lastDayStart == null)
                    lastDayStart = players.ToDictionary(p => p, p => (Qualifying(p), p.HoldsKey));
                PlayPhase();

                state = GameState.NightPhase;
                PlayPhase();

                if (currentDay >= sundownDay) break;
                currentDay++;
            }

            state = GameState.Sundown;
            DecideSurvivors();

            result.sundownDay = sundownDay;
            result.bunkerSeats = settings.SeatsFor(players.Count);
            result.keyFate = KeyFate();
            foreach (PlayerData p in players)
            {
                SeatResult r = F(p);
                r.survived = p.isSeated;
                r.heldKey = p.HoldsKey;
                r.realSupply = p.RealSupplyCount;
                r.reason = p.resultReason;
                r.turnPosition = players.IndexOf(p);
                r.lostWith3Plus = !p.isSeated && (p.resultReason == "tied for the last seat" || p.resultReason == "bunker full");
                if (lastDayStart != null && lastDayStart.TryGetValue(p, out var start))
                {
                    r.hopelessLastDay = start.supply <= 1 && !start.key;
                    r.collapsedAtEnd = (start.supply >= GameManager.SupplyToSurvive || start.key) && !p.isSeated;
                }
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
                result.seats.Add(new SeatResult { strategy = settings.seats[seat] });
                BotProfile profile = BotProfile.For(settings.seats[seat]);
                brains[p] = new BotBrain(settings.forgetfulPlayers ? profile.Forgetful() : profile, rng);
            }

            DeckBuilder.Shuffle(players, rng);
            deck.AddRange(DeckBuilder.BuildDrawPile(settings.BuildRecipe(), rng, removedFromGame));
            if (settings.keyInTopHalf)
            {
                int key = deck.FindIndex(c => c.IsKey);
                CardData keyCard = deck[key];
                deck.RemoveAt(key);
                deck.Insert(rng.Next(deck.Count / 2, deck.Count + 1), keyCard); // the end of the list is the top
            }

            keyInPlay = deck.Find(c => c.IsKey);
            if (settings.keyStart != KeyStart.Shuffled)
            {
                deck.Remove(keyInPlay);
                if (settings.keyStart == KeyStart.Discarded) discardPile.Add(keyInPlay);
                else if (settings.keyStart == KeyStart.NeverDrawn) deck.Insert(0, keyInPlay); // the bottom of the pile
            }

            for (int round = 0; round < GameManager.StartingHandSize; round++)
                foreach (PlayerData p in players)
                    GiveCard(p, TakeTopCard());

            if (settings.keyStart == KeyStart.DealtAtStart)
            {
                // Swap it for one of a random player's dealt cards, so hand sizes stay the same
                PlayerData lucky = players[rng.Next(players.Count)];
                int slot = rng.Next(lucky.hand.Count);
                deck.Insert(rng.Next(deck.Count + 1), lucky.hand[slot]);
                lucky.hand[slot] = keyInPlay;
            }
        }

        private CardData keyInPlay;

        private string KeyFate()
        {
            if (players.Any(p => p.hand.Contains(keyInPlay))) return "Held by a player at Sundown";
            if (deck.Contains(keyInPlay)) return "Never drawn (still in the pile)";
            if (discardPile.Contains(keyInPlay)) return "Discarded";
            if (removedFromGame.Contains(keyInPlay)) return "Removed by Purge";
            return "Elsewhere";
        }

        private void PlayPhase()
        {
            hadTurn.Clear();
            stolenFromTonight.Clear();
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
            freeNightCardUsed = false;
            stoleOrSwapped = false;
            peeksThisTurn = 0;

            BotBrain brain = brains[p];
            brain.NewTurn();

            bool isDay = state == GameState.DayPhase;
            if ((isDay && p.daySkip) || (!isDay && p.nightSkip))
            {
                if (isDay) p.daySkip = false;
                else p.nightSkip = false;
                F(p).turnsSkipped++;
                return;
            }

            actionsLeft = isDay ? settings.dayActions : settings.nightActions;
            if (isDay && deck.Count > 0)
            {
                bool forced = (settings.drawRule == DrawRule.ForcedWhenLow && p.hand.Count < 3)
                           || (settings.drawRule == DrawRule.ForcedEvenDays && currentDay % 2 == 0);
                if (settings.drawRule == DrawRule.FreeEachDay)
                {
                    GiveCard(p, TakeTopCard());
                }
                else if (forced)
                {
                    GiveCard(p, TakeTopCard());
                    result.forcedDraws++;
                    CountAction(p);
                    SpendAction();
                }
            }

            // A guard against a turn that never ends; a real one ends in a handful of moves
            for (int attempt = 0; actionsLeft > 0 && attempt < 40; attempt++)
            {
                if (!HasAnyMove(p))
                {
                    F(p).turnsWithNoMove++;
                    return;
                }
                if (settings.allCardsVisible) ShowEveryBack(brain);

                bool acted = false;
                foreach (BotMove move in brain.RankMoves(this, p))
                {
                    if (move.kind == MoveKind.Pass) break;
                    if (!TryMove(p, move)) continue;

                    Count(p, move.kind);
                    TrackKey(p);
                    acted = true;
                    break;
                }
                if (!acted)
                {
                    result.stalledTurns++;
                    F(p).turnsWithNoMove++;
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

        private SeatResult F(PlayerData p) => result.seats[seatOf[p]];

        public bool CanStealFrom(PlayerData thief, PlayerData victim)
        {
            if (settings.stealRule != StealRule.AdjacentOnly) return true;
            int n = players.Count, a = players.IndexOf(thief), b = players.IndexOf(victim);
            int gap = Math.Abs(a - b);
            return gap == 1 || gap == n - 1;
        }

        public bool FrontUsed(CardData card) => usedFronts.Contains(card);
        public bool KeepsMorningCards => settings.keepMorningCards;

        /// <summary>Catch-up: the player furthest from surviving (fewest kinds, then fewest Supply) draws a free card.</summary>
        private void CatchUpDraw()
        {
            if (deck.Count == 0) return;
            int lowest = players.Min(Rank);
            var behind = players.Where(x => Rank(x) == lowest && !x.HoldsKey).ToList();
            if (behind.Count == 0) return;
            GiveCard(behind[rng.Next(behind.Count)], TakeTopCard());
            result.catchUpDraws++;
        }

        private void CountAction(PlayerData p)
        {
            F(p).actions++;
            foreach (PlayerData other in players)
                if (other != p) F(other).othersActions++;
        }

        /// <summary>Notes when the Key leaves someone's hand because of another player or an event.</summary>
        private void TrackKey(PlayerData actor)
        {
            PlayerData holder = players.FirstOrDefault(x => x.HoldsKey);
            if (keyHolder != null && holder != keyHolder && actor != keyHolder) F(keyHolder).keyTaken++;
            keyHolder = holder;
        }

        /// <summary>Real Supply and peeked cards each player has, to compare after a scramble.</summary>
        private Dictionary<PlayerData, (int supply, int peeked)> Snapshot() =>
            players.ToDictionary(x => x, x => (x.RealSupplyCount, x.hand.Count(c => c.peeked)));

        private void NoteScramble(Dictionary<PlayerData, (int supply, int peeked)> before, PlayerData playedBy, bool fromRondo)
        {
            foreach (PlayerData x in players)
            {
                if (x == playedBy) continue; // their own choice
                int lost = before[x].supply - x.RealSupplyCount;
                if (lost >= 2) F(x).handScrambled++;
                if (fromRondo && lost > 0) F(x).rondoSupplyLost += lost;
                F(x).peeksLost += before[x].peeked;
            }
        }

        private void Count(PlayerData p, MoveKind kind)
        {
            CountAction(p);
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
                return (cardsAllowed && deck.Count > 0 && settings.drawRule != DrawRule.NoDrawing)
                    || (cardsAllowed && settings.conversionCost > 0 && conversionsLeft > 0 && player.hand.Count >= settings.conversionCost)
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
            bool mustPeek = settings.peekRequired && isNight && peeksThisTurn == 0 && p.hand.Count > 0
                            && (settings.freePeeks > 0 || actionsLeft == 1);
            if (mustPeek && m.kind != MoveKind.Peek) return false;

            switch (m.kind)
            {
                case MoveKind.Scavenge:
                    if (!isDay || dayRule == DayRule.TradingOnly || deck.Count == 0 || settings.drawRule == DrawRule.NoDrawing) return false;
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
                    bool free = peeksThisTurn < settings.freePeeks;
                    peeksThisTurn++;
                    if (!free) SpendAction();
                    return true;

                case MoveKind.Steal:
                    if (!isNight || stoleOrSwapped || m.target == null || m.target == p || !ValidIndex(m.target, m.targetCardIndex)) return false;
                    if (settings.stealProtection && stolenFromTonight.Contains(m.target)) return false;
                    if (!CanStealFrom(p, m.target)) return false;
                    stolenFromTonight.Add(m.target);
                    if (settings.stealRule == StealRule.Blind) m.targetCardIndex = rng.Next(m.target.hand.Count);
                    CardData stolen = m.target.hand[m.targetCardIndex];
                    F(m.target).timesStolenFrom++;
                    if (stolen.IsRealSupply) F(m.target).supplyTaken++;
                    if (stolen.peeked) F(m.target).peeksLost++;
                    m.target.hand.RemoveAt(m.targetCardIndex);
                    GiveCard(p, stolen);
                    stolenThisTurn = stolen;
                    stoleOrSwapped = true;
                    SpendAction();
                    return true;

                case MoveKind.Convert:
                    return isDay && Convert(p, m.handIndices);

                case MoveKind.Swap:
                    if (!isNight || stoleOrSwapped || m.target == null || m.target == p || m.target.hand.Count == 0 || !ValidIndex(p, m.handIndex)) return false;
                    CardData mine = p.hand[m.handIndex];
                    p.hand.RemoveAt(m.handIndex);
                    CardData theirs = TakeRandomCard(m.target);
                    if (theirs.IsRealSupply && !mine.IsRealSupply) F(m.target).supplyTaken++;
                    if (theirs.peeked) F(m.target).peeksLost++;
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
                F(from).tradesRefused++;
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

        /// <summary>Test rule: give up conversionCost cards of your choice for 1 real Supply (which you know is real).</summary>
        private bool Convert(PlayerData p, int[] handIndices)
        {
            int cost = settings.conversionCost;
            if (dayRule == DayRule.TradingOnly || cost <= 0 || conversionsLeft <= 0) return false;
            if (handIndices == null || handIndices.Length != cost || handIndices.Distinct().Count() != cost) return false;
            if (handIndices.Any(i => !ValidIndex(p, i))) return false;

            foreach (int i in handIndices.OrderByDescending(i => i))
            {
                discardPile.Add(p.hand[i]);
                p.hand.RemoveAt(i);
            }

            var kind = (SupplyKind)rng.Next(4);
            var supply = new CardData { front = FrontType.Supply, frontSupply = kind, back = BackType.Supply, backSupply = kind };
            GiveCard(p, supply);
            supply.peeked = true;
            brains[p].Memory.Saw(supply);

            conversionsLeft--;
            result.conversions++;
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

            if (isDay && settings.keepMorningCards)
            {
                if (usedFronts.Contains(card)) return false;
                usedFronts.Add(card); // the card stays in hand, its back still counts
            }
            else
            {
                player.hand.RemoveAt(handIndex);
                discardPile.Add(card);
            }

            result.cardPlays.Add((isDay ? card.FrontName : card.BackName, seatOf[player]));

            if (isDay) ResolveMorningCard(card.morningAction, player, target);
            else ResolveNightCard(card, player, target);

            if (!isDay && settings.freeNightCard && !freeNightCardUsed) freeNightCardUsed = true;
            else SpendAction();
            return true;
        }

        private void ResolveMorningCard(MorningActionType action, PlayerData player, PlayerData target)
        {
            switch (action)
            {
                case MorningActionType.SwapDeck:
                    var beforeSwap = Snapshot();
                    (player.hand, target.hand) = (target.hand, player.hand);
                    foreach (var c in player.hand) c.peeked = false;
                    foreach (var c in target.hand) c.peeked = false;
                    NoteScramble(beforeSwap, player, false);
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
                    var beforeSwitch = Snapshot();
                    var hands = players.Select(p => p.hand).ToList();
                    for (int i = 0; i < players.Count; i++)
                    {
                        int to = (i + turnDirection + players.Count) % players.Count;
                        players[to].hand = hands[i];
                        foreach (var c in hands[i]) c.peeked = false;
                    }
                    NoteScramble(beforeSwitch, player, false);
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
            var options = ((RondoResult[])Enum.GetValues(typeof(RondoResult)))
                .Where(r => !rondoUsed.Contains(r))
                .Where(r => !(settings.noDoomsdayOnLastDay && r == RondoResult.DoomsdayClock && currentDay >= sundownDay))
                .ToList();
            if (options.Count == 0) return;

            RondoResult spun = options[rng.Next(options.Count)];
            rondoUsed.Add(spun);
            result.rondo.Add(spun);

            switch (spun)
            {
                case RondoResult.Purge:
                    int purge = settings.strongRondo ? 10 : GameManager.PurgeCount;
                    for (int i = 0; i < purge && deck.Count > 0; i++) removedFromGame.Add(TakeTopCard());
                    break;
                case RondoResult.TradingOnly:
                    dayRule = DayRule.TradingOnly;
                    break;
                case RondoResult.HungerDisaster:
                    for (int round = settings.strongRondo ? 2 : 1; round > 0; round--)
                        LoseOneCardEach(c => c.IsRealSupply && c.backSupply == SupplyKind.Food);
                    break;
                case RondoResult.Tornado:
                    var beforeTornado = Snapshot();
                    var pool = players.SelectMany(p => p.hand).ToList();
                    var sizes = players.Select(p => p.hand.Count).ToList();
                    DeckBuilder.Shuffle(pool, rng);
                    int next = 0;
                    for (int i = 0; i < players.Count; i++)
                    {
                        players[i].hand = new List<CardData>();
                        for (int n = 0; n < sizes[i]; n++) GiveCard(players[i], pool[next++]);
                    }
                    NoteScramble(beforeTornado, null, true);
                    break;
                case RondoResult.DoomsdayClock:
                    sundownDay = Math.Max(currentDay, sundownDay - (settings.strongRondo ? 2 : 1));
                    break;
                case RondoResult.Blackout:
                    dayRule = DayRule.Blackout;
                    break;
                case RondoResult.Outbreak:
                    for (int round = settings.strongRondo ? 2 : 1; round > 0; round--)
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
                F(p).rondoSupplyLost++;
                if (card.peeked) F(p).peeksLost++;
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

            int seatsLeft = settings.SeatsFor(players.Count);

            PlayerData keyHolder = players.FirstOrDefault(p => p.HoldsKey);
            if (keyHolder != null)
            {
                keyHolder.isSeated = true;
                keyHolder.resultReason = "Key";
                seatsLeft--;
            }

            var qualifiers = players
                .Where(p => !p.isSeated && Qualifying(p) >= GameManager.SupplyToSurvive)
                .OrderByDescending(Rank)
                .ToList();

            int index = 0;
            while (seatsLeft > 0 && index < qualifiers.Count)
            {
                int supply = Rank(qualifiers[index]);
                var tied = qualifiers.Skip(index).TakeWhile(p => Rank(p) == supply).ToList();

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
