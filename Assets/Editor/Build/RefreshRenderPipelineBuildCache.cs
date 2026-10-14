using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>Recollect persistent pipeline assets after runtime settings clones are released.</summary>
public sealed class RefreshRenderPipelineBuildCache : IPreprocessBuildWithReport
{
    // Core's own processor runs at int.MinValue + 50. Its singleton may have
    // been created while Play Mode used a temporary settings asset that was
    // destroyed on exit. Drop that gathered data before Core and URP use it.
    public int callbackOrder => int.MinValue + 1;

    public void OnPreprocessBuild(BuildReport report)
    {
        UnityEditor.Rendering.CoreBuildData.instance.Dispose();
    }
}
