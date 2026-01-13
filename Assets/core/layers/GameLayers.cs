namespace Starfire.Core
{
    /// <summary>
    /// Constants for physics layer names and indices.
    /// User must configure these layers in Unity Editor:
    /// Edit > Project Settings > Tags and Layers
    /// </summary>
    public static class GameLayers
    {
        // Layer names (must match Unity's layer configuration)
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Projectile = "Projectile";

        // Layer indices (must match Unity's layer configuration)
        public const int PlayerIndex = 8;
        public const int EnemyIndex = 9;
        public const int ProjectileIndex = 10;
    }
}
