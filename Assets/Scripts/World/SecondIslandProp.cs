using UnityEngine;

public static class SecondIslandProp
{
    public static GameObject Load(string asset) => Resources.Load<GameObject>("SecondIslandProps/" + asset);

    public static Transform Mount(GameObject asset, Transform parent, Vector3 center, Vector3 size, Quaternion rotation, bool stretch = false)
    {
        var model = new GameObject(asset.name).transform;
        model.SetParent(parent, false);
        var instance = Object.Instantiate(asset, model);
        instance.transform.localRotation = rotation * instance.transform.localRotation;
        Fit(model, center, size, stretch);
        return model;
    }

    // Imported models use different units and pivots. Measure in the holder's own axes.
    public static void Fit(Transform holder, Vector3 center, Vector3 size, bool stretch = false)
    {
        var bounds = new Bounds();
        bool measured = false;
        foreach (var mesh in holder.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!mesh.sharedMesh) continue;
            var local = mesh.sharedMesh.bounds;
            var toHolder = holder.worldToLocalMatrix * mesh.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? local.min.x : local.max.x,
                    (i & 2) == 0 ? local.min.y : local.max.y, (i & 4) == 0 ? local.min.z : local.max.z);
                var point = toHolder.MultiplyPoint3x4(corner);
                if (measured) bounds.Encapsulate(point);
                else { bounds = new Bounds(point, Vector3.zero); measured = true; }
            }
        }
        if (!measured) return;

        var dimensions = Vector3.Max(bounds.size, Vector3.one * .0001f);
        var scale = new Vector3(size.x / dimensions.x, size.y / dimensions.y, size.z / dimensions.z);
        holder.localScale = stretch ? scale : Vector3.one * Mathf.Min(scale.x, Mathf.Min(scale.y, scale.z));
        holder.localPosition = center - Vector3.Scale(bounds.center, holder.localScale);
    }
}
