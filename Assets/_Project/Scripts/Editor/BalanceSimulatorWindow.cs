using System;
using System.Diagnostics;
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

        [MenuItem("TDTS/Balance Simulator")]
        private static void Open() => GetWindow<BalanceSimulatorWindow>("Balance Simulator").minSize = new Vector2(420, 520);

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

        private void DrawRules()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rules to test", EditorStyles.boldLabel);

            settings.dayActions = EditorGUILayout.IntSlider("Actions per Day turn", settings.dayActions, 1, 3);
            settings.nightActions = EditorGUILayout.IntSlider("Actions per Night turn", settings.nightActions, 1, 4);

            settings.supplyBacks = EditorGUILayout.IntSlider(
                new GUIContent("Real Supply backs", $"Of the {SimSettings.NonNightBacks} cards without a Night back. The rest are Trash."),
                settings.supplyBacks, 0, SimSettings.NonNightBacks);
            int trash = SimSettings.NonNightBacks - settings.supplyBacks;
            EditorGUILayout.LabelField(" ", $"{settings.supplyBacks} real Supply, {trash} Trash ({100f * settings.supplyBacks / SimSettings.NonNightBacks:0}% real)",
                EditorStyles.miniLabel);

            settings.allCardsVisible = EditorGUILayout.Toggle(
                new GUIContent("All cards visible", "Every bot knows every back, as if nothing were hidden."), settings.allCardsVisible);
            settings.alwaysDraw = EditorGUILayout.Toggle(
                new GUIContent("Always draw a card", "Each player gets a free card at the start of every Day turn."), settings.alwaysDraw);
            settings.rondo = EditorGUILayout.Toggle(new GUIContent("Rondo from Day 6"), settings.rondo);
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

        private void DrawResults()
        {
            if (stats == null || stats.games == 0) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Survivors per game", $"{stats.AvgSurvivors:0.00} of {runSettings.seats.Length} (the bunker holds 2)");
            EditorGUILayout.LabelField("Games where nobody survived", Percent(stats.gamesNobodySurvived, stats.games));
            EditorGUILayout.LabelField("Someone held the Key at Sundown", Percent(stats.keyHolderAtSundown, stats.games));
            EditorGUILayout.LabelField("Trades accepted", Percent(stats.tradesAccepted, stats.tradesOffered));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Survival by strategy", EditorStyles.boldLabel);
            Row(true, "Strategy", "Survived", "via Key", "via Supply", "Avg real Supply");
            foreach (SimStats.StrategyRow row in stats.StrategiesBestFirst)
            {
                Row(false, BotProfile.For(row.strategy).Label,
                    Percent(row.survived, row.seats),
                    row.survivedByKey.ToString(),
                    row.survivedBySupply.ToString(),
                    row.AvgRealSupply.ToString("0.0"));
            }

            if (stats.stalledTurns > 0)
                EditorGUILayout.HelpBox($"{stats.stalledTurns} turns ended early because no move went through.", MessageType.Info);
        }

        private static void Row(bool header, params string[] cells)
        {
            GUIStyle style = header ? EditorStyles.boldLabel : EditorStyles.label;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(cells[0], style, GUILayout.Width(110));
                for (int i = 1; i < cells.Length; i++)
                    EditorGUILayout.LabelField(cells[i], style, GUILayout.MinWidth(60));
            }
        }

        private static string Percent(int part, int whole) =>
            whole == 0 ? "-" : $"{100f * part / whole:0.0}%  ({part}/{whole})";

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
