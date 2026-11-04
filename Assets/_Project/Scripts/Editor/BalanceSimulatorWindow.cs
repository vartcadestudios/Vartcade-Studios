using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TDTS.Bots;
using TDTS.Simulation;
using Debug = UnityEngine.Debug;

namespace TDTS.EditorTools
{
    /// <summary>
    /// TDTS > Balance Simulator: plays hundreds of bot-only games with no scene and
    /// reports how each strategy did, so rule changes can be tested in seconds.
    /// </summary>
    public class BalanceSimulatorWindow : EditorWindow
    {
        [SerializeField] private SimSettings settings = new SimSettings();

        private SimSettings runSettings;
        private SimStats stats;
        private bool running;
        private int done;
        private int seedBase;
        private Stopwatch watch;
        private Vector2 scroll;
        private int tab = 1;

        [MenuItem("TDTS/Balance Simulator")]
        private static void Open() => GetWindow<BalanceSimulatorWindow>("Balance Simulator").minSize = new Vector2(760, 560);

        private void OnDisable() => Stop();

        // ---------- Window ----------

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Ten Days to Sundown · Balance Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Plays whole games with bots only, no scene, and adds up how each strategy did. " +
                                    "Change a rule below and run again to compare.", MessageType.None);

            using (new EditorGUI.DisabledScope(running))
            {
                DrawSeats();
                DrawRules();
                DrawRun();
            }

