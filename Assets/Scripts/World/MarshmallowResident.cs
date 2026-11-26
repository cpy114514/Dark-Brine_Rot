using UnityEngine;

internal sealed class MarshmallowResident
{
    public Transform Root { get; }
    readonly Transform visual, leftArm, rightArm, leftLeg, rightLeg;
    readonly float phase;

    MarshmallowResident(Transform root, float phase)
    {
        Root = root;
        this.phase = phase;
        visual = root.Find("Bouncy visual");
        leftArm = visual.Find("Left arm pivot");
        rightArm = visual.Find("Right arm pivot");
        leftLeg = visual.Find("Left leg pivot");
        rightLeg = visual.Find("Right leg pivot");
    }

    public static MarshmallowResident Spawn(string color, Vector3 position, float phase, Transform parent)
    {
        var prefab = Resources.Load<GameObject>("SecondIslandResidents/" + color + " Friendly Marshmallow");
        if (!prefab)
        {
            Debug.LogError("Missing friendly marshmallow prefab: " + color);
            return null;
        }

        var obj = Object.Instantiate(prefab, position, Quaternion.identity, parent);
        obj.name = color + " friendly resident";
        var collider = obj.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0, 1, 0);
        collider.height = 2f;
        collider.radius = .52f;
        return new MarshmallowResident(obj.transform, phase);
    }

    public void Animate(bool nearby, bool walking)
    {
        float t = Time.time * 5f + phase;
        visual.localPosition = Vector3.up * (nearby ? Mathf.Max(0, Mathf.Sin(t)) * .18f : .02f * Mathf.Sin(t * .4f));
        visual.localRotation = Quaternion.Euler(0, 0, nearby ? Mathf.Sin(t) * 4 : Mathf.Sin(t * .4f) * 2);
        if (leftArm)
            leftArm.localRotation = Quaternion.Euler(0, 0, nearby ? -100 + Mathf.Sin(t * 1.5f) * 24 : -12);
        if (rightArm)
            rightArm.localRotation = Quaternion.Euler(walking ? Mathf.Sin(t) * 30 : 0, 0, 12);
        if (leftLeg)
            leftLeg.localRotation = Quaternion.Euler(walking ? -Mathf.Sin(t) * 30 : 0, 0, -4);
        if (rightLeg)
            rightLeg.localRotation = Quaternion.Euler(walking ? Mathf.Sin(t) * 30 : 0, 0, 4);
    }
}
