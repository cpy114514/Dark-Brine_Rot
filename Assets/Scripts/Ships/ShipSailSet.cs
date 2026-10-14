using UnityEngine;

/// <summary>Versioned render copies; the original ship and wreck source remain intact.</summary>
public sealed class ShipSailSet:ScriptableObject
{
    [System.Serializable]public struct Panel
    {
        public string node;
        public Mesh mesh;
        public Material material;
        public bool flag;
    }
    public Panel[] panels;
}
