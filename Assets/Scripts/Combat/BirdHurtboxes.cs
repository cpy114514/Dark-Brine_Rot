using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    /// <summary>Receive-only volumes fitted to the skin, following its animated bones.</summary>
    [DisallowMultipleComponent]
    public sealed class BirdHurtboxes : MonoBehaviour
    {
        [Serializable] public sealed class Part { public int bone; public Vector3 center, size; }
        [Serializable] public sealed class Skin { public string name; public int vertices, bones; public Part[] parts; }
        [Serializable] public sealed class Data { public Skin[] skins; }
        static Data data;
        readonly List<Collider> parts = new List<Collider>();
        Health health;

        static Data Library
        {
            get
            {
                if (data != null) return data;
                var file = Resources.Load<TextAsset>("Combat/BirdHurtboxes");
                data = file ? Newtonsoft.Json.JsonConvert.DeserializeObject<Data>(file.text) : null;
                if (data == null) data = new Data();
                if (data.skins == null) data.skins = Array.Empty<Skin>();
                return data;
            }
        }

        static Skin Match(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            if (!mesh) return null;
            foreach (var skin in Library.skins)
                if (skin.name == mesh.name && skin.vertices == mesh.vertexCount && skin.bones == renderer.bones.Length)
                    return skin;
            return null;
        }

        public static BirdHurtboxes Ensure(GameObject actor)
        {
            var existing = actor.GetComponent<BirdHurtboxes>();
            if (existing) { existing.Restore(); return existing; }
            foreach (var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (Match(renderer) != null) return actor.AddComponent<BirdHurtboxes>();
            return null;
        }

        void Awake()
        {
            health = GetComponent<Health>();
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var skin = Match(renderer);
                if (skin == null || skin.parts == null) continue;
                var bones = renderer.bones;
                foreach (var shape in skin.parts)
                {
                    if (shape.bone < 0 || shape.bone >= bones.Length || !bones[shape.bone]) continue;
                    var part = new GameObject("BodyHitbox_Bird_" + bones[shape.bone].name);
                    part.layer = gameObject.layer;
                    part.transform.SetParent(bones[shape.bone], false);
                    var collider = part.AddComponent<BoxCollider>();
                    collider.center = shape.center;
                    collider.size = shape.size;
                    collider.isTrigger = true;
                    parts.Add(collider);
                }
            }
            health?.OnDeath?.AddListener(Hide);
        }

        void OnEnable() => Restore();
        void Restore()
        {
            foreach (var part in parts) if (part) part.enabled = health && !health.IsDead;
        }
        void Hide()
        {
            foreach (var part in parts) if (part) part.enabled = false;
        }
        void OnDestroy()
        {
            if (health) health.OnDeath?.RemoveListener(Hide);
            foreach (var part in parts) if (part) Destroy(part.gameObject);
        }
    }
}
