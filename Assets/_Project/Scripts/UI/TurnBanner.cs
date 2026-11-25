using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// Pops "YOUR TURN" up in the middle of the screen when a turn of yours starts, then fades it out.
    /// Waits for the Rondo to finish spinning and announcing first, so the two never overlap.
    /// It never blocks taps, so you can start playing while it's still fading.
    /// HUDController adds this at the start of a game.
    /// </summary>
    public class TurnBanner : MonoBehaviour
    {
        private const float FadeInSeconds = 0.16f, ShowSeconds = 1.2f, FadeOutSeconds = 0.4f;

        private IGameView view;
        private int shownForTurn = -1;
        private Coroutine showing;
        private RectTransform banner, card;

        private void Start()
        {
            view = GameView.Current;
            if (view == null) return;
            view.OnTurnChanged += HandleTurnChanged;
            HandleTurnChanged(); // your turn may already have started
        }

        private void OnDestroy()
        {
            if (view != null) view.OnTurnChanged -= HandleTurnChanged;
        }

        private void HandleTurnChanged()
        {
            if (!view.IsLocalTurn || view.TurnNumber == shownForTurn) return;
            shownForTurn = view.TurnNumber;

            if (showing != null) StopCoroutine(showing);
            if (banner != null) Destroy(banner.gameObject);
            showing = StartCoroutine(Show(view.TurnNumber));
        }

        private IEnumerator Show(int turn)
        {
            // Let a Rondo spin start, then wait for it and its banner to finish
            yield return new WaitForSeconds(0.1f);
            RondoDial rondo = FindFirstObjectByType<RondoDial>();
            while ((rondo != null && rondo.IsAnnouncing) || GameObject.Find("RondoBanner") != null || GameObject.Find("MorningRecap") != null || GameObject.Find("PhaseCalendar") != null)
                yield return null;

            // A skipped turn, or one that's already over, gets no popup
            if (!view.IsLocalTurn || view.TurnNumber != turn) yield break;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) yield break;
            Build(canvas.rootCanvas.transform);

            var fade = banner.GetComponent<CanvasGroup>();
            for (float t = 0f; t < 1f; t += Time.deltaTime / FadeInSeconds)
            {
                fade.alpha = t;
                card.localScale = Vector3.one * Mathf.Lerp(1.45f, 1f, t * t); // stamps down
                yield return null;
            }
            fade.alpha = 1f;
            card.localScale = Vector3.one;

            yield return new WaitForSeconds(ShowSeconds);
            for (float t = 1f; t > 0f; t -= Time.deltaTime / FadeOutSeconds)
            {
                fade.alpha = t;
                yield return null;
            }
            Destroy(banner.gameObject);
            banner = null;
            showing = null;
        }

        /// <summary>
        /// Wireframe 03-8: a gold YOUR TURN stamp, tilted a little, with the phase and how many
        /// actions you have underneath. The table dims slightly behind it.
        /// </summary>
        private void Build(Transform canvas)
        {
            banner = NewRect("TurnBanner", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            banner.SetAsLastSibling();
            var fade = banner.gameObject.AddComponent<CanvasGroup>();
            fade.blocksRaycasts = false; // never in the way of your first tap
            fade.interactable = false;
            fade.alpha = 0f;
            Image dim = banner.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.35f);
            dim.raycastTarget = false;

            card = NewRect("Card", banner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-330f, -105f), new Vector2(330f, 105f));
            card.localEulerAngles = new Vector3(0f, 0f, -3f);
            var shadow = NewRect("Shadow", card, Vector2.zero, Vector2.one, new Vector2(8f, -10f), new Vector2(8f, -10f));
            Rounded(shadow.gameObject.AddComponent<Image>(), UITheme.Charcoal, 10).raycastTarget = false;
            var face = NewRect("Face", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Rounded(face.gameObject.AddComponent<Image>(), UITheme.Brick, 10).raycastTarget = false; // red plate like the card frames

            bool night = view.CurrentState == GameState.NightPhase;
            var title = SundownFonts.Poster(NewText("Title", card, 96, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(30f, 54f), new Vector2(-30f, -14f)));
            title.text = "YOUR TURN";

            var actions = NewText("Actions", card, 26, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(1f, 0f), new Vector2(30f, 16f), new Vector2(-30f, 60f));
            int n = view.ActionsLeft;
            string count = n == 1 ? "1 action" : $"{n} actions";
            actions.text = night ? $"Night {view.CurrentDay}: {count}, one must be a Peek" : $"Day {view.CurrentDay}: {count}";
            actions.color = UITheme.Cream;
        }
    }
}
