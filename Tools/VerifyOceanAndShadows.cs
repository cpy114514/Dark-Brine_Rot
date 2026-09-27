using System;
using System.Linq;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class VerifyOceanAndShadows
{
    public static string Run()
    {
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var island = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (ocean == null || island == null || pipeline == null)
            throw new Exception("The ocean, island, or PC URP pipeline is missing.");
        if (!Mathf.Approximately(pipeline.shadowDistance, 180f) ||
            pipeline.shadowCascadeCount != 4 ||
            Vector3.Distance(pipeline.cascade4Split, new Vector3(0.075f, 0.22f, 0.48f)) > 0.001f)
            throw new Exception("Long-range shadow cascades are not active.");
        if (ocean.foamColor.r < 0.9f || ocean.foamStrength < 1f ||
            !Mathf.Approximately(ocean.wave1.speed, 12.2f))
            throw new Exception("The tuned wave or white-foam settings are not active.");
        var oceanMaterial = ocean.GetComponent<MeshRenderer>().sharedMaterial;
        var foam = island.transform.Find("Procedural Shore Foam");
        if (oceanMaterial == null || oceanMaterial.shader.name != "DarkBrine/Procedural Ocean" ||
            foam == null || foam.GetComponent<MeshRenderer>().sharedMaterial == null)
            throw new Exception("The water or shoreline shader is missing.");
        var mesh = foam.GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null || mesh.vertexCount < 1000)
            throw new Exception("The shoreline foam mesh was not rebuilt.");
        foreach (string name in new[] { "DarkBrine/Procedural Ocean", "DarkBrine/Procedural Shore Foam" })
            if (ShaderUtil.GetShaderMessages(Shader.Find(name)).Any(m => m.severity.ToString() == "Error"))
                throw new Exception("Shader compile error in " + name);
        return $"Ocean and shore shaders valid; {mesh.vertexCount} shore vertices; 180 m/4-cascade shadows active.";
    }

    public static string Preview()
    {
        var ocean = UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        var island = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>();
        var camera = Camera.main;
        if (ocean == null || island == null || camera == null)
            throw new Exception("Cannot position the preview camera.");
        var controller = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        if (controller != null) controller.enabled = false;
        Vector3 shore = island.transform.TransformPoint(new Vector3(island.shorelineRadius, 0f, 0f));
        Vector3 position = shore + new Vector3(10f, ocean.oceanHeight + 5f - shore.y, 0f);
        Vector3 target = shore + new Vector3(-3f, ocean.oceanHeight + 0.5f - shore.y, 0f);
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        return $"Preview camera at {position}, looking across the shore.";
    }
}
