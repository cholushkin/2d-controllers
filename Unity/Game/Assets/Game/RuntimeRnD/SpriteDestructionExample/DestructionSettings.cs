using UnityEngine;

[System.Serializable]
public class DestructionSettings
{
    [Min(0.1f)] public float fragmentRadius = 2f;
    public bool createFragments = true;
    [Range(1, 50)] public int fragmentCount = 20;
    [Range(0f, 1f)] public float fragmentFriction = 0.5f;
    [Range(0f, 0.75f)] public float fragmentBounciness = 0f;
    [Range(0f, 50f)] public float explosionImpulse = 10f;
    [Min(1f)] public float explosionRadiusMultiplier = 2f;
}