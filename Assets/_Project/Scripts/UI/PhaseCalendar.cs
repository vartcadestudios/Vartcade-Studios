using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// When a Day or Night starts, the design team's "DAY 3" / "NIGHT 3" card drops onto the screen,
    /// holds for a moment, then tears away like a page ripped off a wall calendar.
    /// Never blocks taps. TurnBanner waits for it, so YOUR TURN comes after.
    /// With Reduce motion on, the card simply fades in and out.
    /// HUDController adds this at the start of a game.
    /// </summary>
    public class PhaseCalendar : MonoBehaviour
    {
        private const float DropSeconds = 0.45f, HoldSeconds = 0.9f, RipSeconds = 0.5f;
        private const float CardHeight = 440f;

        private IGameView view;
        private Coroutine showing;
        private RectTransform root;
        private string lastShown;

        private void Start()
        {
            view = GameView.Current;
            if (view == null) return;
            view.OnStateChanged += HandleStateChanged;
            HandleStateChanged(GameState.Init, view.CurrentState); // the first Day may already have started
        }

        private void OnDestroy()
        {
            if (view != null) view.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState from, GameState to)
        {
            if (to != GameState.DayPhase && to != GameState.NightPhase) return;
            bool night = to == GameState.NightPhase;
            string key = (night ? "N" : "D") + view.CurrentDay;
            if (key == lastShown) return; // one card per phase, even if the state is announced twice
            lastShown = key;

            Sprite art = CardArt.Phase(view.CurrentDay, night);
            if (art == null) return;

            if (showing != null) StopCoroutine(showing);
            if (root != null) Destroy(root.gameObject);
            showing = StartCoroutine(Show(art));
        }

        private IEnumerator Show(Sprite art)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) yield break;

            root = NewRect("PhaseCalendar", canvas.rootCanvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.SetAsLastSibling();
            var fade = root.gameObject.AddComponent<CanvasGroup>();
            fade.blocksRaycasts = false;
            fade.interactable = false;
            Image dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.45f);
            dim.raycastTarget = false;

            var card = NewRect("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.sizeDelta = new Vector2(CardHeight * CardArt.Aspect, CardHeight);
            card.pivot = new Vector2(0.5f, 1f); // hangs from its top edge, like a calendar page
            Image face = card.gameObject.AddComponent<Image>();
            face.sprite = art;
            face.preserveAspect = true;
            face.raycastTarget = false;
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(10f, -14f);

            Vector2 rest = new Vector2(0f, CardHeight / 2f);

            if (GameSettings.ReduceMotion)
            {
                card.anchoredPosition = rest;
                yield return Fade(fade, 0f, 1f, 0.2f);
                yield return new WaitForSeconds(HoldSeconds + 0.3f);
                yield return Fade(fade, 1f, 0f, 0.3f);
            }
            else
            {
                // Drop in from above with a small overshoot
                Vector2 above = rest + new Vector2(0f, 900f);
                for (float t = 0f; t < 1f; t += Time.deltaTime / DropSeconds)
                {
                    float e = EaseOutBack(t);
                    card.anchoredPosition = Vector2.LerpUnclamped(above, rest, e);
                    card.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-6f, 0f, t));
                    fade.alpha = Mathf.Clamp01(t * 3f);
                    yield return null;
                }
                card.anchoredPosition = rest;
                card.localEulerAngles = Vector3.zero;
                fade.alpha = 1f;

                yield return new WaitForSeconds(HoldSeconds);

                // Rip: the page swings from its top-left corner, then falls away
                card.pivot = new Vector2(0f, 1f);
                card.anchoredPosition = rest - new Vector2(card.sizeDelta.x / 2f, 0f);
                Vector2 start = card.anchoredPosition;
                for (float t = 0f; t < 1f; t += Time.deltaTime / RipSeconds)
                {
                    float e = t * t;
                    card.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, -38f, e));
                    card.anchoredPosition = start + new Vector2(Mathf.Lerp(0f, 160f, e), Mathf.Lerp(0f, -620f, e));
                    fade.alpha = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
                    yield return null;
                }
            }

            Destroy(root.gameObject);
            root = null;
            showing = null;
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            group.alpha = to;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
