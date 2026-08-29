namespace TOME.Core
{
    public static class GameConstants
    {
        public const int ScreenWidth = 1080;
        public const int ScreenHeight = 1920;

        // Vertical layout (per spec, 1920 base)
        public const float EnemyZoneTop = 100f;
        public const float EnemyZoneBottom = 500f;   // clamp from 600 per UX review
        public const float DragZoneTop = 600f;
        public const float DragZoneBottom = 960f;
        public const float InventoryBarTop = 960f;
        public const float InventoryBarBottom = 1920f;

        public const int InventorySlotCount = 4;
        public const int MaxItemTier = 5;

        // Time
        public const float CombatPausedScale = 0f;
        public const float CombatNormalScale = 1f;
    }
}
