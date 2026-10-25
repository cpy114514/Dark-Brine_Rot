using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Mavis;

public static class VerifyCacheEquivalenceV243
{
    public static async Task<object> ReloadPaused()
    {
        if(!Application.isPlaying)throw new Exception("Play required");
        GameSaveManager.CancelPendingContinue();
        var ready=new TaskCompletionSource<bool>();
        void Loaded(Scene scene,LoadSceneMode mode){if(scene.path!="Assets/Scenes/First story/Story1.unity")return;EditorApplication.isPaused=true;ready.TrySetResult(true);}
        SceneManager.sceneLoaded+=Loaded;
        try{_ = SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity",LoadSceneMode.Single);await ready.Task;return new{paused=EditorApplication.isPaused};}
        finally{SceneManager.sceneLoaded-=Loaded;}
    }
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Play required");
        var observer=new GameObject("Temporary differential exact-cache audit v243").AddComponent<CacheEquivalenceV243>();observer.label=label;
        return new{started=true};
    }
}
[DefaultExecutionOrder(31550)]public sealed class CacheEquivalenceV243:MonoBehaviour
{
    public string label;Story1WreckBattle battle;int tick,hits;readonly List<object> rows=new List<object>();
    const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    object Get(string n)=>typeof(Story1WreckBattle).GetField(n,Flags).GetValue(battle);
    object Call(string n,params object[] args)=>typeof(Story1WreckBattle).GetMethod(n,Flags).Invoke(battle,args);
    void LateUpdate()
    {
        if(!battle)battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        if(!battle||!battle.RedesignActive||battle.RedesignStage==Story1WreckBattle.OpeningStage.Crush||battle.RedesignStage==Story1WreckBattle.OpeningStage.Finished)return;
        bool hit=hits!=battle.RedesignContacts;hits=battle.RedesignContacts;if(++tick%15!=0&&!hit)return;
        var shark=(Transform)Get("shark");var root=shark.position;var rotation=shark.rotation;float gap=battle.RedesignTutorialHeroGap;
        try
        {
            Call("RedSeparateTutorialShark",.18f,(Vector3?)null);Vector3 cached=shark.position;float cachedGap=battle.RedesignTutorialHeroGap;
            shark.SetPositionAndRotation(root,rotation);
            typeof(Story1WreckBattle).GetField("redTutorialVerticesFrame",Flags).SetValue(battle,-2);
            Call("RedSeparateTutorialShark",.18f,(Vector3?)null);Vector3 fresh=shark.position;float freshGap=battle.RedesignTutorialHeroGap;
            shark.SetPositionAndRotation(root,rotation);
            var skin=(SkinnedMeshRenderer)Call("RedContactSkin");Call("RedSolidIndex",skin);
            var entries=new List<DictionaryEntry>();foreach(DictionaryEntry entry in (IDictionary)Get("redSolidWood"))entries.Add(entry);
            var items=entries.ToArray();int queries=0;double contactError=0;bool equal=true;
            // Cycle real timber and query offsets, including moving relative roots
            // in the native route. Reference invalidation leaves exact narrow phase.
            foreach(var item in items.Where((x,i)=>(i+tick/15)%5==0))
            {
                var shape=(Story1WreckMeshCollision)item.Key;if(!shape||!shape.Active)continue;
                foreach(var offset in new[]{Vector3.zero,Vector3.up*.125f})
                {
                    object[] a={shape,skin,offset,Vector3.zero};bool ca=(bool)Call("RedSolidTouch",a);
                    item.Value.GetType().GetField("poseVersion").SetValue(item.Value,-2);
                    object[] b={shape,skin,offset,Vector3.zero};bool cb=(bool)Call("RedSolidTouch",b);
                    equal&=ca==cb;if(ca&&cb)contactError=Math.Max(contactError,Vector3.Distance((Vector3)a[3],(Vector3)b[3]));queries++;
                }
            }
            bool passed=cached.Equals(fresh)&&cachedGap.Equals(freshGap)&&equal&&contactError==0;
            rows.Add(new{frame=Time.frameCount,stage=battle.RedesignStage.ToString(),time=Get("redesignTime"),hit,queries,positionError=Vector3.Distance(cached,fresh),gapError=cachedGap-freshGap,contactError,passed});
            File.WriteAllText(".codex/opening-charge-20261023/"+label+"/cache-equivalence.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
            if(!passed)Debug.LogError("Cached/reference native result differs; reject v243.");
        }
        catch(Exception e){File.WriteAllText(".codex/opening-charge-20261023/"+label+"/cache-equivalence-error.txt",e.ToString());enabled=false;}
        finally{shark.SetPositionAndRotation(root,rotation);typeof(Story1WreckBattle).GetProperty("RedesignTutorialHeroGap").SetValue(battle,gap);}
    }
}
