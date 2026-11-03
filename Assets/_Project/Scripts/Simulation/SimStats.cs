using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDTS.Bots;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.Simulation
{
    /// <summary>Totals across every game in a Balance Simulator run, plus the tables built from them.</summary>
    public class SimStats
    {
        public class StrategyRow
        {
            public BotStrategy strategy;
            public int seats;          // seat-games played with this strategy
            public int survived;
            public int survivedByKey;
            public int survivedBySupply;
            public int lostTied;       // 3+ Supply but tied for the last seat
            public int lostFull;       // 3+ Supply but the bunker was already full
            public int lostShort;      // fewer than 3 real Supply and no Key
            public int totalRealSupply;
            public readonly int[] supplyHistogram = new int[6]; // 0, 1, 2, 3, 4, 5+ real Supply at Sundown

            public float SurvivalRate => seats == 0 ? 0f : (float)survived / seats;
            public float AvgRealSupply => seats == 0 ? 0f : (float)totalRealSupply / seats;
        }

        public class CardRow
        {
            public string card;
            public bool morning;
            public int plays;
            public int playsBySurvivors; // plays by a player who survived that game
            public int gamesUsed;
            public readonly Dictionary<BotStrategy, int> playsBy = new Dictionary<BotStrategy, int>();

            public float WinRateWhenPlayed => plays == 0 ? 0f : (float)playsBySurvivors / plays;
            public BotStrategy TopUser => playsBy.OrderByDescending(p => p.Value).First().Key;
        }

        public class RondoRow
        {
            public RondoResult result;
            public int games;
            public int survivors; // survivors in the games where it came up

            public float AvgSurvivors => games == 0 ? 0f : (float)survivors / games;
        }

        public int games;
        public int players;
        public int survivors;
        public int gamesNobodySurvived;
        public int keyHolderAtSundown;
        public int tradesOffered;
        public int tradesAccepted;
        public int stalledTurns;
        public int shortenedGames; // Doomsday Clock brought Sundown forward
        public int conversions;
        public int forcedDraws;
        public readonly int[] positionSeats = new int[6];    // by starting turn position
        public readonly int[] positionSurvived = new int[6];
        public double seconds;

        public readonly Dictionary<BotStrategy, StrategyRow> strategies = new Dictionary<BotStrategy, StrategyRow>();
        public readonly Dictionary<string, CardRow> cards = new Dictionary<string, CardRow>();
        public readonly Dictionary<BotStrategy, Dictionary<MoveKind, int>> moves = new Dictionary<BotStrategy, Dictionary<MoveKind, int>>();
        public readonly Dictionary<RondoResult, RondoRow> rondo = new Dictionary<RondoResult, RondoRow>();

        public float AvgSurvivors => games == 0 ? 0f : (float)survivors / games;
        /// <summary>What every strategy would get if all played equally well: 2 seats shared by everyone.</summary>
        public float FairShare => players == 0 ? 0f : Math.Min(1f, 2f / players);

        private static readonly HashSet<string> MorningNames =
            new HashSet<string>(((MorningActionType[])Enum.GetValues(typeof(MorningActionType))).Select(CardData.NameOf));

        public void Add(SimGameResult game)
        {
            games++;
            players = game.seats.Count;
            tradesOffered += game.tradesOffered;
            tradesAccepted += game.tradesAccepted;
            stalledTurns += game.stalledTurns;
            conversions += game.conversions;
            forcedDraws += game.forcedDraws;
            if (game.sundownDay < 10) shortenedGames++;

            int alive = game.seats.Count(s => s.survived);
            survivors += alive;
            if (alive == 0) gamesNobodySurvived++;
            if (game.seats.Any(s => s.heldKey)) keyHolderAtSundown++;

            for (int seat = 0; seat < game.seats.Count; seat++)
            {
                SeatResult s = game.seats[seat];
                if (!strategies.TryGetValue(s.strategy, out StrategyRow row))
                    strategies[s.strategy] = row = new StrategyRow { strategy = s.strategy };

                row.seats++;
                row.totalRealSupply += s.realSupply;
                positionSeats[s.turnPosition]++;
                if (s.survived) positionSurvived[s.turnPosition]++;
                row.supplyHistogram[Math.Min(5, s.realSupply)]++;
                if (s.survived)
                {
                    row.survived++;
                    if (s.reason == "Key") row.survivedByKey++;
                    else row.survivedBySupply++;
                }
                else if (s.reason == "tied for the last seat") row.lostTied++;
                else if (s.reason == "bunker full") row.lostFull++;
                else row.lostShort++;

                if (!moves.TryGetValue(s.strategy, out var counts))
                    moves[s.strategy] = counts = new Dictionary<MoveKind, int>();
                foreach (var pair in game.movesBySeat[seat])
                    counts[pair.Key] = (counts.TryGetValue(pair.Key, out int n) ? n : 0) + pair.Value;
            }

            foreach (string card in game.cardPlays.Select(p => p.card).Distinct())
                CardRowFor(card).gamesUsed++;

            foreach ((string card, int seat) in game.cardPlays)
            {
                CardRow row = CardRowFor(card);
                row.plays++;
                if (game.seats[seat].survived) row.playsBySurvivors++;
                BotStrategy who = game.seats[seat].strategy;
                row.playsBy[who] = (row.playsBy.TryGetValue(who, out int n) ? n : 0) + 1;
            }

            foreach (RondoResult spun in game.rondo)
            {
                if (!rondo.TryGetValue(spun, out RondoRow row)) rondo[spun] = row = new RondoRow { result = spun };
                row.games++;
                row.survivors += alive;
            }
        }

        private CardRow CardRowFor(string card)
        {
            if (!cards.TryGetValue(card, out CardRow row))
                cards[card] = row = new CardRow { card = card, morning = MorningNames.Contains(card) };
            return row;
        }

        public IEnumerable<StrategyRow> StrategiesBestFirst => strategies.Values.OrderByDescending(r => r.SurvivalRate);
        public IEnumerable<CardRow> CardsMostPlayed => cards.Values.OrderByDescending(r => r.plays);
        public IEnumerable<RondoRow> RondoMostDeadly => rondo.Values.OrderBy(r => r.AvgSurvivors);

        /// <summary>Average uses of a move per game, for one seat playing this strategy.</summary>
        public float MovesPerGame(BotStrategy strategy, MoveKind kind)
        {
            if (!strategies.TryGetValue(strategy, out StrategyRow row) || row.seats == 0) return 0f;
            return moves.TryGetValue(strategy, out var counts) && counts.TryGetValue(kind, out int n) ? (float)n / row.seats : 0f;
        }

        public static readonly MoveKind[] MoveColumns =
            { MoveKind.Scavenge, MoveKind.Trade, MoveKind.PlayCard, MoveKind.Peek, MoveKind.Steal, MoveKind.Swap, MoveKind.Convert };

        // ---------- Tables (shared by the window and the CSV export) ----------

        public string[] OverviewHeader => new[] { "Measure", "Value" };

        public List<string[]> OverviewRows(SimSettings s) => new List<string[]>
        {
            new[] { "Games played", $"{games}" },
            new[] { "Players per game", $"{players}" },
            new[] { "Rules", $"{s.dayActions} Day / {s.nightActions} Night actions, {s.supplyBacks} Supply / {SimSettings.NonNightBacks - s.supplyBacks} Trash backs, " +
                             $"draw rule {s.drawRule}{(s.conversionCost > 0 ? $", convert {s.conversionCost} cards for 1 Supply" : "")}" +
                             $"{(s.allCardsVisible ? ", all cards visible" : "")}{(!s.rondo ? ", no Rondo" : s.strongRondo ? ", strong Rondo" : "")}" +
                             $"{(s.forgetfulPlayers ? ", forgetful players" : "")}" },
            new[] { "Survivors per game", $"{AvgSurvivors:0.00} of 2 seats" },
            new[] { "Fair share per player", Pct(FairShare) },
            new[] { "Games where nobody survived", Pct(gamesNobodySurvived, games) },
            new[] { "Someone held the Key at Sundown", Pct(keyHolderAtSundown, games) },
            new[] { "Sundown came early (Doomsday Clock)", Pct(shortenedGames, games) },
            new[] { "Trades offered per game", $"{(games == 0 ? 0 : (float)tradesOffered / games):0.0}" },
            new[] { "Trades accepted", Pct(tradesAccepted, tradesOffered) },
            new[] { "Conversions per game", $"{(games == 0 ? 0 : (float)conversions / games):0.0}" },
            new[] { "Forced draws per game", $"{(games == 0 ? 0 : (float)forcedDraws / games):0.0}" },
            new[] { "First vs last player survival", $"{Pct(PositionRate(0))} vs {Pct(PositionRate(players - 1))}" },
            new[] { "Turns that ended early (no move worked)", $"{stalledTurns}" },
            new[] { "Time", $"{seconds:0.0}s" },
        };

        public static readonly string[] StrategyHeader =
            { "Strategy", "Survived", "vs fair share", "via Key", "via Supply", "Lost: <3 Supply", "Lost: tie", "Lost: bunker full", "Avg real Supply" };

        public List<string[]> StrategyRows() => StrategiesBestFirst.Select(r => new[]
        {
            BotProfile.For(r.strategy).Label,
            Pct(r.survived, r.seats),
            $"{(r.SurvivalRate - FairShare) * 100f:+0.0;-0.0} pts",
            $"{r.survivedByKey}", $"{r.survivedBySupply}",
            $"{r.lostShort}", $"{r.lostTied}", $"{r.lostFull}",
            $"{r.AvgRealSupply:0.0}",
        }).ToList();

        public static readonly string[] SupplyHeader = { "Strategy", "0", "1", "2", "3", "4", "5+", "Reached 3+" };

        public List<string[]> SupplyRows() => StrategiesBestFirst.Select(r =>
        {
            var cells = new List<string> { BotProfile.For(r.strategy).Label };
            cells.AddRange(r.supplyHistogram.Select(n => Pct(n, r.seats, false)));
            cells.Add(Pct(r.supplyHistogram.Skip(3).Sum(), r.seats, false));
            return cells.ToArray();
        }).ToList();

        public static readonly string[] MovesHeader = { "Strategy", "Scavenge", "Trade", "Play card", "Peek", "Steal", "Swap", "Convert" };

        public List<string[]> MoveRows() => StrategiesBestFirst.Select(r =>
        {
            var cells = new List<string> { BotProfile.For(r.strategy).Label };
            cells.AddRange(MoveColumns.Select(k => $"{MovesPerGame(r.strategy, k):0.0}"));
            return cells.ToArray();
        }).ToList();

        public static readonly string[] CardHeader = { "Card", "Type", "Times played", "Per game", "Used in % of games", "Win rate when played", "Played most by" };

        public List<string[]> CardRows() => CardsMostPlayed.Select(r => new[]
        {
            r.card, r.morning ? "Morning" : "Night",
            $"{r.plays}", $"{(games == 0 ? 0 : (float)r.plays / games):0.00}",
            Pct(r.gamesUsed, games, false), Pct(r.WinRateWhenPlayed),
            BotProfile.For(r.TopUser).Label,
        }).ToList();

        public float PositionRate(int position) =>
            position < 0 || position >= positionSeats.Length || positionSeats[position] == 0 ? 0f : (float)positionSurvived[position] / positionSeats[position];

        public static readonly string[] PositionHeader = { "Turn position", "Survived", "vs fair share" };

        public List<string[]> PositionRows() => Enumerable.Range(0, players).Select(i => new[]
        {
            i == 0 ? "1st (starts each phase)" : i == players - 1 ? $"{i + 1}th (last)" : $"{i + 1}{(i == 1 ? "nd" : i == 2 ? "rd" : "th")}",
            Pct(positionSurvived[i], positionSeats[i]),
            $"{(PositionRate(i) - FairShare) * 100f:+0.0;-0.0} pts",
        }).ToList();

        public static readonly string[] RondoHeader = { "Rondo result", "Came up in % of games", "Survivors per game when it did" };

        public List<string[]> RondoRows() => RondoMostDeadly.Select(r => new[]
        {
            RondoName(r.result), Pct(r.games, games, false), $"{r.AvgSurvivors:0.00}",
        }).ToList();

        private static string RondoName(RondoResult result) => result switch
        {
            RondoResult.TradingOnly => "Trading Only",
            RondoResult.HungerDisaster => "Hunger Disaster",
            RondoResult.DoomsdayClock => "Doomsday Clock",
            RondoResult.PeacefulDay => "Peaceful Day",
            _ => result.ToString(),
        };

        // ---------- CSV ----------

        public string ToCsv(SimSettings settings)
        {
            var sb = new StringBuilder();
            void Section(string title, string[] header, List<string[]> rows)
            {
                sb.AppendLine(Csv(title));
                sb.AppendLine(string.Join(",", header.Select(Csv)));
                foreach (string[] row in rows) sb.AppendLine(string.Join(",", row.Select(Csv)));
                sb.AppendLine();
            }

            Section("Overview", OverviewHeader, OverviewRows(settings));
            Section("Survival by strategy", StrategyHeader, StrategyRows());
            Section("Real Supply at Sundown (share of games)", SupplyHeader, SupplyRows());
            Section("Moves per game (one seat)", MovesHeader, MoveRows());
            Section("Action cards", CardHeader, CardRows());
            Section("Rondo", RondoHeader, RondoRows());
            Section("Survival by turn position", PositionHeader, PositionRows());
            return sb.ToString();
        }

        private static string Csv(string cell) =>
            cell.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? $"\"{cell.Replace("\"", "\"\"")}\"" : cell;

        public static string Pct(float rate) => $"{rate * 100f:0.0}%";

        public static string Pct(int part, int whole, bool withCounts = true) =>
            whole == 0 ? "-" : withCounts ? $"{100f * part / whole:0.0}% ({part}/{whole})" : $"{100f * part / whole:0.0}%";
    }
}
