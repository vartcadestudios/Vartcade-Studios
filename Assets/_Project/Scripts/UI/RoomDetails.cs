using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// Story props on the bunker wall behind the opponents (Resources/Room), Filipino-home style:
    /// a supply shelf that empties over the 10 days, a drugstore wall calendar counting the days left,
    /// a hand-painted BAWAL sign, a Silong Shelters poster, a broken electric fan, a wall crack that
    /// grows each Day, and flies circling the lamp (gone at Night and with Reduce motion).
    /// Props sit only where the wall is free: above the heads and between the seats.
    /// TableRoom builds this in place of its shape shelf when the images are there.
    /// </summary>
    public class RoomDetails : MonoBehaviour
    {
        private Image shelf;
        private RectTransform crack;
        private TextMeshProUGUI daysLeft;
        private RectTransform[] flies;
        private Sprite[] shelfStages;
        private int shownDay = -1;

        /// <summary>Adds the props to the room. Returns false (and adds nothing) if the shelf images are missing.</summary>
        public static bool TryBuild(RectTransform room)
        {
            var stages = new[] { Load("room_shelf_1"), Load("room_shelf_2"), Load("room_shelf_3") };
            if (stages[0] == null) return false;

            var layer = NewRect("RoomDetails", room, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var details = layer.gameObject.AddComponent<RoomDetails>();
            details.shelfStages = stages;

            // x, y are the centre in 1920 x 1080 reference pixels (y from the top), like TableRoom.Place
            // The HUD covers the left wall and the top-right corner, so the props keep clear of them
            Image crackImage = Prop(layer, "room_crack", 1110f, 245f, 120f, 120f);
            details.crack = crackImage != null ? crackImage.rectTransform : null;
            details.shelf = Prop(layer, "room_shelf_1", 715f, 425f, 300f, 243f);
            Prop(layer, "room_bawal_sign", 480f, 205f, 260f, 95f);
            Image calendar = Prop(layer, "room_calendar", 1200f, 425f, 130f, 173f);
            Prop(layer, "room_poster_silong", 1700f, 290f, 140f, 191f);
            Prop(layer, "room_fan", 1840f, 480f, 140f, 220f);

            if (calendar != null)
            {
                details.daysLeft = SundownFonts.UseTitle(NewText("DaysLeft", calendar.transform, 64f, FontStyles.Normal,
                    TextAlignmentOptions.Center, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.7f), Vector2.zero, Vector2.zero));
                details.daysLeft.color = UITheme.DarkRed;
                details.daysLeft.enableAutoSizing = true;
                details.daysLeft.fontSizeMin = 20f;
                details.daysLeft.fontSizeMax = 64f;
            }

            // Flies around the lamp bulb (the lamp hangs from the top centre)
            Sprite fly = Load("room_fly");
            if (fly != null)
            {
                details.flies = new RectTransform[3];
                for (int i = 0; i < 3; i++)
                {
                    var f = NewRect("Fly", layer, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
                    f.sizeDelta = new Vector2(20f, 16f);
                    Image image = f.gameObject.AddComponent<Image>();
                    image.sprite = fly;
                    image.raycastTarget = false;
                    details.flies[i] = f;
                }
            }
            return true;
        }

        private static Sprite Load(string name) => Resources.Load<Sprite>("Room/" + name);

        private static Image Prop(RectTransform layer, string name, float x, float y, float width, float height)
        {
            Sprite sprite = Load(name);
            if (sprite == null) return null;
            var rect = TableRoom.Place(NewRect(name, layer, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private void Update()
        {
            IGameView view = GameView.Current;
            if (view == null) return;

            // Time passes on the wall: the shelf empties, the crack grows, the calendar counts down
            int day = Mathf.Max(1, view.CurrentDay);
            if (day != shownDay)
            {
                shownDay = day;
                int stage = day <= 3 ? 0 : day <= 7 ? 1 : 2;
                if (shelf != null && shelfStages[stage] != null) shelf.sprite = shelfStages[stage];
                float grown = Mathf.Clamp01((day - 1f) / 9f);
                if (crack != null) crack.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, grown);
                if (daysLeft != null) daysLeft.text = Mathf.Max(0, view.SundownDay - day + 1).ToString();
            }

            if (flies == null) return;
            bool show = !view.EyesClosed && !GameSettings.ReduceMotion;
            float t = Time.time;
            for (int i = 0; i < flies.Length; i++)
            {
                flies[i].gameObject.SetActive(show);
                if (!show) continue;
                float p = i * 2.1f;
                float x = Mathf.Cos(t * (1.3f + i * 0.4f) + p) * (70f + 20f * i) + (Mathf.PerlinNoise(t * 2f, p) - 0.5f) * 30f;
                float y = -150f + Mathf.Sin(t * (1.7f + i * 0.3f) + p) * 35f + (Mathf.PerlinNoise(p, t * 2f) - 0.5f) * 20f;
                flies[i].anchoredPosition = new Vector2(x, y);
                flies[i].localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 9f + p) * 20f);
            }
        }
    }
}
