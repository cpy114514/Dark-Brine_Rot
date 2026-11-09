using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MarshmallowHollowArt
{
    public readonly Material Stone, Shadow, Sugar, Copper, Amber, Mint, Rose, Ink;
    readonly List<Material> materials = new List<Material>(8);
    readonly Mesh candyMesh;
    Mesh crystalMesh;

    public MarshmallowHollowArt(Mesh marshmallow = null)
    {
        candyMesh = marshmallow;
        Stone = Material("Lilac sugarstone", new Color(.30f,.225f,.36f));
        Shadow = Material("Charred sugar", new Color(.113f,.080f,.151f));
        Sugar = Material("Sugar crust", new Color(.64f,.52f,.65f));
        Copper = Material("Old copper", new Color(.51f,.30f,.23f));
        Ink = Material("Engraved stone", new Color(.25f,.21f,.31f));
        Amber = Material("Lantern sugar", new Color(1f,.61f,.30f), .36f);
        Mint = Material("Mint crystal", new Color(.38f,.94f,.80f), .26f);
        Rose = Material("Rose crystal", new Color(.93f,.49f,.67f), .24f);
    }

    Material Material(string name, Color color, float glow = 0)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
        material.SetFloat("_Smoothness", glow > 0 ? .42f : .16f);
        if (glow > 0)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * glow);
        }
        materials.Add(material);
        return material;
    }

    public static Transform Node(Transform parent, string name, Vector3 at)
    {
        var node = new GameObject(name).transform;
        node.SetParent(parent, false);
        node.localPosition = at;
        return node;
    }

    public Transform Box(Transform parent, string name, Vector3 at, Vector3 size, Material material, bool solid = true) =>
        Shape(PrimitiveType.Cube, parent, name, at, size, material, solid);

    Transform Shape(PrimitiveType type, Transform parent, string name, Vector3 at, Vector3 size, Material material, bool solid)
    {
        var shape = GameObject.CreatePrimitive(type);
        shape.name = name;
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = at;
        shape.transform.localScale = size;
        shape.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) shape.GetComponent<Collider>().enabled = false;
        return shape.transform;
    }

    public Transform Rounded(Transform parent, string name, Vector3 at, Vector3 size, Material material, bool solid = false)
    {
        if (!candyMesh) return Shape(PrimitiveType.Sphere, parent, name, at, size, material, solid);
        var node = Node(parent, name, at);
        var bounds = candyMesh.bounds;
        var visual = Node(node, "Sugar shell", Vector3.zero);
        visual.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        visual.localPosition = -Vector3.Scale(bounds.center, visual.localScale);
        visual.gameObject.AddComponent<MeshFilter>().sharedMesh = candyMesh;
        visual.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (solid) node.gameObject.AddComponent<BoxCollider>().size = size;
        return node;
    }

    public Transform Crystal(Transform parent, Vector3 at, float size, Material tint)
    {
        var rock = Node(parent, "Faceted sugar crystal", at);
        if (!crystalMesh) crystalMesh = MakeCrystal();
        rock.gameObject.AddComponent<MeshFilter>().sharedMesh = crystalMesh;
        rock.gameObject.AddComponent<MeshRenderer>().sharedMaterial = tint;
        rock.localScale = Vector3.one * size;
        rock.localRotation = Quaternion.Euler(14, 28, -18);
        return rock;
    }

    static Mesh MakeCrystal()
    {
        var vertices = new List<Vector3>(60);
        var faces = new List<int>(72);
        void Face(params Vector3[] points)
        {
            int start = vertices.Count;
            vertices.AddRange(points);
            for (int i = 1; i < points.Length-1; i++)
            {
                faces.Add(start); faces.Add(start+i); faces.Add(start+i+1);
            }
        }
        for (int side = 0; side < 6; side++)
        {
            float a = side * Mathf.PI / 3f, b = (side+1) * Mathf.PI / 3f;
            var x = new Vector3(Mathf.Cos(a)*.28f,0,Mathf.Sin(a)*.28f);
            var z = new Vector3(Mathf.Cos(b)*.28f,0,Mathf.Sin(b)*.28f);
            var xa=x-Vector3.up*.36f; var xb=x+Vector3.up*.35f;
            var za=z-Vector3.up*.36f; var zb=z+Vector3.up*.35f;
            Face(xa,xb,zb,za);
            Face(xb,new Vector3(.04f,.83f,0),zb);
            Face(xa,za,Vector3.down*.70f);
        }
        var mesh = new Mesh { name = "Hexagonal sugar crystal" };
        mesh.SetVertices(vertices); mesh.SetTriangles(faces,0);
        mesh.RecalculateNormals();
        return mesh;
    }

    public Transform Arch(Transform parent, Vector3 at, float width, float height, Material material)
    {
        var root = Node(parent, "Sugarstone arch", at);
        float half = width * .5f;
        for (int side = -1; side <= 1; side += 2)
        {
            Box(root, "Stone jamb", new Vector3(side * (half - .48f), height * .33f, 0), new Vector3(1.2f, height * .66f, 1.75f), material);
            Box(root, "Copper trim", new Vector3(side * (half - 1.06f), height * .35f, -.84f), new Vector3(.14f, height * .60f, .17f), Copper, false);
        }
        for (int i = 0; i <= 8; i++)
        {
            float angle = i * Mathf.PI / 8f;
            var cap = Box(root, "Arch voussoir", new Vector3(Mathf.Cos(angle) * (half - .54f), height * .64f + Mathf.Sin(angle) * height * .29f, 0),
                new Vector3(width * .185f, .94f, 1.8f), i == 4 ? Sugar : material);
            cap.localRotation = Quaternion.Euler(0,0,angle * Mathf.Rad2Deg - 90f);
        }
        return root;
    }

    public Text Panel(Transform parent, string title, Vector3 at, float width, float height)
    {
        var anchor = Node(parent, title.Split('\n')[0], at);
        Box(anchor, "Inscription backing", new Vector3(0,0,.10f), new Vector3(width+.22f,height+.18f,.18f), Copper, false);
        Box(anchor, "Inscription plate", Vector3.zero, new Vector3(width,height,.12f), Shadow, false);
        var go = new GameObject("Engraved inscription", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(anchor, false);
        var rect = (RectTransform)go.transform;
        rect.localPosition = new Vector3(0,0,-.081f);
        rect.localScale = Vector3.one * .012f;
        rect.sizeDelta = new Vector2(width / .012f,height / .012f);
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var text = new GameObject("English", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(rect, false);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(8,3); text.rectTransform.offsetMax = new Vector2(-8,-3);
        text.font = Mavis.GameLocalization.Font;
        text.fontSize = height > 1.1f ? 24 : 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = title; text.color = new Color(1,.89f,.72f);
        text.raycastTarget = false;
        return text;
    }

    // The wall reliefs and their matching keys share exactly the same silhouettes.
    public enum Mark { Wave, Kiln, Island, Plane, Falling, Slope, Club, Waymark }
    public Transform Glyph(Transform parent, Mark mark, Vector3 at, float size, Material material)
    {
        var root = Node(parent, mark + " relief", at);
        switch (mark)
        {
            case Mark.Wave:
                Stroke(root, material, size, -.8f,-.1f,-.46f,.25f,-.15f,.29f,.16f,-.12f,.45f,-.17f,.8f,.17f);
                Stroke(root, material, size, -.8f,-.43f,-.46f,-.18f,-.15f,-.15f,.16f,-.43f,.48f,-.48f,.8f,-.24f);
                break;
            case Mark.Kiln:
                Stroke(root, material, size, -.63f,-.62f,-.63f,.18f,-.33f,.56f,.32f,.56f,.63f,.18f,.63f,-.62f,-.63f,-.62f);
                Stroke(root, material, size, -.27f,-.56f,-.19f,-.07f,.06f,.26f,.22f,-.04f,.30f,-.56f);
                break;
            case Mark.Island:
                Stroke(root, material, size, -.78f,.20f,-.36f,.43f,.12f,.36f,.66f,.56f,.82f,.17f,.31f,-.30f,.05f,-.57f,-.42f,-.24f,-.78f,.20f);
                break;
            case Mark.Plane:
                Stroke(root, material, size, -.83f,.11f,.16f,.25f,.62f,.55f,.74f,.53f,.62f,.16f,.82f,-.02f,.33f,-.20f,-.18f,-.26f,-.75f,-.11f,-.83f,.11f);
                Stroke(root, material, size, -.12f,.18f,.26f,-.02f,-.02f,-.58f,-.39f,-.52f,-.21f,-.13f);
                break;
            case Mark.Falling:
                Stroke(root, material, size, -.48f,.65f,-.12f,.28f,.18f,-.06f,.52f,-.53f,.12f,-.44f);
                Stroke(root, material, size, .52f,-.53f,.50f,-.09f);
                Stroke(root, material, size, -.67f,.34f,-.28f,-.01f);
                break;
            case Mark.Slope:
                Stroke(root, material, size, -.80f,.43f,-.48f,.39f,-.25f,.18f,.02f,-.17f,.32f,-.32f,.74f,-.29f);
                Stroke(root, material, size, .29f,-.14f,.29f,.37f,.70f,.27f,.29f,.17f);
                break;
            case Mark.Club:
                Stroke(root, material, size, -.47f,-.61f,-.19f,-.24f,.34f,.62f,.61f,.62f,.73f,.38f,.51f,.13f,-.31f,-.68f,-.47f,-.61f);
                Stroke(root, material, size, .17f,.27f,.42f,.43f,.53f,.38f);
                break;
            case Mark.Waymark:
                Stroke(root, material, size, -.68f,0,0,.73f,.68f,0,0,-.73f,-.68f,0);
                Stroke(root, material, size, -.43f,.14f,.13f,.14f,.37f,.38f);
                Stroke(root, material, size, -.39f,-.24f,-.10f,-.02f,.42f,-.02f);
                break;
        }
        return root;
    }

    void Stroke(Transform parent, Material material, float scale, params float[] coordinates)
    {
        var go = Node(parent, "Carving", Vector3.zero);
        var line = go.gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.sharedMaterial = material;
        line.alignment = LineAlignment.TransformZ;
        line.widthMultiplier = .082f * scale;
        line.numCornerVertices = 2; line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.positionCount = coordinates.Length / 2;
        for (int i = 0; i < line.positionCount; i++)
            line.SetPosition(i, new Vector3(coordinates[i * 2], coordinates[i * 2 + 1], 0) * scale);
    }

    public void Lantern(Transform parent, Vector3 at, Material crystal, Color tint, float range = 12)
    {
        Crystal(parent, at, .7f, crystal);
        var light = Node(parent, "Crystal spill light", at + Vector3.up * .6f).gameObject.AddComponent<Light>();
        light.type = LightType.Point; light.color = tint; light.intensity = 3.0f; light.range = range;
        light.shadows = LightShadows.None;
    }

    public void Dispose()
    {
        foreach (var material in materials) if (material) Object.Destroy(material);
        if (crystalMesh) Object.Destroy(crystalMesh);
    }
}

