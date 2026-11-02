using System.Collections.Generic;
using System.Linq;
using TDTS.Bots;

namespace TDTS.Simulation
{
    /// <summary>Totals across every game in a Balance Simulator run.</summary>
    public class SimStats
    {
        public class StrategyRow
        {
            public BotStrategy strategy;
            public int seats;          // seat-games played with this strategy
            public int survived;
            public int survivedByKey;
            public int survivedBySupply;
            public int totalRealSupply;

            public float SurvivalRate => seats == 0 ? 0f : (float)survived / seats;
            public float AvgRealSupply => seats == 0 ? 0f : (float)totalRealSupply / seats;
        }

        public class CardRow
        {
            public string card;
            public int plays;
            public int playsBySurvivors; // plays by a player who survived that game
            public int gamesUsed;

            public float WinRateWhenPlayed => plays == 0 ? 0f : (float)playsBySurvivors / plays;
        }

        public int games;
        public int survivors;
        public int gamesNobodySurvived;
        public int keyHolderAtSundown;
        public int tradesOffered;
        public int tradesAccepted;
        public int stalledTurns;
        public int shortenedGames; // Doomsday Clock brought Sundown forward
        public double seconds;

        public readonly Dictionary<BotStrategy, StrategyRow> strategies = new Dictionary<BotStrategy, StrategyRow>();
        public readonly Dictionary<string, CardRow> cards = new Dictionary<string, CardRow>();
        public readonly Dictionary<BotStrategy, Dictionary<MoveKind, int>> moves = new Dictionary<BotStrategy, Dictionary<MoveKind, int>>();

        public float AvgSurvivors => games == 0 ? 0f : (float)survivors / games;

        public void Add(SimGameResult game)
        {
            games++;
            tradesOffered += game.tradesOffered;
            tradesAccepted += game.tradesAccepted;
            stalledTurns += game.stalledTurns;
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
                if (s.survived)
                {
                    row.survived++;
                    if (s.reason == "Key") row.survivedByKey++;
                    else row.survivedBySupply++;
                }

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
            }
        }

        private CardRow CardRowFor(string card)
        {
            if (!cards.TryGetValue(card, out CardRow row)) cards[card] = row = new CardRow { card = card };
            return row;
        }

        public IEnumerable<StrategyRow> StrategiesBestFirst => strategies.Values.OrderByDescending(r => r.SurvivalRate);
        public IEnumerable<CardRow> CardsMostPlayed => cards.Values.OrderByDescending(r => r.plays);
    }
}
