using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    // Independent finger chains follow the animated hands and curl around the handles.
    [DisallowMultipleComponent]
    public sealed class SahurTwinBladeHandGrip : MonoBehaviour
    {
        SahurWeaponLoadout weapons;
        ThirdPersonPlayerController movement;
        SahurSwimmingWeapon swimming;
        SahurHandRigData rig;
        SkinnedMeshRenderer visual;
        readonly List<Transform> fingers = new List<Transform>();
        readonly List<GameObject> created = new List<GameObject>();
        Mesh original;
        Transform[] originalBones;
        float weight;
        public int FingerBoneCount => fingers.Count;

        void Start()
        {
            weapons = GetComponent<SahurWeaponLoadout>();
            movement = GetComponent<ThirdPersonPlayerController>();
            swimming = GetComponent<SahurSwimmingWeapon>();
            var item = Resources.Load<EquipmentItem>("Equipment/Capri/CapriTwinBlades");
            var equipment = item && item.worldModel ? item.worldModel.GetComponent<CapriTwinBladeEquipment>() : null;
            rig = equipment ? equipment.handRig : null;
            if (!rig || !rig.skinnedMesh) return;
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh != rig.sourceMesh && renderer.sharedMesh != rig.skinnedMesh) continue;
                visual = renderer;
                original = visual.sharedMesh;
                originalBones = visual.bones;
                if (originalBones.Length == rig.skinnedMesh.bindposes.Length)
                {
                    for (int i = rig.originalBoneCount; i < originalBones.Length; i++) fingers.Add(originalBones[i]);
                }
                else
                {
                    var bones = new List<Transform>(originalBones);
                    foreach (var definition in rig.fingers)
                    {
                        var bone = new GameObject(definition.name);
                        bone.transform.SetParent(bones[definition.parentIndex], false);
                        bone.transform.localPosition = definition.localPosition;
                        bones.Add(bone.transform); fingers.Add(bone.transform); created.Add(bone);
                    }
                    visual.bones = bones.ToArray();
                }
                visual.sharedMesh = rig.skinnedMesh;
                break;
            }
        }

        public void Apply(float deltaTime)
        {
            if (!visual || !rig || fingers.Count != rig.fingers.Length) return;
            bool holding = weapons && weapons.UsesTwinBlades && movement && movement.enabled &&
                !movement.Swimming && !(swimming && swimming.Climbing);
            weight = Mathf.MoveTowards(weight, holding ? 1 : 0, Mathf.Max(0, deltaTime) / .12f);
            for (int i = 0; i < fingers.Count; i++)
                fingers[i].localRotation = Quaternion.Slerp(Quaternion.identity, rig.fingers[i].closedRotation, weight);
        }

        void LateUpdate() => Apply(Time.deltaTime);
        void OnDestroy()
        {
            if (visual && original) { visual.sharedMesh = original; visual.bones = originalBones; }
            foreach (var bone in created) if (bone) Destroy(bone);
        }
    }
}
