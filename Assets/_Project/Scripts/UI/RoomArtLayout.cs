namespace TDTS.UI
{
    /// <summary>
    /// Where the v3 art's separate pieces sit in the 1920 x 1080 frame (x, y from the top-left, width, height).
    /// Written by the export script (Docs/assets/environment/v3/src/export_game.py) together with the sprites.
    /// </summary>
    public static class RoomArtLayout
    {
        public static readonly int[] Table = { 0, 647, 1920, 433 };
        public static readonly int[] TableNight = { 0, 647, 1920, 433 };
        public static readonly int[] Lamp = { 897, 95, 126, 185 };
        public static readonly int[] LampNight = { 895, 94, 129, 185 };
        public static readonly int[] Fan = { 1749, 396, 102, 92 };
        public static readonly int[] Spider = { 1422, 164, 39, 115 };

        public static readonly int[] StreetClouds = { 0, 89, 1828, 200 };
        public static readonly int[] StreetLaundry = { 1458, 356, 283, 66 };
        public static readonly int[] StreetCrows = { 298, 87, 902, 176 };
    }
}
