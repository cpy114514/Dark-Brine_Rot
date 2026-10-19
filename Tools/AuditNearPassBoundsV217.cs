using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>Read-only one-shot world-space audit. Load with the existing live Editor run_script.</summary>
public static class AuditNearPassBoundsV217
{
    public static object Start(string take)
    {
        if(!Application.isPlaying)throw new InvalidOperationException("An existing live Play recording is required");
        if(string.IsNullOrWhiteSpace(take)||Path.GetFileName(take)!=take)throw new ArgumentException("Use a take directory name");
        var observer=new GameObject("Read-only near-pass bounds audit v217").AddComponent<OpeningNearPassBoundsV217>();
        observer.take=take;
        return new{started=true,take,firstShot34Only=true};
    }
}

[DefaultExecutionOrder(32500)]
public sealed class OpeningNearPassBoundsV217:MonoBehaviour
{
    public string take;
    public int maximumSamples=1;
    Story1SharkCollisionSequence sequence;
    Story1WreckBattle battle;
    Mesh baked;
    string output;
    float nextSample;
    readonly List<Vector3> vertices=new List<Vector3>();
    readonly List<object> samples=new List<object>();

    void Start()
    {
        sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        battle=sequence!=null?sequence.GetComponent<Story1WreckBattle>():null;
        var project=Directory.GetParent(Application.dataPath).FullName;
        output=Path.Combine(project,".codex","encounter-redesign-20261017",take,"near-pass-bounds-v217.json");
        baked=new Mesh{name="Read-only native bounds sample v217"};
    }
    void LateUpdate()
    {
        if(battle==null||sequence==null||battle.RedesignStage!=Story1WreckBattle.OpeningStage.Duel||battle.RedesignShot!=34||Time.time<nextSample)return;
        nextSample=Time.time+.2f;
        try
        {
            var skinRows=new List<object>();
            foreach(var skin in sequence.sahur.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.BakeMesh(baked,false);baked.GetVertices(vertices);baked.RecalculateBounds();
                bool any=false;Bounds world=default,rotationOnly=default;
                foreach(var vertex in vertices)
                {
                    Vector3 point=skin.transform.TransformPoint(vertex);
                    Vector3 noScale=skin.transform.position+skin.transform.rotation*vertex;
                    if(!any){world=new Bounds(point,Vector3.zero);rotationOnly=new Bounds(noScale,Vector3.zero);any=true;}
                    else{world.Encapsulate(point);rotationOnly.Encapsulate(noScale);}
                }
                skinRows.Add(new{
                    name=Hierarchy(skin.transform),vertexCount=vertices.Count,skin.enabled,skin.updateWhenOffscreen,
                    sourceMesh=skin.sharedMesh!=null?skin.sharedMesh.name:null,
                    rendererWorldBounds=B(skin.bounds),rendererLocalBounds=B(skin.localBounds),
                    sourceLocalBounds=skin.sharedMesh!=null?B(skin.sharedMesh.bounds):null,
                    bakedFalseLocalBounds=B(baked.bounds),bakedFalseTransformPointWorldBounds=B(world),
                    bakedFalseRotationTranslationOnlyBounds=B(rotationOnly),
                    position=V(skin.transform.position),rotation=Q(skin.transform.rotation),
                    localScale=V(skin.transform.localScale),lossyScale=V(skin.transform.lossyScale),localToWorld=M(skin.transform.localToWorldMatrix),
                    rootBone=skin.rootBone!=null?Hierarchy(skin.rootBone):null,
                    rootBonePosition=skin.rootBone!=null?V(skin.rootBone.position):null,
                    rootBoneLossyScale=skin.rootBone!=null?V(skin.rootBone.lossyScale):null
                });
                baked.Clear();
            }
            var envelopes=new List<object>();
            var field=battle.GetType().GetField("redNearEnvelopes",BindingFlags.Instance|BindingFlags.NonPublic);
            if(field?.GetValue(battle) is IEnumerable values)
            {
                int index=0;
                foreach(var envelope in values)
                {
                    var type=envelope.GetType();
                    object Read(string name)=>type.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(envelope);
                    envelopes.Add(new{region=index==0?"skull":index==1?"chest":"hips",a=V((Vector3)Read("a")),b=V((Vector3)Read("b")),radius=Read("radius"),vertices=Read("vertices")});index++;
                }
            }
            var animator=sequence.sahur.CharacterAnimator;
            object Bone(HumanBodyBones name)
            {
                var bone=animator.GetBoneTransform(name);
                return bone==null?null:new{name=name.ToString(),path=Hierarchy(bone),position=V(bone.position),rotation=Q(bone.rotation),lossyScale=V(bone.lossyScale)};
            }
            samples.Add(new{
                frame=Time.frameCount,clock=battle.RedesignClock,stage=battle.RedesignStage.ToString(),shot=battle.RedesignShot,
                hero=V(sequence.sahur.transform.position),heroRotation=Q(sequence.sahur.transform.rotation),heroScale=V(sequence.sahur.transform.lossyScale),
                nearPassValid=battle.RedesignNearPassValid,nearPassSampleClock=battle.RedesignNearPassSampleClock,
                nearPassSignedEnvelopeGap=battle.RedesignNearPassSignedGap,
                bones=new[]{Bone(HumanBodyBones.Head),Bone(HumanBodyBones.Neck),Bone(HumanBodyBones.Chest),Bone(HumanBodyBones.Spine),Bone(HumanBodyBones.Hips)},
                envelopes,skins=skinRows
            });
        }
        catch(Exception exception){samples.Add(new{frame=Time.frameCount,clock=battle.RedesignClock,error=exception.ToString()});}
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));
        if(samples.Count>=maximumSamples)enabled=false;
    }
    static float[] V(Vector3 value)=>new[]{value.x,value.y,value.z};
    static float[] Q(Quaternion value)=>new[]{value.x,value.y,value.z,value.w};
    static object B(Bounds value)=>new{centre=V(value.center),size=V(value.size),min=V(value.min),max=V(value.max)};
    static float[] M(Matrix4x4 value)=>Enumerable.Range(0,16).Select(i=>value[i]).ToArray();
    static string Hierarchy(Transform transform)
    {
        string path=transform.name;
        while(transform.parent!=null){transform=transform.parent;path=transform.name+"/"+path;}
        return path;
    }
    void OnDestroy(){if(baked!=null)Destroy(baked);}
}
