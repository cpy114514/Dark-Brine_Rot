using UnityEngine;
using UnityEngine.Rendering;

namespace Mavis
{
    // Submit only when the target camera actually renders, before native culling.
    [DefaultExecutionOrder(31000)]
    public sealed class GrassRenderSubmission : MonoBehaviour
    {
        internal WorldPerformanceManager owner;
        void OnEnable()=>RenderPipelineManager.beginCameraRendering+=DrawCamera;
        void OnDisable()=>RenderPipelineManager.beginCameraRendering-=DrawCamera;
        void DrawCamera(ScriptableRenderContext context,Camera camera)
        {
            if(owner!=null&&owner.isActiveAndEnabled&&camera&&camera.cameraType==CameraType.Game)
                owner.SubmitVisibleGrass(context,camera);
        }
    }
}
