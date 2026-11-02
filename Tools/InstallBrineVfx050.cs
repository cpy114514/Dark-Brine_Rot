using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class InstallBrineVfx050
{
    const string Root=".codex/vfx050";
    public static object Build()
    {
        if(Application.isPlaying)throw new Exception("Build outside Play");
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Combat"))AssetDatabase.CreateFolder("Assets/Resources","Combat");
        const string root="Assets/Resources/Combat/";
        var surfaceShader=Shader.Find("DarkBrine/Brine Surface");
        var stoneShader=Shader.Find("DarkBrine/Brine Ground Stone");
        if(!surfaceShader||!stoneShader)throw new Exception("Import the Brine shaders first");
        var surface=AssetDatabase.LoadAssetAtPath<Material>(root+"BrineSurface.mat");
        if(!surface){surface=new Material(surfaceShader){name="Brine layered pressure surfaces"};AssetDatabase.CreateAsset(surface,root+"BrineSurface.mat");}
        var stone=AssetDatabase.LoadAssetAtPath<Material>(root+"BrineStone.mat");
        if(!stone){stone=new Material(stoneShader){name="Shaded local ground fragments"};AssetDatabase.CreateAsset(stone,root+"BrineStone.mat");}
        var style=AssetDatabase.LoadAssetAtPath<Mavis.BrineVfxStyle>(root+"BrineVfxStyle.asset");
        if(!style){style=ScriptableObject.CreateInstance<Mavis.BrineVfxStyle>();AssetDatabase.CreateAsset(style,root+"BrineVfxStyle.asset");}
        var volumeShader=Shader.Find("DarkBrine/Brine Thermal Volume");if(!volumeShader)throw new Exception("Import the thermal volume shader");
        var noise=AssetDatabase.LoadAssetAtPath<Texture3D>(root+"BrineDensity.asset");
        if(!noise)
        {
            const int size=64;var bytes=new byte[size*size*size];
            for(int z=0;z<size;z++)for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float n=TileNoise(x/64f,y/64f,z/64f,4)*.62f+TileNoise(x/64f,y/64f,z/64f,8)*.27f+TileNoise(x/64f,y/64f,z/64f,16)*.11f;
                bytes[x+size*(y+size*z)]=(byte)Mathf.RoundToInt(Mathf.Clamp01(n)*255);
            }
            noise=new Texture3D(size,size,size,TextureFormat.R8,true){name="Tileable three-dimensional brine density",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            noise.SetPixelData(bytes,0);noise.Apply(true,true);AssetDatabase.CreateAsset(noise,root+"BrineDensity.asset");
        }
        var volume=AssetDatabase.LoadAssetAtPath<Material>(root+"BrineThermal.mat");
        if(!volume){volume=new Material(volumeShader){name="Raymarched turbulent thermal volume"};AssetDatabase.CreateAsset(volume,root+"BrineThermal.mat");}
        volume.SetTexture("_DensityTex",noise);EditorUtility.SetDirty(volume);
        style.surface=surface;style.stone=stone;style.blast=volume;EditorUtility.SetDirty(style);AssetDatabase.SaveAssets();
        return new{profile=AssetDatabase.GetAssetPath(style),shaders=new[]{surfaceShader,stoneShader,volumeShader}.Select(sh=>new{
            name=sh.name,messages=ShaderUtil.GetShaderMessages(sh).Select(m=>new{m.message,m.line,m.severity}).ToArray()}).ToArray()};
    }
    static float Hash(int x,int y,int z,int cells)
    {
        unchecked{uint h=(uint)(((x%cells)*73856093)^((y%cells)*19349663)^((z%cells)*83492791));h^=h>>13;h*=1274126177;return (h&65535)/65535f;}
    }
    static float TileNoise(float x,float y,float z,int cells)
    {
        x*=cells;y*=cells;z*=cells;int a=(int)x,b=(int)y,c=(int)z;
        float u=x-a,v=y-b,w=z-c;u=u*u*u*(u*(u*6-15)+10);v=v*v*v*(v*(v*6-15)+10);w=w*w*w*(w*(w*6-15)+10);
        return Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(Hash(a,b,c,cells),Hash(a+1,b,c,cells),u),Mathf.Lerp(Hash(a,b+1,c,cells),Hash(a+1,b+1,c,cells),u),v),
            Mathf.Lerp(Mathf.Lerp(Hash(a,b,c+1,cells),Hash(a+1,b,c+1,cells),u),Mathf.Lerp(Hash(a,b+1,c+1,cells),Hash(a+1,b+1,c+1,cells),u),v),w);
    }
    public static object State()=>new{playing=Application.isPlaying,paused=EditorApplication.isPaused,
        scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>new{SceneManager.GetSceneAt(i).path,SceneManager.GetSceneAt(i).isDirty}).ToArray(),
        inputs=InputSystem.devices.Select(d=>new{d.name,d.enabled}).ToArray()};
    public static object Sandbox(string scenePath=null)
    {
        if(Application.isPlaying||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Preserve current Editor work");
        Directory.CreateDirectory(Root);
        File.WriteAllText(Root+"/scene-setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(EditorSceneManager.GetSceneManagerSetup()));
        string source=Application.persistentDataPath;
        Directory.CreateDirectory(Root+"/save-before");
        foreach(string p in Directory.GetFiles(source,"dark-brine-rot-save*"))File.Copy(p,Root+"/save-before/"+Path.GetFileName(p),true);
        File.WriteAllText(Root+"/save-location.txt",source);
        const string campaignKey="DarkBrine.HealingPacks.Campaign";
        string campaign=PlayerPrefs.GetString(campaignKey,"legacy");
        string countKey="DarkBrine.HealingPacks.Remaining."+campaign;
        File.WriteAllText(Root+"/prefs-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
            exists=PlayerPrefs.HasKey(campaignKey),campaign,countExists=PlayerPrefs.HasKey(countKey),count=PlayerPrefs.GetInt(countKey,3)}));
        if(string.IsNullOrEmpty(scenePath))EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        else EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single);
        EditorApplication.isPlaying=true;
        return new{starting=true};
    }
    public static object Opening(string label="opening-pad",bool controller=true)
    {
        string output=Root+"/"+label+"/review.json";
        if(Directory.Exists(Path.GetDirectoryName(output)))throw new Exception("Preserve previous opening take");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        try
        {
            File.WriteAllText("Temp/opening-review-request.txt",output);File.WriteAllText("Temp/opening-review-helm.flag","true");
            if(controller)File.WriteAllText("Temp/opening-review-pad.flag","true");
            return Sandbox("Assets/Scenes/First story/Story1.unity");
        }
        catch
        {
            foreach(string file in new[]{"Temp/opening-review-request.txt","Temp/opening-review-helm.flag","Temp/opening-review-pad.flag"})if(File.Exists(file))File.Delete(file);
            throw;
        }
    }
    public static object Restore()
    {
        if(Application.isPlaying)throw new Exception("Stop Play first");
        string source=File.ReadAllText(Root+"/save-location.txt");
        bool unchanged=true;
        foreach(string p in Directory.GetFiles(Root+"/save-before"))
        {
            string target=Path.Combine(source,Path.GetFileName(p));
            if(File.Exists(target)&&File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(p)))continue;
            unchanged=false;
            if(File.Exists(target))File.Copy(target,Root+"/test-"+DateTime.UtcNow.Ticks+"-"+Path.GetFileName(p));
            File.Copy(p,target,true);
        }
        EditorSceneManager.RestoreSceneManagerSetup(Newtonsoft.Json.JsonConvert.DeserializeObject<SceneSetup[]>(File.ReadAllText(Root+"/scene-setup.json")));
        EditorApplication.isPaused=false;
        var prefs=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/prefs-before.json"));
        if((bool)prefs["exists"])PlayerPrefs.SetString("DarkBrine.HealingPacks.Campaign",(string)prefs["campaign"]);
        else PlayerPrefs.DeleteKey("DarkBrine.HealingPacks.Campaign");
        string countKey="DarkBrine.HealingPacks.Remaining."+(string)prefs["campaign"];
        if((bool)prefs["countExists"])PlayerPrefs.SetInt(countKey,(int)prefs["count"]);else PlayerPrefs.DeleteKey(countKey);
        PlayerPrefs.Save();
        typeof(Mavis.HealingPackSupply).GetMethod("ResetCache",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
        return new{saveUnchanged=unchanged,restored=true,scenes=EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path).ToArray(),
            inputs=InputSystem.devices.Select(d=>new{d.name,d.enabled}).ToArray()};
    }
}
