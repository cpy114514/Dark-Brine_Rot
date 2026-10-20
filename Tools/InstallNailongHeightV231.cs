using System;
using UnityEngine;
using UnityEditor;

public static class InstallNailongHeightV231
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Use Edit Mode for persistent material changes.");
        var sonar=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BossCombat/NailongSonar.mat");
        var saliva=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/BossCombat/NailongSaliva.mat");
        if(!sonar||!saliva)throw new Exception("Existing Nailong materials missing.");
        sonar.SetFloat("_DistortionPixels",6);sonar.SetFloat("_RimOpacity",.26f);sonar.SetFloat("_BodyOpacity",.032f);

        EditorUtility.SetDirty(sonar);
        AssetDatabase.SaveAssetIfDirty(sonar);
        return new{sonarRim=.26f,refractionPixels=6,waterRefractionPixels=4,originalMaterialsUpdated=true};
    }
}
