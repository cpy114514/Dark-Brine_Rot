using UnityEngine;

/// <summary>Removes the water surface inside ship_g's closed hull, including during the launch.</summary>
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(300)]
public sealed class ShipOceanMask : MonoBehaviour
{
    OceanWorld ocean;
    // Interior half widths measured every five metres along ship_g at scale 200.
    // Each vector stores the widths at heights 0, 3, 6 and 9 above its root.
    static readonly Vector4[] Widths =
    {
        Vector4.zero, Vector4.zero, new Vector4(.33f,.33f,8.08f,10.29f),
        new Vector4(1.11f,6.48f,10.15f,12.44f), new Vector4(1.11f,8.03f,11.86f,14.12f),
        new Vector4(1.11f,9.66f,13.27f,15.44f), new Vector4(1.11f,11.16f,14.41f,17.37f),
        new Vector4(1.11f,11.93f,15.22f,18.16f), new Vector4(1.11f,12.40f,15.76f,17.72f),
        new Vector4(1.11f,12.64f,16.00f,18.30f), new Vector4(1.11f,12.70f,16.06f,17.86f),
        new Vector4(1.11f,12.51f,15.94f,17.67f), new Vector4(1.11f,12.07f,15.58f,17.28f),
        new Vector4(1.11f,11.32f,14.99f,17.06f), new Vector4(1.11f,10.27f,14.38f,15.76f),
        new Vector4(1.11f,8.80f,13.15f,14.61f), new Vector4(1.11f,6.38f,11.24f,13.11f),
        new Vector4(0f,1.11f,8.94f,11.69f), new Vector4(0f,0f,2.04f,11.13f),
        new Vector4(0f,0f,0f,1.11f), Vector4.zero
    };

    void LateUpdate()
    {
        if (ocean == null) ocean = FindFirstObjectByType<OceanWorld>();
        if (ocean == null) return;
        Quaternion uprightHull = transform.rotation * Quaternion.Euler(180f, 0f, 0f);
        Vector3 scale = transform.lossyScale / 200f;
        ocean.SetShipHullMask(Matrix4x4.TRS(transform.position, uprightHull, scale).inverse, Widths);
    }

    void OnDisable()
    {
        if (ocean != null) ocean.ClearShipHullMask();
    }
}
