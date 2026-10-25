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
        private const float FadeInSeconds = 0.2f, ShowSeconds = 1.4f, FadeOutSeconds = 0.4f;

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
            while ((rondo != null && rondo.IsAnnouncing) || GameObject.Find("RondoBanner") != null)
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
                card.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, t);
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
        /// The table dims a little, and a rounded card across the middle shows the phase,
        /// YOUR TURN, and how many actions you have. The card has an orange edge so it stands
        /// out from the board, which is nearly the same dark blue.
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
            dim.color = new Color(0f, 0f, 0f, 0.45f);
            dim.raycastTarget = false;

            card = NewRect("Card", banner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-340f, -110f), new Vector2(340f, 110f));
            var edge = NewRect("Edge", card, Vector2.zero, Vector2.one, new Vector2(-3f, -3f), new Vector2(3f, 3f));
            Rounded(edge.gameObject.AddComponent<Image>(), AccentColor, 24).raycastTarget = false;
            var face = NewRect("Face", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Rounded(face.gameObject.AddComponent<Image>(), PanelColor, 24).raycastTarget = false;

            var stripe = NewRect("Stripe", card, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 14f), new Vector2(10f, -14f));
            Rounded(stripe.gameObject.AddComponent<Image>(), AccentColor, 4).raycastTarget = false;

            bool night = view.CurrentState == GameState.NightPhase;
            var eyebrow = NewText("Eyebrow", card, 20, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(30f, -58f), new Vector2(-30f, -20f));
            eyebrow.text = night ? $"NIGHT {view.CurrentDay}  ·  EYES CLOSED" : $"DAY {view.CurrentDay}";
            eyebrow.characterSpacing = 14f;
            eyebrow.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.7f);

            var title = SundownFonts.UseTitle(NewText("Title", card, 72, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(30f, 50f), new Vector2(-30f, -50f)));
            title.text = "YOUR TURN";
            title.color = AccentColor;

            var actions = NewText("Actions", card, 24, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(1f, 0f), new Vector2(30f, 18f), new Vector2(-30f, 58f));
            int n = view.ActionsLeft;
            actions.text = n == 1 ? "1 action" : $"{n} actions";
            actions.color = TextColor;
        }
    }
}
