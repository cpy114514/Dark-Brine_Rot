using UnityEngine;

namespace Mavis
{
    /// <summary>Keeps a placed weapon at its authored location until Nailong is defeated.</summary>
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-150)]
    public sealed class NailongWeaponUnlock : MonoBehaviour
    {
        public string nailongSaveId;
        Renderer[] visuals;
        Collider[] colliders;
        Canvas[] captions;
        [SerializeField, HideInInspector] bool[] visibleBefore, collisionBefore, captionBefore;
        [SerializeField, HideInInspector] bool captured;
        PlacedEquipmentPickup pickup;
        bool unlocked;
        float nextCheck;

        void OnEnable()
        {
            visuals = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            captions = GetComponentsInChildren<Canvas>(true);
            if (!captured)
            {
                visibleBefore = System.Array.ConvertAll(visuals, r => r.enabled);
                collisionBefore = System.Array.ConvertAll(colliders, c => c.enabled);
                captionBefore = System.Array.ConvertAll(captions, c => c.enabled);
                captured = true;
            }
            pickup = GetComponent<PlacedEquipmentPickup>();
            unlocked = false;
            SetAvailable(false);
            CheckUnlock();
        }

        void Update()
        {
            if (!Application.isPlaying || unlocked || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .25f;
            CheckUnlock();
        }

        void CheckUnlock()
        {
            if (!Application.isPlaying) return;
            bool defeated = GameWorldProgress.IsDefeated(nailongSaveId);
            if (!defeated)
                foreach (var boss in FindObjectsByType<NailongHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var identity = boss.GetComponent<WorldSaveIdentity>();
                    if (boss.IsDead && identity && identity.id == nailongSaveId) { defeated = true; break; }
                }
            if (!defeated) return;
            unlocked = true;
            SetAvailable(true);
        }

        void SetAvailable(bool available)
        {
            for (int i = 0; i < visuals.Length; i++) if (visuals[i]) visuals[i].enabled = available && (i >= visibleBefore.Length || visibleBefore[i]);
            for (int i = 0; i < colliders.Length; i++) if (colliders[i]) colliders[i].enabled = available && (i >= collisionBefore.Length || collisionBefore[i]);
            for (int i = 0; i < captions.Length; i++) if (captions[i]) captions[i].enabled = available && (i >= captionBefore.Length || captionBefore[i]);
            if (pickup) pickup.enabled = available;
        }

        void OnDisable()
        {
            if (visuals != null) SetAvailable(true);
        }
    }
}
