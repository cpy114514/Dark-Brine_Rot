using UnityEngine;

namespace Mavis
{
    // A distinct MonoScript makes the late camera-submission order explicit.
    // Draw before native culling, after project camera-follow LateUpdate.
    [DefaultExecutionOrder(31000)]
    public sealed class GrassRenderSubmission : MonoBehaviour
    {
        internal WorldPerformanceManager owner;
        void LateUpdate()
        {
            if (owner != null && owner.isActiveAndEnabled) owner.SubmitVisibleGrass();
        }
    }
}
