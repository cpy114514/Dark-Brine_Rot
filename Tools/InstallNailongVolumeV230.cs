using System;
using UnityEngine;
using UnityEditor;

public static class InstallNailongVolumeV230
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Use Edit Mode for persistent material changes.");
        var sonar=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BossCombat/NailongSonar.mat");
        var saliva=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BossCombat/NailongSaliva.mat");
        if(!sonar||!saliva)throw new Exception("Existing Nailong materials missing.");
        sonar.SetFloat("_DistortionPixels",6);sonar.SetFloat("_RimOpacity",.26f);sonar.SetFloat("_BodyOpacity",.012f);
        saliva.SetColor("_WaterTint",new Color(.10f,.46f,.55f,1));
        saliva.SetColor("_BodyColour",new Color(.015f,.22f,.28f,1));saliva.SetFloat("_Absorption",.42f);saliva.SetFloat("_RefractionPixels",4);
        EditorUtility.SetDirty(sonar);EditorUtility.SetDirty(saliva);
        AssetDatabase.SaveAssetIfDirty(sonar);AssetDatabase.SaveAssetIfDirty(saliva);
        return new{sonarRim=.26f,refractionPixels=6,waterRefractionPixels=4,originalMaterialsUpdated=true};
    }
}
