using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    /// <summary>Movement capsules block actors; animated anatomy remains available to weapon queries.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatBodyCollision : MonoBehaviour
    {
        static readonly List<CombatBodyCollision> actors = new List<CombatBodyCollision>();
        readonly List<(Collider a, Collider b)> changedPairs = new List<(Collider, Collider)>();
        Collider[] anatomy;
        Collider motor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetActors() => actors.Clear();

        public static void Ensure(GameObject actor)
        {
            var body = actor.GetComponent<CombatBodyCollision>();
            if (!body) actor.AddComponent<CombatBodyCollision>();
            else if (body.isActiveAndEnabled) body.Register();
        }

        public static bool IsAnatomy(Collider collider) => collider &&
            collider.name.StartsWith("BodyHitbox_", StringComparison.Ordinal);

        // Actors are not scenery. A moving arm must never become a camera wall or a floor.
        public static bool IsActor(Collider collider) => collider &&
            (IsAnatomy(collider) || collider.GetComponentInParent<CharacterController>() != null || collider.GetComponentInParent<NailongAI>() != null ||
             collider.GetComponentInParent<BombardinoBoss>() != null || collider.GetComponentInParent<Story1SharkHealth>() != null);

        void OnEnable() => Register();
        void Start() => Register();

        void Register()
        {
            motor = GetComponent<CharacterController>();
            if (!motor || !motor.enabled)
            {
                var capsule = GetComponent<CapsuleCollider>();
                if (capsule && capsule.enabled) motor = capsule;
            }
            if (anatomy == null) anatomy = Array.FindAll(GetComponentsInChildren<Collider>(true), IsAnatomy);
            if (!actors.Contains(this)) actors.Add(this);
            foreach (var other in actors)
            {
                if (!other || !other.motor) continue;
                foreach (var part in anatomy) Ignore(part, other.motor);
                if (other == this) continue;
                foreach (var part in other.anatomy)
                {
                    Ignore(motor, part);
                    // Kinematic anatomical bodies need no contact solving against each other.
                    // IgnoreCollision does not remove them from overlap/raycast weapon queries.
                    foreach (var own in anatomy) Ignore(own, part);
                }
            }
        }

        void Ignore(Collider a, Collider b)
        {
            if (!a || !b || a == b || Physics.GetIgnoreCollision(a,b)) return;
            Physics.IgnoreCollision(a,b,true);
            changedPairs.Add((a,b));
        }

        void ReleaseContacts(Transform leaving)
        {
            for (int i=changedPairs.Count-1;i>=0;i--)
            {
                var pair=changedPairs[i];
                if (pair.a && pair.b && !pair.a.transform.IsChildOf(leaving) && !pair.b.transform.IsChildOf(leaving)) continue;
                if (pair.a && pair.b) Physics.IgnoreCollision(pair.a,pair.b,false);
                changedPairs.RemoveAt(i);
            }
        }

        void OnDisable()
        {
            foreach(var actor in actors) if(actor) actor.ReleaseContacts(transform);
            actors.Remove(this);
        }
    }
}
