using UnityEngine;

namespace Mavis
{
    /// <summary>Authored physical wayfinding sign. The arrow points at an actual destination.</summary>
    public sealed class IslandLandmark : MonoBehaviour
    {
        public string englishName, chineseName;
        public Transform destination;
        public TextMesh caption;
        void Start() { Refresh(); GameLocalization.Changed += Refresh; }
        void Refresh()
        {
            if (!caption) return;
            caption.font = GameLocalization.Font ? GameLocalization.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            caption.GetComponent<MeshRenderer>().sharedMaterial = caption.font.material;
            caption.text = englishName;
            if (destination) caption.text += "\n" + Mathf.RoundToInt(Vector3.ProjectOnPlane(destination.position - transform.position, Vector3.up).magnitude) + " m";
        }
        void OnDestroy() => GameLocalization.Changed -= Refresh;
    }
}
