using System;
using UnityEngine;
using UnityEditor;

public static class InstallNailongReadabilityV229
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Use Edit Mode for persistent material changes.");
        var sonar=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BossCombat/NailongSonar.mat");
        var saliva=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BossCombat/NailongSaliva.mat");
        if(!sonar||!saliva)throw new Exception("Existing Nailong materials missing.");
        sonar.SetFloat("_DistortionPixels",6);sonar.SetFloat("_RimOpacity",.18f);sonar.SetFloat("_BodyOpacity",.035f);
        saliva.SetColor("_WaterTint",new Color(.72f,.9f,.98f,1));saliva.SetFloat("_RefractionPixels",4);
        EditorUtility.SetDirty(sonar);EditorUtility.SetDirty(saliva);
        AssetDatabase.SaveAssetIfDirty(sonar);AssetDatabase.SaveAssetIfDirty(saliva);
        return new{sonarRim=.18f,refractionPixels=6,waterRefractionPixels=4,originalMaterialsUpdated=true};
    }
}
