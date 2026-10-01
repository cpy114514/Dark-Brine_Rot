using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MatchMenuIslandColor
{
    public static object Inspect()
    {
        var island=UnityEngine.Object.FindObjectsByType<MenuIslandDecoration>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
        var h=island.GetComponent<SahurDecorationHover>();
        return new {parents=island.GetComponentsInParent<Transform>().Select(t=>new {t.name,components=t.GetComponents<Component>().Select(c=>c.GetType().Name).ToArray()}).ToArray(), normal=h.normalSprite?.name,hover=h.hoverSprite?.name,
            normalPaper=Paper(h.normalSprite).ToString(),coloredPaper=Paper(h.hoverSprite).ToString(),
            center=h.hoverSprite.texture.GetPixelBilinear(.5f,.5f).ToString(),
            rect=((RectTransform)island.transform).sizeDelta.ToString()};
    }
    // Uncoloured paper is the common neutral reference; pencil and transparency are excluded.
    static Vector3 Paper(Sprite sprite)
    {
        var colors = sprite.texture.GetPixels32().Where(c => c.a > 250 && Math.Min(c.r,Math.Min(c.g,c.b)) > 100).ToArray();
        if (colors.Length < 100) throw new Exception("Not enough paper samples.");
        float Percentile(Func<Color32,byte> channel) => colors.Select(channel).OrderBy(v=>v).ElementAt((int)(colors.Length*.8f))/255f;
        return new Vector3(Percentile(c=>c.r),Percentile(c=>c.g),Percentile(c=>c.b));
    }
    static object[] ConfigureLocal(Material material, Sprite sprite, Vector3 reference, Vector4 global, bool colored)
    {
        var texture=sprite.texture;
        var pixels=texture.GetPixels32();
        var report=new List<object>();
        for(int y=0;y<3;y++) for(int x=0;x<3;x++)
        {
            float u=.2f+x*.3f, v=.2f+y*.3f;
            var samples=new List<Vector3>();
            for(int py=Math.Max(0,(int)((v-.20f)*texture.height));py<Math.Min(texture.height,(int)((v+.20f)*texture.height));py+=6)
            for(int px=Math.Max(0,(int)((u-.20f)*texture.width));px<Math.Min(texture.width,(int)((u+.20f)*texture.width));px+=6)
            {
                var c=pixels[py*texture.width+px];
                if(c.a<250) continue;
                var rgb=new Vector3(c.r/255f*global.x,c.g/255f*global.y,c.b/255f*global.z);
                float min=Math.Min(rgb.x,Math.Min(rgb.y,rgb.z)),max=Math.Max(rgb.x,Math.Max(rgb.y,rgb.z));
                // Neutral paper only: do not white-balance green pencil, yellow sand or brown trunks.
                if(min<.44f || max>1.03f || (max-min)/max>(colored?.17f:.22f)) continue;
                samples.Add(rgb);
            }
            Vector3 paper=reference;
            if(samples.Count>=30)
            {
                float Q(Func<Vector3,float> channel)=>samples.Select(channel).OrderBy(a=>a).ElementAt((int)(samples.Count*.75f));
                paper=new Vector3(Q(a=>a.x),Q(a=>a.y),Q(a=>a.z));
            }
            // Limit exposure equalisation; a darker pencil drawing must not become washed out.
            var gain=new Vector4(reference.x/paper.x,reference.y/paper.y,reference.z/paper.z,0);
            float luminance=.2126f*paper.x+.7152f*paper.y+.0722f*paper.z;
            float targetLuminance=.2126f*reference.x+.7152f*reference.y+.0722f*reference.z;
            float exposure=targetLuminance/luminance;
            float compensation=Mathf.Clamp(exposure,.90f,1.10f)/exposure;
            gain*=compensation;
            gain.x=Mathf.Clamp(gain.x,.78f,1.25f);gain.y=Mathf.Clamp(gain.y,.78f,1.25f);gain.z=Mathf.Clamp(gain.z,.78f,1.25f);
            material.SetVector("_Local"+x+y,gain);
            report.Add(new{region=x+","+y,samples=samples.Count,paper=paper.ToString(),gain=gain.ToString()});
        }
        material.SetFloat("_LocalCorrection",1f);
        return report.ToArray();
    }
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before saving.");
        AssetDatabase.ImportAsset("Assets/MainScene/HandDrawnContrast.shader",ImportAssetOptions.ForceSynchronousImport);
        var shader = Shader.Find("UI/Hand Drawn Contrast");
        if (UnityEditor.ShaderUtil.ShaderHasError(shader)) throw new Exception("Shader compile error.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Menu/MainMenu.unity");
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main Menu/MainMenu.unity",OpenSceneMode.Additive);
        var island = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MenuIslandDecoration>(true)).Single();
        var hover = island.GetComponent<SahurDecorationHover>();
        var tree = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MainScene/Tree_without_color.png");
        var reference = Paper(tree); var original = Paper(hover.normalSprite); var originalColored = Paper(hover.hoverSprite);
        Vector4 Gain(Vector3 paper) => new Vector4(reference.x/paper.x,reference.y/paper.y,reference.z/paper.z,0);
        var gain = Gain(original); var coloredGain = Gain(originalColored);
        var shared = AssetDatabase.LoadAssetAtPath<Material>("Assets/MainScene/ColoredDecorationContrast.mat");
        var normal = Material("Assets/MainScene/IslandNeutralPaper.mat",shader);
        var colored = Material("Assets/MainScene/IslandColoredContrast.mat",shader);
        foreach (var material in new[]{normal,colored})
        {
            Undo.RecordObject(material,"Match island paper colour");
            material.SetVector("_WhiteBalance",material==normal?gain:coloredGain); material.SetFloat("_Saturation",material==normal?1f:.7f);
            material.SetFloat("_Pivot",shared.GetFloat("_Pivot")); material.SetFloat("_AlignmentEnabled",0);
            material.SetFloat("_Contrast",material==normal?1f:shared.GetFloat("_Contrast"));
            EditorUtility.SetDirty(material);
        }
        var normalRegions=ConfigureLocal(normal,hover.normalSprite,reference,gain,false);
        var coloredRegions=ConfigureLocal(colored,hover.hoverSprite,reference,coloredGain,true);
        Undo.RecordObjects(new UnityEngine.Object[]{hover,hover.GetComponent<Image>()},"Match island colour");
        hover.SetNormalMaterial(normal); hover.hoverMaterial = colored;
        EditorUtility.SetDirty(hover); EditorUtility.SetDirty(hover.GetComponent<Image>());
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return new { referencePaper=reference.ToString(),whiteBalance=gain.ToString(),coloredWhiteBalance=coloredGain.ToString(),normalRegions,coloredRegions,contrast=colored.GetFloat("_Contrast"),sharedLocalCorrection=shared.GetFloat("_LocalCorrection") };
    }
    static Material Material(string path,Shader shader)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m) { m=new Material(shader); AssetDatabase.CreateAsset(m,path); }
        return m;
    }
}
