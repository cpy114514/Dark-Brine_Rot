using System.Collections.Generic;
using UnityEngine;

/// <summary>Poses the imported generic rig in time with the boss's combat state.</summary>
[DefaultExecutionOrder(1000)]
public sealed class PigRoninBossVisual : MonoBehaviour
{
    PigRoninBoss boss;
    readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
    readonly Dictionary<Transform, Quaternion> previousPose = new Dictionary<Transform, Quaternion>();
    Transform hips, chest, head, leftArm, rightArm, leftForearm, rightForearm, leftLeg, rightLeg, leftKnee, rightKnee, hand, blade;
    Transform modelRoot, scabbardBone;
    Quaternion modelRest;
    Vector3 scabbardRest;
    CharacterController playerBody;
    float nextPlayerSearch;
    Material metal, dark, gold;
    Mesh bladeMesh;
    Vector3 swordAim;
    Vector3 previousPosition;
    float walkPhase, walkBlend;
    static readonly float[] AvoidanceAngles = { 15, -15, 30, -30, 45, -45, 60, -60, 90, -90, 120, -120, 180 };
    float deathTime = -1;
    public bool HasBlade => blade != null;

    void Awake()
    {
        boss = GetComponent<PigRoninBoss>();
        var animator = GetComponentInChildren<Animator>();
        if (animator)
        {
            animator.Play("HandsOnHips", 0, 0);
            animator.Update(0);
            animator.enabled = false;
            modelRoot = animator.transform;
            modelRest = modelRoot.localRotation;
        }
        var renderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer)
        {
            renderer.updateWhenOffscreen = true;
            foreach (var t in renderer.bones) if (t && !rest.ContainsKey(t)) rest.Add(t, t.localRotation);
            hips = Bone("Hips"); chest = Bone("Chest"); head = Bone("Head");
            leftArm = Bone("LeftUpperArm"); rightArm = Bone("RightUpperArm");
            leftForearm = Bone("LeftLowerArm"); rightForearm = Bone("RightLowerArm"); hand = Bone("RightHand");
            leftLeg = Bone("LeftUpperLeg"); rightLeg = Bone("RightUpperLeg");
            leftKnee = Bone("LeftLowerLeg"); rightKnee = Bone("RightLowerLeg");
            scabbardBone = Bone("Sword");
            if (scabbardBone) scabbardRest = scabbardBone.localPosition;
        }
        MakeSword();
        var grippingHand = transform.Find("Gripping hand");
        if (grippingHand)
        {
            // The closed fingers and palm share the grip's coordinate system,
            // so they remain wrapped around it through every sword rotation.
            grippingHand.SetParent(blade, false);
            grippingHand.localPosition = Vector3.zero;
            grippingHand.localRotation = Quaternion.identity;
            grippingHand.localScale = Vector3.one;
        }
        previousPosition = transform.position;
    }

    Transform Bone(string name)
    {
        foreach (var t in rest.Keys) if (t.name == name) return t;
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == name) { if (!rest.ContainsKey(t)) rest.Add(t, t.localRotation); return t; }
        return null;
    }

    Material MakeMaterial(Color color, bool unlit = false)
    {
        var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        var m = new Material(shader) { color = color };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", unlit ? 0 : .65f);
        return m;
    }

    void MakeSword()
    {
        metal = MakeMaterial(new Color(.8f, .85f, .87f));
        dark = MakeMaterial(new Color(.09f, .055f, .065f));
        gold = MakeMaterial(new Color(.71f, .45f, .12f));
        blade = new GameObject("Drawn katana").transform;
        blade.SetParent(transform, false);
        SwordPart("Blade", new Vector3(0, 0, .62f), new Vector3(.09f, .025f, .85f), metal);
        bladeMesh = MakeBladeMesh();
        blade.Find("Blade").GetComponent<MeshFilter>().sharedMesh = bladeMesh;
        SwordCylinder("Guard", new Vector3(0, 0, .20f), new Vector3(.14f, .009f, .14f), gold);
        SwordCylinder("Grip", new Vector3(0, 0, .08f), new Vector3(.052f, .10f, .052f), dark);
        SwordCylinder("Pommel", new Vector3(0, 0, -.025f), new Vector3(.057f, .008f, .057f), gold);
        for (int i = 0; i < 3; i++)
            SwordCylinder("Grip binding", new Vector3(0, 0, .025f + i * .052f), new Vector3(.055f, .003f, .055f), gold);
    }
    void SwordPart(string name, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name; part.transform.SetParent(blade, false);
        part.transform.localPosition = position; part.transform.localScale = scale;
        Destroy(part.GetComponent<Collider>()); part.GetComponent<Renderer>().sharedMaterial = material;
    }
    void SwordCylinder(string name, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        part.name = name; part.transform.SetParent(blade, false);
        part.transform.localPosition = position; part.transform.localRotation = Quaternion.Euler(90, 0, 0);
        part.transform.localScale = scale; Destroy(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
    static Mesh MakeBladeMesh()
    {
        // A lightly curved, tapered blade with a bevel and a pointed tip.
        const int rings = 12;
        var vertices = new Vector3[(rings + 1) * 4];
        var triangles = new int[rings * 24];
        for (int i = 0; i <= rings; i++)
        {
            float t = (float)i / rings;
            float curve = .35f * t * t;
            float width = i == rings ? 0 : Mathf.Lerp(.48f, .32f, t);
            float thick = i == rings ? 0 : .45f;
            int v = i * 4;
            vertices[v] = new Vector3(curve - width, 0, t - .5f);
            vertices[v + 1] = new Vector3(curve, thick, t - .5f);
            vertices[v + 2] = new Vector3(curve + width, 0, t - .5f);
            vertices[v + 3] = new Vector3(curve, -thick, t - .5f);
            if (i == rings) continue;
            int k = i * 24;
            for (int side = 0; side < 4; side++)
            {
                int a = v + side, b = v + (side + 1) % 4, c = a + 4, d = b + 4;
                int o = k + side * 6;
                triangles[o] = a; triangles[o + 1] = c; triangles[o + 2] = b;
                triangles[o + 3] = b; triangles[o + 4] = c; triangles[o + 5] = d;
            }
        }
        var mesh = new Mesh { name = "Curved katana blade", vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    void Turn(Transform bone, Vector3 axis, float angle)
    { if (bone) bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation; }

    void LateUpdate()
    {
        if (!boss) return;
        foreach (var pair in rest)
            if (pair.Key) { previousPose[pair.Key] = pair.Key.localRotation; pair.Key.localRotation = pair.Value; }
        if (scabbardBone) scabbardBone.localPosition = scabbardRest;
        Vector3 right = transform.right, up = transform.up, forward = transform.forward;
        var state = boss.CurrentState; var attack = boss.CurrentAttack; float p = boss.ActionProgress;
        float bodySpin = state == PigRoninBoss.State.Attack && attack == PigRoninBoss.AttackKind.Spin
            ? Mathf.SmoothStep(0, 1, p) * 360f : 0;
        if (modelRoot) modelRoot.localRotation = modelRest * Quaternion.AngleAxis(bodySpin, Vector3.up);
        float dt = Time.deltaTime;
        Vector3 travel = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up);
        previousPosition = transform.position;
        bool walking = state == PigRoninBoss.State.Chase || state == PigRoninBoss.State.Return;
        float speed = dt > .0001f ? travel.magnitude / dt : 0;
        walkBlend = Mathf.MoveTowards(walkBlend, walking ? Mathf.Clamp01(speed / Mathf.Max(.1f, boss.chaseSpeed)) : 0, dt * 8);
        if (walking) walkPhase += travel.magnitude * 1.7f;
        float walk = Mathf.Sin(walkPhase) * walkBlend;
        Turn(leftLeg, right, walk * 24); Turn(rightLeg, right, -walk * 24);
        Turn(leftKnee, right, Mathf.Max(0, -walk) * 24); Turn(rightKnee, right, Mathf.Max(0, walk) * 24);
        Turn(leftArm, right, -walk * 12); Turn(rightArm, right, walk * 10);
        Turn(chest, right, 3 + Mathf.Sin(Time.time * 2) * 1.5f);

        float swing = 0, lift = 0, twist = 0, lean = 0;
        Vector3 swordDirection = (forward * .8f + up * .35f).normalized;
        if (state == PigRoninBoss.State.Windup)
        {
            float t = Mathf.SmoothStep(0, 1, p);
            lift = (attack == PigRoninBoss.AttackKind.Overhead ? -115 : attack == PigRoninBoss.AttackKind.Dash ? -48 : -75) * t;
            twist = (attack == PigRoninBoss.AttackKind.Spin ? -12 : -30) * t;
            lean = attack == PigRoninBoss.AttackKind.Dash ? 14 * t : -8 * t;
            swordDirection = Vector3.Slerp(swordDirection, (up * .8f - forward * .3f + right * .35f).normalized, t);
        }
        else if (state == PigRoninBoss.State.Attack)
        {
            if (attack == PigRoninBoss.AttackKind.TwinSlash)
            {
                swing = p < .5f ? Mathf.Lerp(-75, 75, Mathf.SmoothStep(0, 1, p * 2)) : Mathf.Lerp(75, -75, Mathf.SmoothStep(0, 1, (p - .5f) * 2));
                lift = -55; twist = swing * .42f;
                swordDirection = (Quaternion.AngleAxis(swing, up) * forward + up * .12f).normalized;
            }
            else if (attack == PigRoninBoss.AttackKind.Overhead)
            {
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(p * 1.8f));
                lift = Mathf.Lerp(-115, 45, t); lean = Mathf.Lerp(-10, 28, t);
                swordDirection = Vector3.Slerp(up, (forward - up * .5f).normalized, t);
            }
            else if (attack == PigRoninBoss.AttackKind.Dash)
            {
                lift = -68; lean = 16; swordDirection = (forward + up * .05f).normalized;
            }
            else
            {
                float angle = Mathf.SmoothStep(0, 1, p) * 360;
                twist = 0; lift = -70; swordDirection = Quaternion.AngleAxis(angle, up) * forward;
            }
        }
        else if (state == PigRoninBoss.State.Staggered) { lean = -22 * Mathf.Sin(p * Mathf.PI); lift = 15; }
        else if (state == PigRoninBoss.State.Dead)
        {
            if (deathTime < 0) deathTime = Time.time;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.time - deathTime) / 1.2f));
            lean = 65 * t; Turn(leftKnee, right, 45 * t); Turn(rightKnee, right, 45 * t); lift = 45 * t;
            swordDirection = Vector3.Slerp(swordDirection, forward, t);
        }
        Turn(hips, up, twist * .3f); Turn(chest, up, twist * .7f); Turn(chest, right, lean);
        Turn(leftArm, right, lift * .3f); Turn(leftForearm, right, -10); Turn(head, right, -lean * .25f);
        Vector3 localHand = HandTarget(state, attack, p);
        Quaternion spin = Quaternion.AngleAxis(bodySpin, Vector3.up);
        SolveArm(rightArm, rightForearm, hand, transform.TransformPoint(spin * localHand),
            transform.rotation * spin * new Vector3(1, -.75f, -.15f));
        float poseBlend = 1f - Mathf.Exp(-Mathf.Max(0, Time.deltaTime) * 16f);
        foreach (var pair in rest)
            if (pair.Key && previousPose.TryGetValue(pair.Key, out var previous))
                pair.Key.localRotation = Quaternion.Slerp(previous, pair.Key.localRotation, poseBlend);
        if (scabbardBone && hips)
        {
            Vector3 towardWaist = Vector3.ProjectOnPlane(hips.position - scabbardBone.position, Vector3.up);
            if (towardWaist.sqrMagnitude > .0001f) scabbardBone.position += towardWaist.normalized * .30f;
        }
        if (blade && hand)
        {
            swordDirection = SwordTarget(state, attack, p, bodySpin);
            if (swordAim.sqrMagnitude < .01f) swordAim = swordDirection;
            swordAim = Vector3.RotateTowards(swordAim, swordDirection, 9f * Time.deltaTime, 0);
            // The imported hand joint lies inside the end of the forearm.
            // Place the palm beyond that joint so the fingers remain visible.
            Vector3 wristAxis = rightForearm ? hand.position - rightForearm.position : Vector3.zero;
            Vector3 gripCenter = hand.position + (wristAxis.sqrMagnitude > .0001f ? wristAxis.normalized * (.0833f * Mathf.Abs(transform.lossyScale.x)) : Vector3.zero);
            swordAim = AvoidPlayer(gripCenter, swordAim).normalized;
            swordDirection = swordAim;
            blade.position = gripCenter - swordDirection * (.08f * Mathf.Abs(transform.lossyScale.x));
            // Keep the palm side of the grip facing the wrist; otherwise the
            // hand turns around the sword independently of the forearm.
            Vector3 bladeRight = Vector3.ProjectOnPlane(blade.right, swordDirection);
            if (bladeRight.sqrMagnitude < .001f) bladeRight = Vector3.ProjectOnPlane(right, swordDirection);
            if (bladeRight.sqrMagnitude < .001f) bladeRight = Vector3.ProjectOnPlane(forward, swordDirection);
            bladeRight.Normalize();
            Vector3 towardGrip = Vector3.ProjectOnPlane(gripCenter - hand.position, swordDirection);
            if (towardGrip.sqrMagnitude > .0025f)
                bladeRight = Vector3.RotateTowards(bladeRight, towardGrip.normalized, 8f * Time.deltaTime, 0);
            Vector3 bladeUp = Vector3.Cross(swordDirection, bladeRight);
            blade.rotation = Quaternion.LookRotation(swordDirection, bladeUp);
        }
    }
    static float Ease(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));
    static Vector3 HandTarget(PigRoninBoss.State state, PigRoninBoss.AttackKind kind, float p)
    {
        Vector3 guard = new Vector3(.41f, .86f, .28f);
        Vector3 ready = kind == PigRoninBoss.AttackKind.Overhead ? new Vector3(.55f, 1.15f, .28f) :
            kind == PigRoninBoss.AttackKind.Dash ? new Vector3(.43f, .90f, .22f) :
            kind == PigRoninBoss.AttackKind.Spin ? new Vector3(.52f, .94f, .18f) : new Vector3(.53f, 1.02f, .10f);
        Vector3 finish = kind == PigRoninBoss.AttackKind.Overhead ? new Vector3(.33f, .76f, .37f) :
            kind == PigRoninBoss.AttackKind.Dash ? new Vector3(.40f, .84f, .43f) :
            kind == PigRoninBoss.AttackKind.Spin ? new Vector3(.52f, .88f, .27f) : new Vector3(.50f, .88f, .32f);
        if (state == PigRoninBoss.State.Windup) return Vector3.Lerp(guard, ready, Ease(p));
        if (state == PigRoninBoss.State.Attack)
        {
            if (kind == PigRoninBoss.AttackKind.TwinSlash)
            {
                Vector3 across = new Vector3(.14f, .89f, .34f);
                return p < .5f ? Vector3.Lerp(ready, across, Ease(p * 2)) : Vector3.Lerp(across, finish, Ease((p - .5f) * 2));
            }
            return Vector3.Lerp(ready, finish, Ease(kind == PigRoninBoss.AttackKind.Overhead ? p * 1.8f : p * 2f));
        }
        if (state == PigRoninBoss.State.Recover) return Vector3.Lerp(finish, guard, Ease(p));
        if (state == PigRoninBoss.State.Dead) return new Vector3(.42f, .65f, .18f);
        return guard;
    }
    Vector3 SwordTarget(PigRoninBoss.State state, PigRoninBoss.AttackKind kind, float p, float bodySpin)
    {
        Vector3 guard = new Vector3(.36f, .22f, 1).normalized;
        Vector3 ready = kind == PigRoninBoss.AttackKind.Overhead ? new Vector3(.15f, 1, -.18f).normalized :
            kind == PigRoninBoss.AttackKind.Dash ? new Vector3(.30f, .28f, 1).normalized :
            kind == PigRoninBoss.AttackKind.Spin ? new Vector3(1, .12f, .30f).normalized : new Vector3(.8f, .42f, -.3f).normalized;
        Vector3 finish = kind == PigRoninBoss.AttackKind.Overhead ? new Vector3(.18f, -.22f, 1).normalized :
            kind == PigRoninBoss.AttackKind.Dash ? new Vector3(.12f, .03f, 1).normalized :
            kind == PigRoninBoss.AttackKind.Spin ? new Vector3(1, .12f, .30f).normalized : new Vector3(.85f, .10f, .50f).normalized;
        Vector3 aim = guard;
        if (state == PigRoninBoss.State.Windup) aim = Vector3.Slerp(guard, ready, Ease(p));
        else if (state == PigRoninBoss.State.Attack)
        {
            if (kind == PigRoninBoss.AttackKind.TwinSlash)
            {
                Vector3 across = new Vector3(-.8f, .12f, .6f).normalized;
                aim = p < .5f ? Vector3.Slerp(ready, across, Ease(p * 2)) : Vector3.Slerp(across, finish, Ease((p - .5f) * 2));
            }
            else aim = Vector3.Slerp(ready, finish, Ease(kind == PigRoninBoss.AttackKind.Overhead ? p * 1.8f : p * 2));
        }
        else if (state == PigRoninBoss.State.Recover) aim = Vector3.Slerp(finish, guard, Ease(p));
        else if (state == PigRoninBoss.State.Dead) aim = new Vector3(.7f, -.4f, .2f).normalized;
        return transform.rotation * Quaternion.AngleAxis(bodySpin, Vector3.up) * aim;
    }
    static void SolveArm(Transform upper, Transform lower, Transform handBone, Vector3 target, Vector3 pole)
    {
        if (!upper || !lower || !handBone) return;
        Vector3 shoulder = upper.position, elbow = lower.position, wrist = handBone.position;
        float a = Vector3.Distance(shoulder, elbow), b = Vector3.Distance(elbow, wrist);
        Vector3 direction = target - shoulder;
        float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
        if (direction.sqrMagnitude < .000001f || a < .001f || b < .001f) return;
        direction.Normalize();
        Vector3 bend = Vector3.ProjectOnPlane(pole, direction);
        if (bend.sqrMagnitude < .001f) bend = Vector3.ProjectOnPlane(Vector3.down, direction);
        bend.Normalize();
        float along = (a * a - b * b + distance * distance) / (2 * distance);
        float height = Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
        Vector3 elbowTarget = shoulder + direction * along + bend * height;
        upper.rotation = Quaternion.FromToRotation(elbow - shoulder, elbowTarget - shoulder) * upper.rotation;
        elbow = lower.position; wrist = handBone.position;
        Vector3 wristTarget = shoulder + direction * distance;
        lower.rotation = Quaternion.FromToRotation(wrist - elbow, wristTarget - elbow) * lower.rotation;
    }
    Vector3 AvoidPlayer(Vector3 grip, Vector3 desired)
    {
        // Keep the blade pointing away from the pig's own torso as it swings.
        Vector3 outward = Vector3.ProjectOnPlane(grip - transform.position, Vector3.up);
        if (outward.sqrMagnitude > .01f)
        {
            outward.Normalize();
            float inward = Vector3.Dot(desired, outward);
            if (inward < -.05f) desired = (desired + outward * (-inward + .10f)).normalized;
        }
        if (!playerBody && Time.time >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.time + 1;
            var player = FindFirstObjectByType<ThirdPersonPlayerController>();
            if (player) playerBody = player.GetComponent<CharacterController>();
        }
        if (!playerBody || !playerBody.enabled) return desired;
        Bounds bounds = playerBody.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) + .30f;
        Vector3 lower = new Vector3(bounds.center.x, bounds.min.y + radius, bounds.center.z);
        Vector3 upper = new Vector3(bounds.center.x, bounds.max.y - radius, bounds.center.z);
        float length = 1.05f * Mathf.Abs(transform.lossyScale.x);
        bool Clear(Vector3 direction) => SegmentDistance(grip - direction * .10f, grip + direction * length, lower, upper) >= radius;
        if (Clear(desired)) return desired;
        foreach (float angle in AvoidanceAngles)
        {
            Vector3 candidate = Quaternion.AngleAxis(angle, Vector3.up) * desired;
            if (Clear(candidate)) return candidate;
        }
        Vector3 away = Vector3.ProjectOnPlane(grip - bounds.center, Vector3.up);
        return away.sqrMagnitude > .0001f ? away.normalized : -transform.forward;
    }
    // Closest distance between two finite line segments.
    static float SegmentDistance(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
    {
        Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
        float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
        float s, t;
        if (a <= .000001f && e <= .000001f) return r.magnitude;
        if (a <= .000001f) { s = 0; t = Mathf.Clamp01(f / e); }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= .000001f) { t = 0; s = Mathf.Clamp01(-c / a); }
            else
            {
                float b = Vector3.Dot(d1, d2), denominator = a * e - b * b;
                s = denominator != 0 ? Mathf.Clamp01((b * f - c * e) / denominator) : 0;
                t = (b * s + f) / e;
                if (t < 0) { t = 0; s = Mathf.Clamp01(-c / a); }
                else if (t > 1) { t = 1; s = Mathf.Clamp01((b - c) / a); }
            }
        }
        return (p1 + d1 * s - p2 - d2 * t).magnitude;
    }
    void OnDestroy()
    {
        if (metal) Destroy(metal); if (dark) Destroy(dark); if (gold) Destroy(gold);
        if (bladeMesh) Destroy(bladeMesh);
    }
}
