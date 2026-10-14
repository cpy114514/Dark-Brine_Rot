using System;
using UnityEngine;

/// <summary>Build-time foot metadata for a skin whose CPU mesh is released in a Player.</summary>
[PreferBinarySerialization]
public sealed class SahurSoleSkinCache : ScriptableObject
{
    [Serializable] public struct Vertex
    {
        public Vector3 point;
        public BoneWeight weights;
        public byte sides;
    }
    public Mesh source;
    public string dependencyHash;
    public int vertexCount, leftFoot, rightFoot, leftToe, rightToe;
    public Matrix4x4[] bind;
    public Vertex[] vertices;
}
