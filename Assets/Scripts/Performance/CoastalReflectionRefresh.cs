using UnityEngine;

namespace Mavis
{
    // Two small local captures follow the existing dynamic day/night lighting.
    // Capture one probe at a time and let Unity time-slice its cubemap faces.
    public sealed class CoastalReflectionRefresh : MonoBehaviour
    {
        public ReflectionProbe[] probes;
        [Min(10f)] public float refreshSeconds=30;
        float nextRefresh;
        int nextProbe,renderId=-1;
        ReflectionProbe rendering;
        void Update()
        {
            if(!QualitySettings.realtimeReflectionProbes||probes==null||probes.Length==0)return;
            if(rendering&&renderId>=0&&!rendering.IsFinishedRendering(renderId))return;
            if(Time.unscaledTime<nextRefresh)return;
            var probe=probes[nextProbe++%probes.Length];
            nextRefresh=Time.unscaledTime+refreshSeconds/probes.Length;
            if(!probe||!probe.isActiveAndEnabled)return;
            rendering=probe;renderId=probe.RenderProbe();
        }
    }
}
