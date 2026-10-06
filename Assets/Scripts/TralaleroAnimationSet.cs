using UnityEngine;

/// <summary>Blender-authored rig and contact samples in original mesh coordinates.</summary>
public sealed class TralaleroAnimationSet : ScriptableObject
{
    public GameObject rigPrefab;
    public Vector3 tailStrikeContact, shipSmashContact;
    public float cruiseSeconds=1.6f;
}
