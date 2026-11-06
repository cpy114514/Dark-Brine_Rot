using UnityEngine;

namespace Mavis
{
    // Small, editor-authored support hulls. Runtime never reads imported meshes
    // or enables Read/Write on the 300k-vertex enemy models.
    public sealed class EnemyDeathSupportLibrary : ScriptableObject
    {
        [System.Serializable] public struct Point {public Vector3 vertex;public BoneWeight weight;}
        [System.Serializable] public sealed class Entry
        {
            public string key;
            public Matrix4x4[] bindposes;
            public Point[] points;
        }
        public Entry[] entries;
        public static string Key(Mesh mesh,int bones)=>mesh.name+"/"+mesh.vertexCount+"/"+bones;
    }
}
