using UnityEngine;

/// <summary>Places the wristband beside Sahur's awakening spot.</summary>
public sealed class SecondIslandArrivalTrail : MonoBehaviour
{
    static readonly Vector2 WristbandSite = new Vector2(-143.5f, 1614.3f);
    Collider surface;
    Material postMaterial;

    public static void Build(GravityWristbandReward reward, ThirdPersonPlayerController player)
    {
        var experience = player.GetComponent<MarshmallowIslandExperience>();
        var surface = experience && experience.surface ? experience.surface.GetComponent<Collider>() : null;
        if (!surface) return;
        var arrival = new GameObject("Island arrival props").AddComponent<SecondIslandArrivalTrail>();
        arrival.transform.SetParent(reward.transform.parent, false);
        arrival.surface = surface;
        arrival.PlaceProps(reward);
    }

    void PlaceProps(GravityWristbandReward reward)
    {
        if (!TryGround(WristbandSite, out var rewardGround)) return;
        reward.transform.position = rewardGround;
        BuildSign(WristbandSite + new Vector2(2.2f, .4f), new Vector2(-147.5f, 1610.3f), "GRAVITY WRISTBAND\nWalk up to collect");
    }

    bool TryGround(Vector2 site, out Vector3 point)
    {
        var b = surface.bounds;
        bool hit = surface.Raycast(new Ray(new Vector3(site.x, b.max.y + 2f, site.y), Vector3.down), out var ground, b.size.y + 4f);
        point = ground.point;
        return hit;
    }

    void BuildSign(Vector2 site, Vector2 approach, string text)
    {
        if (!TryGround(site, out var at)) return;
        var sign = new GameObject(text.Split('\n')[0]).transform;
        sign.SetParent(transform, false);
        var facing = site - approach;
        sign.SetPositionAndRotation(at, Quaternion.LookRotation(new Vector3(facing.x, 0, facing.y)));
        if (!postMaterial) postMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.40f,.24f,.16f) };
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "Chalkboard post";
        post.transform.SetParent(sign, false);
        post.transform.localPosition = new Vector3(0,.97f,.08f);
        post.transform.localScale = new Vector3(.25f, 2.32f, .26f);
        post.GetComponent<Renderer>().sharedMaterial = postMaterial;
        SkyboardReward.Label(sign, text);
        var board = (RectTransform)sign.Find("Pickup label");
        board.localPosition = Vector3.up * 2.22f;
        board.localScale = Vector3.one * .0095f;
        board.sizeDelta = new Vector2(420, 136);
        board.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 23;
    }

    void OnDestroy()
    {
        if (postMaterial) Destroy(postMaterial);
    }
}
