using System;
using UnityEngine;

/// <summary>Blender-cut geometry from ship_g.fbx, with its original UVs and materials.</summary>
public sealed class Story1ShipWreckAsset : ScriptableObject
{
    [Serializable] public sealed class Fragment
    {
        public string name, sourceObject;
        public Mesh mesh;
        public Material material;
        public Vector3 sourceCenter, sourceSize;
        public float floatRotation;
        public Shard[] splinters;
    }
    [Serializable] public sealed class Shard
    {
        public Mesh mesh;
        public Vector3 localCenter,localSize;
    }
    public GameObject originalShip;
    public string sourceSha256;
    public Fragment[] decks, hulls, details;

    // The imported FBX has a 100x root scale and a 180-degree X-axis conversion.
    public static Matrix4x4 ModelToWorld(Transform ship) => ship.localToWorldMatrix *
        Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(180, 0, 0), Vector3.one * .01f);
    public static Quaternion SourceRotation(Transform ship, Fragment piece) =>
        ship.rotation * Quaternion.Euler(180, 0, 0) * Quaternion.LookRotation(Vector3.right) *
        Quaternion.Euler(0, 0, -piece.floatRotation);
}
