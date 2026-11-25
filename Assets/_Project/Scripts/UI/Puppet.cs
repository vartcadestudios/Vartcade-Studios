using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The shared placeholder puppet from the wireframes: a cutout figure in a teal jumpsuit with its
    /// seat number on the chest. Thin or big body. It breathes, blinks, looks toward the pointer and
    /// now and then checks its wristwatch. Drawn in code; final avatar art replaces it later.
    /// </summary>
    public class Puppet : MonoBehaviour
    {
        private RectTransform body;
        private RectTransform head;
        private RectTransform eyes;
        private RectTransform leftEye, rightEye;
        private RectTransform watchArm;
        private float nextBlink;
        private float blinkUntil;
        private float nextWatch;
        private float watchUntil;
        private float phase;

        public bool EyesClosed { get; set; }

        // ---------- The design team's characters (Resources/Puppets) ----------
        // Parts from the team's PSD drafts (head, torso, front arm), coloured and set in a teal Silong jumpsuit.
        // Each part sits in its box inside the puppet (0 to 1, y up), so the rig scales to any height.
        private struct Rect4
        {
            public readonly float x0, y0, x1, y1;
            public Rect4(float x0, float y0, float x1, float y1) { this.x0 = x0; this.y0 = y0; this.x1 = x1; this.y1 = y1; }
            public static readonly Rect4 None = new Rect4(0f, 0f, 0f, 0f);
        }

        private class Variant
        {
            public readonly string key;
            public readonly float aspect; // width / height
            public readonly Color skin;   // for the eyelids
            public readonly Rect4 torso, arm, head, hairBack, eyeLeft, eyeRight; // eyes are inside the head box

            public Variant(string key, float aspect, Color skin, Rect4 torso, Rect4 arm, Rect4 head, Rect4 hairBack, Rect4 eyeLeft, Rect4 eyeRight)
            {
                this.key = key; this.aspect = aspect; this.skin = skin;
                this.torso = torso; this.arm = arm; this.head = head; this.hairBack = hairBack; this.eyeLeft = eyeLeft; this.eyeRight = eyeRight;
            }

            public bool HasHairBack => hairBack.x1 > hairBack.x0;
        }

        // The cast, C1 to C6 (Docs/assets/characters/puppets): bigger heads use the big body, smaller heads the thin one.
        // C1 and C4 have their long hair on its own layer (hairback) so it can sway behind the shoulders.
        private static readonly Variant[] Variants =
        {
            new Variant("c1", 0.5908f, new Color32(198, 138, 94, 255),
                new Rect4(0.0000f, 0.0508f, 0.9735f, 0.6041f), new Rect4(0.6215f, 0.0000f, 1.0000f, 0.4868f), new Rect4(0.1011f, 0.4804f, 0.8863f, 1.0000f), new Rect4(0.1011f, 0.4281f, 0.8871f, 1.0000f),
                new Rect4(0.1696f, 0.4849f, 0.2853f, 0.5453f), new Rect4(0.4268f, 0.4705f, 0.5908f, 0.5280f)),
            new Variant("c2", 0.5785f, new Color32(232, 176, 134, 255),
                new Rect4(0.0000f, 0.0398f, 0.9821f, 0.5862f), new Rect4(0.6175f, 0.0000f, 1.0000f, 0.4808f), new Rect4(0.0663f, 0.4986f, 0.7882f, 1.0000f), Rect4.None,
                new Rect4(0.1989f, 0.4111f, 0.3578f, 0.4699f), new Rect4(0.4779f, 0.4258f, 0.6404f, 0.4846f)),
            new Variant("c3", 0.5785f, new Color32(160, 104, 66, 255),
                new Rect4(0.0000f, 0.0398f, 0.9821f, 0.5862f), new Rect4(0.6175f, 0.0000f, 1.0000f, 0.4808f), new Rect4(0.0991f, 0.4986f, 0.7555f, 1.0000f), Rect4.None,
                new Rect4(0.0889f, 0.4638f, 0.2002f, 0.5081f), new Rect4(0.3467f, 0.4638f, 0.5381f, 0.5141f)),
            new Variant("c4", 0.4815f, new Color32(185, 130, 86, 255),
                new Rect4(0.0360f, 0.0083f, 1.0000f, 0.6635f), new Rect4(0.6360f, 0.0000f, 0.9655f, 0.5096f), new Rect4(0.0000f, 0.5463f, 0.8935f, 1.0000f), new Rect4(0.0000f, 0.5463f, 0.8937f, 1.0000f),
                new Rect4(0.1939f, 0.4125f, 0.3434f, 0.5059f), new Rect4(0.4577f, 0.4125f, 0.6301f, 0.4892f)),
            new Variant("c5", 0.4585f, new Color32(141, 90, 59, 255),
                new Rect4(0.0000f, 0.0186f, 1.0000f, 0.6917f), new Rect4(0.6578f, 0.0000f, 1.0000f, 0.5032f), new Rect4(0.0666f, 0.5564f, 0.7352f, 1.0000f), Rect4.None,
                new Rect4(0.1153f, 0.5092f, 0.2561f, 0.5696f), new Rect4(0.4066f, 0.5428f, 0.5886f, 0.5948f)),
            new Variant("c6", 0.4642f, new Color32(214, 160, 118, 255),
                new Rect4(0.0000f, 0.0083f, 1.0000f, 0.6635f), new Rect4(0.6224f, 0.0000f, 0.9643f, 0.5096f), new Rect4(0.0137f, 0.5463f, 0.8384f, 1.0000f), Rect4.None,
                new Rect4(0.2213f, 0.3560f, 0.3698f, 0.4329f), new Rect4(0.5009f, 0.3471f, 0.6581f, 0.4181f)),
        };

        private bool drawn;            // built from the team's art rather than shapes
        private GameObject lidLeft, lidRight;
        private RectTransform hairBack;
        private bool mirrored;

        // Seat 1 gets C1, seat 2 C4, and so on, so big and thin bodies alternate round the table
        private static readonly int[] SeatOrder = { 0, 3, 1, 4, 2, 5 };

        /// <summary>Each seat number has its own character from the cast.</summary>
        private static Variant PickVariant(int seat, bool big)
        {
            return Variants[SeatOrder[((seat - 1) % 6 + 6) % 6]];
        }

        /// <summary>
        /// Turns the puppet to face the other way (the art faces left). Used for seats on the left of
        /// the table so they look in toward the middle. The seat number stays readable.
        /// </summary>
        public void FaceRight()
        {
            mirrored = true;
            transform.localScale = new Vector3(-1f, 1f, 1f);
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
                text.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
        }

        private static Sprite PartSprite(Variant v, string part) => Resources.Load<Sprite>($"Puppets/{v.key}_{part}");

        private static readonly Color[] Skins =
        {
            new Color32(0xE8, 0xB0, 0x86, 255), new Color32(0xC6, 0x8A, 0x5E, 255), new Color32(0x8D, 0x5A, 0x3B, 255),
        };
        private static readonly Color[] Hair =
        {
            new Color32(0x4A, 0x2E, 0x1E, 255), new Color32(0x22, 0x1A, 0x16, 255), new Color32(0x7A, 0x4E, 0x2A, 255),
        };

        /// <summary>Builds a puppet standing on its feet at the given anchor point. Height is in reference pixels.</summary>
        public static Puppet Create(Transform parent, Vector2 anchor, int seat, float height = 300f, bool big = false)
            => Create(parent, anchor, seat, height, big, false);

        /// <summary>As above; faceRight turns the puppet to look the other way (seats on the left of the table).</summary>
        public static Puppet Create(Transform parent, Vector2 anchor, int seat, float height, bool big, bool faceRight)
        {
            var root = NewRect($"Puppet{seat:00}", parent, anchor, anchor, Vector2.zero, Vector2.zero);
            root.pivot = new Vector2(0.5f, 0f);
            float width = height * (big ? 0.62f : 0.46f);
            root.sizeDelta = new Vector2(width, height);
            var puppet = root.gameObject.AddComponent<Puppet>();

            Variant variant = PickVariant(seat, big);
            Sprite torso = PartSprite(variant, "torso"), arm = PartSprite(variant, "arm"), head = PartSprite(variant, "head");
            if (torso != null && arm != null && head != null)
            {
                root.sizeDelta = new Vector2(height * variant.aspect, height);
                puppet.BuildDrawn(root, seat, height, variant, torso, arm, head);
            }
            else puppet.Build(root, seat, width, height, big);
            if (faceRight) puppet.FaceRight();
            return puppet;
        }

        private void Build(RectTransform root, int seat, float width, float height, bool big)
        {
            Color skin = Skins[(seat - 1 + Skins.Length * 10) % Skins.Length];
            Color hair = Hair[(seat + 1) % Hair.Length];
            phase = seat * 1.7f;

            // Body: a rounded jumpsuit with a darker zip seam and the seat number
            body = Part(root, "Body", new Vector2(0.5f, 0f), new Vector2(width, height * 0.62f), UITheme.Teal, width * 0.45f, new Vector2(0.5f, 0f));
            Part(body, "Seam", new Vector2(0.5f, 0f), new Vector2(Mathf.Max(3f, width * 0.03f), height * 0.56f), UITheme.TealDark, 0f, new Vector2(0.5f, 0f));
            var number = NewText("Seat", body, height * 0.075f, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.55f), new Vector2(0.48f, 0.8f), Vector2.zero, Vector2.zero);
            number.text = seat.ToString("00");
            number.color = UITheme.Brick;

            // Arm that lifts to check the watch
            watchArm = Part(body, "Arm", new Vector2(big ? 0.9f : 0.86f, 0.78f), new Vector2(width * 0.16f, height * 0.36f),
                UITheme.TealDark, width * 0.1f, new Vector2(0.5f, 1f));
            Part(watchArm, "Watch", new Vector2(0.5f, 0.08f), new Vector2(width * 0.15f, width * 0.1f), UITheme.Gold, 3f, new Vector2(0.5f, 0.5f));

            // Head with hair, eyes and a flat mouth
            float headSize = width * (big ? 0.62f : 0.78f);
            head = Part(root, "Head", new Vector2(0.5f, 0.58f), new Vector2(headSize, headSize), skin, 0f, new Vector2(0.5f, 0f), circle: true);
            Part(head, "Hair", new Vector2(0.5f, 1f), new Vector2(headSize * 1.04f, headSize * 0.42f), hair, headSize * 0.4f, new Vector2(0.5f, 1f));
            eyes = NewRect("Eyes", head, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), Vector2.zero, Vector2.zero);
            float eye = headSize * 0.11f;
            leftEye = Part(eyes, "EyeL", new Vector2(0.5f, 0.5f), new Vector2(eye, eye), UITheme.Charcoal, 0f, new Vector2(0.5f, 0.5f), circle: true);
            leftEye.anchoredPosition = new Vector2(-headSize * 0.17f, 0f);
            rightEye = Part(eyes, "EyeR", new Vector2(0.5f, 0.5f), new Vector2(eye, eye), UITheme.Charcoal, 0f, new Vector2(0.5f, 0.5f), circle: true);
            rightEye.anchoredPosition = new Vector2(headSize * 0.17f, 0f);
            Part(head, "Mouth", new Vector2(0.5f, 0.26f), new Vector2(headSize * 0.22f, Mathf.Max(2f, headSize * 0.035f)), UITheme.Charcoal, 0f, new Vector2(0.5f, 0.5f));

            nextBlink = Time.unscaledTime + Random.Range(1.5f, 4f);
            nextWatch = Time.unscaledTime + Random.Range(10f, 18f);
        }

        private void BuildDrawn(RectTransform root, int seat, float height, Variant v, Sprite torso, Sprite arm, Sprite headSprite)
        {
            drawn = true;
            phase = seat * 1.7f;

            // Long hair hangs behind the shoulders; it turns about the same neck point as the head
            Sprite hair = v.HasHairBack ? PartSprite(v, "hairback") : null;
            if (hair != null)
            {
                float px = v.head.x0 + 0.5f * (v.head.x1 - v.head.x0), py = v.head.y0 + 0.06f * (v.head.y1 - v.head.y0);
                hairBack = ArtPart(root, "HairBack", v.hairBack, hair,
                    new Vector2((px - v.hairBack.x0) / (v.hairBack.x1 - v.hairBack.x0), (py - v.hairBack.y0) / (v.hairBack.y1 - v.hairBack.y0)));
            }

            body = ArtPart(root, "Body", v.torso, torso, new Vector2(0.5f, 0f));
            // Default outfit is a dark grey tank top; the seat number is printed on it like a stencil
            var number = NewText("Seat", body, height * 0.07f, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0.14f, 0.5f), new Vector2(0.48f, 0.72f), Vector2.zero, Vector2.zero);
            number.text = seat.ToString("00");
            number.color = UITheme.WithAlpha(UITheme.Cream, 0.55f);

            watchArm = ArtPart(root, "Arm", v.arm, arm, new Vector2(0.5f, 0.95f)); // swings from the shoulder
            head = ArtPart(root, "Head", v.head, headSprite, new Vector2(0.5f, 0.06f)); // nods from the neck

            lidLeft = Lid(head, v.eyeLeft, v.skin);
            lidRight = Lid(head, v.eyeRight, v.skin);

            nextBlink = Time.unscaledTime + Random.Range(1.5f, 4f);
            nextWatch = Time.unscaledTime + Random.Range(10f, 18f);
        }

        private static RectTransform ArtPart(Transform parent, string name, Rect4 box, Sprite sprite, Vector2 pivot)
        {
            var rect = NewRect(name, parent, new Vector2(box.x0, box.y0), new Vector2(box.x1, box.y1), Vector2.zero, Vector2.zero);
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            // Paper cut-out look: each part casts a soft dark copy of itself down and to the side
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.05f, 0.03f, 0.02f, 0.38f);
            shadow.effectDistance = new Vector2(9f, -9f);
            return rect;
        }

        /// <summary>
        /// Paper puppets seated at the side of the table turn a little toward the middle: the cut-out is
        /// squashed sideways (amount 0..1), like a card seen at an angle.
        /// </summary>
        public void Turn(float amount)
        {
            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(s.x * (1f - 0.08f * Mathf.Clamp01(amount)), s.y, s.z);
        }

        /// <summary>A skin-coloured eyelid with a dark lash line, shown over one eye when it closes.</summary>
        private static GameObject Lid(RectTransform head, Rect4 eye, Color skin)
        {
            var lid = NewRect("Lid", head, new Vector2(eye.x0, eye.y0), new Vector2(eye.x1, eye.y1), new Vector2(-3f, -3f), new Vector2(3f, 3f));
            Image image = Rounded(lid.gameObject.AddComponent<Image>(), skin, 40);
            image.raycastTarget = false;
            var lash = NewRect("Lash", lid, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.2f), new Vector2(0f, -1.5f), new Vector2(0f, 1.5f));
            Image line = lash.gameObject.AddComponent<Image>();
            line.color = UITheme.Charcoal;
            line.raycastTarget = false;
            lid.gameObject.SetActive(false);
            return lid.gameObject;
        }

        private void UpdateDrawn(float t, bool still)
        {
            float breath = still ? 0f : Mathf.Sin(t * 1.6f + phase);
            body.localScale = new Vector3(1f, 1f + breath * 0.012f, 1f);
            head.anchoredPosition = new Vector2(0f, breath * 2f);

            if (t >= nextBlink)
            {
                blinkUntil = t + 0.12f;
                nextBlink = t + Random.Range(2f, 5f);
            }
            bool closed = EyesClosed || t < blinkUntil;
            lidLeft.SetActive(closed);
            lidRight.SetActive(closed);

            // The head tilts a little toward the pointer
            float tilt = 0f;
            if (!still && !EyesClosed && Mouse.current != null)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, head.position);
                tilt = Mathf.Clamp((Mouse.current.position.ReadValue().x - screen.x) / 400f, -1f, 1f) * (mirrored ? 4f : -4f);
            }
            float z = Mathf.LerpAngle(head.localEulerAngles.z, tilt + breath * 0.6f, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            head.localEulerAngles = new Vector3(0f, 0f, z);

            // The long hair follows the head and swings a little behind it
            if (hairBack != null)
            {
                hairBack.anchoredPosition = head.anchoredPosition;
                float sway = still ? 0f : Mathf.Sin(t * 1.1f + phase) * 1.8f;
                hairBack.localEulerAngles = new Vector3(0f, 0f, z + sway);
            }

            // Now and then the arm lifts a little, as if checking a watch
            if (!still && t >= nextWatch)
            {
                watchUntil = t + 1.6f;
                nextWatch = t + Random.Range(12f, 20f);
            }
            float lift = t < watchUntil ? Mathf.Sin(Mathf.InverseLerp(watchUntil - 1.6f, watchUntil, t) * Mathf.PI) : 0f;
            watchArm.localEulerAngles = new Vector3(0f, 0f, lift * 28f);
        }

        private void Update()
        {
            if (drawn)
            {
                UpdateDrawn(Time.unscaledTime, GameSettings.ReduceMotion);
                return;
            }

            float t = Time.unscaledTime;
            bool still = GameSettings.ReduceMotion;

            // Breathe: the body swells a little, the head rides on top
            float breath = still ? 0f : Mathf.Sin(t * 1.6f + phase);
            body.localScale = new Vector3(1f, 1f + breath * 0.015f, 1f);
            head.anchoredPosition = new Vector2(0f, breath * 2f);

            // Blink every 2 to 5 s (eyes stay shut while EyesClosed)
            if (t >= nextBlink)
            {
                blinkUntil = t + 0.12f;
                nextBlink = t + Random.Range(2f, 5f);
            }
            float open = EyesClosed || t < blinkUntil ? 0.12f : 1f;
            leftEye.localScale = rightEye.localScale = new Vector3(1f, open, 1f);

            // Look toward the pointer
            Vector2 look = Vector2.zero;
            if (Mouse.current != null && Screen.width > 0 && !EyesClosed)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, head.position);
                look = Vector2.ClampMagnitude((Mouse.current.position.ReadValue() - screen) / 300f, 1f);
            }
            eyes.anchoredPosition = Vector2.Lerp(eyes.anchoredPosition, look * head.rect.width * 0.06f, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

            // Now and then, lift the arm and glance at the watch
            if (!still && t >= nextWatch)
            {
                watchUntil = t + 1.6f;
                nextWatch = t + Random.Range(12f, 20f);
            }
            float lift = t < watchUntil ? Mathf.Sin(Mathf.InverseLerp(watchUntil - 1.6f, watchUntil, t) * Mathf.PI) : 0f;
            watchArm.localEulerAngles = new Vector3(0f, 0f, lift * 110f);
            if (lift > 0.3f) eyes.anchoredPosition = new Vector2(head.rect.width * 0.05f, -head.rect.width * 0.04f);
        }

        private static RectTransform Part(Transform parent, string name, Vector2 anchor, Vector2 size, Color color,
            float cornerScale, Vector2 pivot, bool circle = false)
        {
            var rect = NewRect(name, parent, anchor, anchor, Vector2.zero, Vector2.zero);
            rect.pivot = pivot;
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            if (circle) image.sprite = CircleSprite();
            else if (cornerScale > 0f) Rounded(image, color, cornerScale);
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
