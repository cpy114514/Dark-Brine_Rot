using System;
using UnityEngine;

/// <summary>Readable copies of native ship render geometry for the opening rigging contact query.</summary>
public sealed class Story1RiggingContactSet : ScriptableObject
{
    [Serializable] public sealed class Node
    {
        public string name;
        public Mesh mesh;
    }
    public string sourceSha256;
    public GameObject originalShip;
    public Node[] nodes;
}
