using UnityEngine;

namespace Game.Gameplay.Controllers.Terrain
{
    [CreateAssetMenu(menuName = "Controllers/Terrain Config", fileName = "TerrainConfig")]
    public sealed class TerrainConfig : ScriptableObject
    {
        [Min(0.1f)] public float fragmentRadius = 0.6f;
        [Range(1, 50)] public int fragmentCount = 12;
        [Range(0f, 1f)] public float fragmentFriction = 0.5f;
        [Range(0f, 0.75f)] public float fragmentBounciness = 0f;
        [Min(0.1f)] public float debrisLifetime = 4f;
    }
}
