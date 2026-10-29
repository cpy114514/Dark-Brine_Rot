using UnityEngine;
namespace Mavis.VisualStyles
{
    [CreateAssetMenu(menuName = "Dark Brine/Paper Material Profile")]
    public sealed class PaperMaterialProfile : ScriptableObject
    {
        public int Revision { get; private set; }
        void OnValidate() { Revision++; }
        [Range(0, .06f)] public float fiberStrength = .018f;
        [Range(0, 1)] public float foldShading = .85f;
        [Range(0, 1)] public float matte = .8f;
        [Range(0, 1)] public float edgeHighlight = .4f;
        [Range(.1f, 2)] public float foldScale = .55f;
        [Range(3, 12)] public float normalSteps = 5;
    }
}