            DrawProgress();
            DrawResults();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSeats()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);

            int count = EditorGUILayout.IntSlider("Number of players", settings.seats.Length, 4, 6);
            if (count != settings.seats.Length)
            {
                var resized = new BotStrategy[count];
                for (int i = 0; i < count; i++)
                    resized[i] = i < settings.seats.Length ? settings.seats[i] : BotStrategy.HumanLike;
                settings.seats = resized;
            }

            for (int i = 0; i < settings.seats.Length; i++)
                settings.seats[i] = (BotStrategy)EditorGUILayout.EnumPopup($"Seat {i + 1} strategy", settings.seats[i]);
        }

        private static readonly string[] MorningNames = { "Swap Deck", "Reverse Turn", "Skip Next Player", "Everyone +1 Card", "Everyone Switches" };
        private static readonly string[] NightNames = { "Flashlight", "Targeted Night Skip", "Targeted Day Skip" };

        private void DrawDeck()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Action cards ({settings.morningFronts.Sum()} Morning fronts, {settings.nightBacks.Sum()} Night backs)", EditorStyles.boldLabel);
            for (int i = 0; i < MorningNames.Length; i++)
                settings.morningFronts[i] = Mathf.Clamp(EditorGUILayout.IntField($"  {MorningNames[i]} (front)", settings.morningFronts[i]), 0, 20);
            for (int i = 0; i < NightNames.Length; i++)
                settings.nightBacks[i] = Mathf.Clamp(EditorGUILayout.IntField($"  {NightNames[i]} (back)", settings.nightBacks[i]), 0, 20);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Game deck: 20 + 20"))
                {
                    settings.morningFronts = new[] { 4, 4, 4, 4, 4 };
                    settings.nightBacks = new[] { 8, 6, 6 };
                }
                if (GUILayout.Button("Old deck: 18 + 18"))
                {
                    settings.morningFronts = new[] { 4, 4, 4, 3, 3 };
                    settings.nightBacks = new[] { 6, 6, 6 };
                }
            }
        }

        private void DrawRules()
        {
            DrawDeck();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rules to test", EditorStyles.boldLabel);

            settings.dayActions = EditorGUILayout.IntSlider("Actions per Day turn", settings.dayActions, 1, 3);
            settings.nightActions = EditorGUILayout.IntSlider("Actions per Night turn", settings.nightActions, 1, 4);

            settings.supplyBacks = EditorGUILayout.IntSlider(
                new GUIContent("Real Supply backs", $"Of the {settings.NonNightBacks} cards without a Night back. The rest are Trash."),
                Mathf.Min(settings.supplyBacks, settings.NonNightBacks), 0, settings.NonNightBacks);
            int trash = settings.NonNightBacks - settings.supplyBacks;
            EditorGUILayout.LabelField(" ", $"{settings.supplyBacks} real Supply, {trash} Trash ({100f * settings.supplyBacks / Mathf.Max(1, settings.NonNightBacks):0}% real)",
                EditorStyles.miniLabel);

            settings.allCardsVisible = EditorGUILayout.Toggle(
                new GUIContent("All cards visible", "Every bot knows every back, as if nothing were hidden."), settings.allCardsVisible);
            settings.forgetfulPlayers = EditorGUILayout.Toggle(
                new GUIContent("Forgetful players", "Casual players: nobody follows cards once they leave their hand, and everyone " +
                                                    "forgets what they peeked (their own hand too) about a third of the time each Day."),
                settings.forgetfulPlayers);

            settings.drawRule = (DrawRule)EditorGUILayout.EnumPopup(
                new GUIContent("Draw rule", "Choice: Scavenge is a normal Day action. FreeEachDay: a free card every Day turn. " +
                                            "NoDrawing: no Scavenge at all. ForcedWhenLow: under 3 cards, your Day turn starts with a draw " +
                                            "that uses an action. ForcedEvenDays: the same on Days 2, 4, 6, 8 and 10."),
                settings.drawRule);

            settings.conversionCost = EditorGUILayout.IntSlider(
                new GUIContent("Convert cards for Supply", "Day action: give up this many cards for 1 real Supply. 0 = off."),
                settings.conversionCost, 0, 4);
            if (settings.conversionCost > 0)
                settings.conversionSupply = EditorGUILayout.IntSlider(
                    new GUIContent("  Supply available to convert", "How many real Supply cards can be converted into in one game."),
                    settings.conversionSupply, 1, 30);

            settings.peekRequired = EditorGUILayout.Toggle(
                new GUIContent("Peek required each Night", "R4. With 1 Night action, the required peek uses the whole turn, " +
                                                            "so nobody can Steal, Swap or play a Night card."),
                settings.peekRequired);

            settings.freePeeks = EditorGUILayout.IntSlider(
                new GUIContent("Free peeks each Night", "Peeks that don't use a Night action. 0 = every peek uses an action (the game). " +
                                                        "5 or more means effectively all peeks are free."),
                settings.freePeeks, 0, 5);

            settings.stealProtection = EditorGUILayout.Toggle(
                new GUIContent("Steal protection", "Once you've been stolen from, nobody can steal from you again that Night."),
                settings.stealProtection);

            settings.stealRule = (StealRule)EditorGUILayout.EnumPopup(
                new GUIContent("Steal rule", "PickCard: choose which card to take (the game). Blind: take one at random. " +
                                             "AdjacentOnly: only from a player sitting next to you."),
                settings.stealRule);
            settings.keepMorningCards = EditorGUILayout.Toggle(
                new GUIContent("Keep played Morning cards", "The card stays in your hand so its back still counts; each front works once."),
                settings.keepMorningCards);
            settings.freeNightCard = EditorGUILayout.Toggle(
                new GUIContent("First Night card is free", "Your first Night card each Night doesn't use an action."),
                settings.freeNightCard);
            settings.catchUpDraw = EditorGUILayout.Toggle(
                new GUIContent("Catch-up draw", "From Day 2, the player furthest from surviving draws a free card each Day."),
                settings.catchUpDraw);
            settings.keyInTopHalf = EditorGUILayout.Toggle(
                new GUIContent("Key in the top half", "The Bunker Key is shuffled into the top half of the draw pile."),
                settings.keyInTopHalf);
            settings.bunkerSeats = EditorGUILayout.IntSlider(
                new GUIContent("Bunker seats", "0 = like the game: 2 seats, 3 with 5 or 6 players."), settings.bunkerSeats, 0, 4);

            settings.rondo = EditorGUILayout.Toggle(new GUIContent("Rondo"), settings.rondo);
            if (settings.rondo)
                settings.strongRondo = EditorGUILayout.Toggle(
                    new GUIContent("  Stronger Rondo", "Starts on Day 4. Purge takes 10 cards, Hunger and Outbreak take 2 cards each, " +
                                                       "Doomsday Clock takes 2 days."),
                    settings.strongRondo);
            if (settings.rondo)
                settings.noDoomsdayOnLastDay = EditorGUILayout.Toggle(
                    new GUIContent("  No Doomsday on the last Day", "Doomsday Clock can't come up when it would do nothing."),
                    settings.noDoomsdayOnLastDay);
        }

        private void DrawRun()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Run", EditorStyles.boldLabel);
            settings.games = Mathf.Clamp(EditorGUILayout.IntField("Games", settings.games), 1, 20000);
            settings.seed = EditorGUILayout.IntField(new GUIContent("Seed", "0 = different games every run. Any other number repeats the same games."), settings.seed);

            if (GUILayout.Button($"Run {settings.games} games", GUILayout.Height(30))) Run();
        }

        private void DrawProgress()
        {
            if (!running && stats == null) return;
            EditorGUILayout.Space();

            int total = runSettings?.games ?? 1;
            Rect bar = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.ProgressBar(bar, (float)done / total, running ? $"Playing... {done} / {total} games" : $"Done: {done} games in {stats.seconds:0.0}s");

            if (running && GUILayout.Button("Stop")) Stop();
        }

        private static readonly string[] Tabs = { "Overview", "Strategies", "Supply", "Moves", "Cards", "Rondo", "Turn order", "Frustration" };

        private void DrawResults()
        {
            if (stats == null || stats.games == 0) return;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(running))
                    if (GUILayout.Button("Export CSV...", GUILayout.Width(110))) ExportCsv();
            }

            tab = GUILayout.Toolbar(tab, Tabs);
            EditorGUILayout.Space(4);

            switch (tab)
            {
                case 0:
                    Explain("The whole run at a glance. The bunker has 2 seats (3 with 5 or 6 players), so with 4 players the fair share is 50% each.");
                    DrawTable(stats.OverviewHeader, stats.OverviewRows(runSettings), 250);
                    break;
                case 1:
                    Explain("How often each strategy got a bunker seat, how, and why it lost when it didn't. " +
                            "'vs fair share' is how far above or below an equal split it did.");
                    DrawTable(SimStats.StrategyHeader, stats.StrategyRows(), 90);
                    break;
                case 2:
                    Explain("How many real Supply cards each strategy held at Sundown (share of its games). " +
                            "Without the Key you need 3 or more to qualify for a seat.");
                    DrawTable(SimStats.SupplyHeader, stats.SupplyRows(), 90);
                    break;
                case 3:
                    Explain("Average moves per game for one seat playing that strategy.");
                    DrawTable(SimStats.MovesHeader, stats.MoveRows(), 90);
                    break;
                case 4:
                    Explain("Every action card played. 'Win rate when played' is how often the player who played it survived that game.");
                    DrawTable(SimStats.CardHeader, stats.CardRows(), 140);
                    break;
                case 5:
                    Explain("Rondo results from Day 6, most deadly first. Compare 'survivors per game' with the Overview average.");
                    DrawTable(SimStats.RondoHeader, stats.RondoRows(), 140);
                    break;
                case 6:
                    Explain("Survival by seat in the starting turn order. In a fair game every position is close to the fair share.");
                    DrawTable(SimStats.PositionHeader, stats.PositionRows(), 160);
                    break;
                case 7:
                    Explain("Moments players tend to find frustrating, per player per game, and the share of players it happened to " +
                            "at least once. Lower is better; zero isn't the goal, since some setbacks make a comeback exciting.");
                    DrawTable(SimStats.FrustrationHeader, stats.FrustrationRows(), 200);
                    break;
            }
        }

        private static void Explain(string text) => EditorGUILayout.HelpBox(text, MessageType.None);

        /// <summary>A simple grid: bold header, alternating row shading, first column wider.</summary>
        private static void DrawTable(string[] header, System.Collections.Generic.List<string[]> rows, float firstColumn)
        {
            DrawRow(header, firstColumn, true, false);
            for (int i = 0; i < rows.Count; i++) DrawRow(rows[i], firstColumn, false, i % 2 == 1);
        }

        private static void DrawRow(string[] cells, float firstColumn, bool header, bool shaded)
        {
            Rect rect = EditorGUILayout.BeginHorizontal();
            if (shaded) EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.12f));
            if (header) EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.25f));

            GUIStyle style = header ? EditorStyles.miniBoldLabel : EditorStyles.label;
            EditorGUILayout.LabelField(cells[0], style, GUILayout.Width(firstColumn));
            for (int i = 1; i < cells.Length; i++)
                EditorGUILayout.LabelField(cells[i], style, GUILayout.MinWidth(55));
            EditorGUILayout.EndHorizontal();
        }

        private void ExportCsv()
        {
            string path = EditorUtility.SaveFilePanel("Export simulator results", "", $"balance-{DateTime.Now:yyyy-MM-dd-HHmm}.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;
            System.IO.File.WriteAllText(path, stats.ToCsv(runSettings));
            EditorUtility.RevealInFinder(path);
        }

        // ---------- Running ----------

        private void Run()
        {
            runSettings = JsonUtility.FromJson<SimSettings>(JsonUtility.ToJson(settings)); // edits during a run don't change it
            stats = new SimStats();
            done = 0;
            seedBase = settings.seed != 0 ? settings.seed : Environment.TickCount;
            watch = Stopwatch.StartNew();
            running = true;
            EditorApplication.update += Step;
        }

        /// <summary>Plays games in short bursts so the window stays responsive and the bar moves.</summary>
        private void Step()
        {
            var burst = Stopwatch.StartNew();
            try
            {
                while (done < runSettings.games && burst.ElapsedMilliseconds < 40)
                {
                    stats.Add(new SimGame(runSettings, seedBase + done).Run());
                    done++;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"[Balance Simulator] Game {done + 1} (seed {seedBase + done}) failed; stopped.");
                Stop();
            }

            stats.seconds = watch.Elapsed.TotalSeconds;
            if (done >= runSettings.games) Stop();
            Repaint();
        }

        private void Stop()
        {
            if (!running) return;
            running = false;
            EditorApplication.update -= Step;
            if (stats != null && watch != null) stats.seconds = watch.Elapsed.TotalSeconds;
            Repaint();
        }
    }
}
