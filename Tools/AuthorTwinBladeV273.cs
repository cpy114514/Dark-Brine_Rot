using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Offline edits on a model-only preview rig, safe while the human is playing.
public static class AuthorTwinBladeV273
{
    const string Previous = "Assets/Resources/Combat/Combinations/BladeSlashV272.anim";
    const string Revised = "Assets/Resources/Combat/Combinations/BladeSlashV273.anim";
    const string Folder = ".codex/twin-blade-v273";
    sealed class Rig : IDisposable
    {
        public Scene scene;
        public GameObject root;
        public Animator animator;
        public AnimatorController controller;
        public Transform[] hands, blades;
        public Vector3[] tips;
        public Rig(AnimationClip clip)
        {
            scene = EditorSceneManager.NewPreviewScene();
            root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx"));
            SceneManager.MoveGameObjectToScene(root, scene); root.hideFlags = HideFlags.HideAndDontSave;
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); root.transform.localScale = Vector3.one * 3;
            animator = root.GetComponent<Animator>(); animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            controller = new AnimatorController { hideFlags = HideFlags.HideAndDontSave }; controller.AddLayer("Base Layer");
            var state = controller.layers[0].stateMachine.AddState("Slash"); state.motion = clip;
            var mirror = controller.layers[0].stateMachine.AddState("Mirror"); mirror.motion = clip; mirror.mirror = true;
            animator.runtimeAnimatorController = controller; animator.Rebind(); animator.Play("Slash", 0, 0); animator.Update(0);
            hands = new[] {HumanBodyBones.LeftHand, HumanBodyBones.RightHand}.Select(animator.GetBoneTransform).ToArray();
            blades = new Transform[2]; tips = new Vector3[2];
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Equipment/Capri/CapriTwinBlades.prefab");
            for (int i = 0; i < 2; i++)
            {
                var template = model.transform.Find(i == 0 ? "Left blade" : "Right blade");
                var go = new GameObject(template.name); go.transform.SetParent(hands[i], false);
                var mesh = template.GetComponent<MeshFilter>().sharedMesh;
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = template.GetComponent<MeshRenderer>().sharedMaterials;
                float length = Mathf.Max(mesh.bounds.size.x, Mathf.Max(mesh.bounds.size.y, mesh.bounds.size.z));
                int axis = mesh.bounds.size.x > mesh.bounds.size.y && mesh.bounds.size.x > mesh.bounds.size.z ? 0 : mesh.bounds.size.y > mesh.bounds.size.z ? 1 : 2;
                float sign = Mathf.Sign(mesh.bounds.center[axis]); if (sign == 0) sign = 1;
                float start = sign > 0 ? mesh.bounds.min[axis] : mesh.bounds.max[axis], end = sign > 0 ? mesh.bounds.max[axis] : mesh.bounds.min[axis];
                Vector3 handle = Vector3.zero, tip = Vector3.zero; int nh = 0, nt = 0;
                foreach (var v in mesh.vertices)
                { if (Mathf.Abs(v[axis] - start) < length * .18f) { handle += v; nh++; } if (Mathf.Abs(v[axis] - end) < length * .12f) { tip += v; nt++; } }
                handle /= nh; tip /= nt;
                float side = i == 0 ? -1 : 1;
                float scale = 2.52f / (length * Mathf.Abs(hands[i].lossyScale.y));
                var grip = Quaternion.FromToRotation((tip - handle).normalized, Mavis.SahurWeaponLoadout.GripShaft(side));
                go.transform.localRotation = grip; go.transform.localScale = Vector3.one * scale;
                go.transform.localPosition = new Vector3(-side * .04f, .125f, -.005f) - grip * (handle * scale);
                blades[i] = go.transform; tips[i] = tip;
            }
        }
        public void Sample(float phase, bool mirror = false)
        { animator.Play(mirror ? "Mirror" : "Slash", 0, phase); animator.Update(0); root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); }
        public Vector3 Tip(int side) => root.transform.InverseTransformPoint(blades[side].TransformPoint(tips[side]));
        public void Dispose()
        { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(controller); EditorSceneManager.ClosePreviewScene(scene); }
    }
    static float[] V(Vector3 v) => new[] {v.x,v.y,v.z};
    static float Angle(Vector3 v) => Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
    static void Capture(Rig rig, string name, bool top)
    {
        var cameraGo = new GameObject("Isolated preview camera"); SceneManager.MoveGameObjectToScene(cameraGo,rig.scene);
        var camera = cameraGo.AddComponent<Camera>(); camera.enabled=false;camera.scene=rig.scene;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.10f,.14f);camera.nearClipPlane=.05f;
        if(top){camera.orthographic=true;camera.orthographicSize=5.3f;camera.transform.position=new Vector3(0,16,1.5f);camera.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);}
        else{camera.fieldOfView=42;camera.transform.position=new Vector3(6,5,-9);camera.transform.LookAt(new Vector3(0,2,0));}
        var lightGo=new GameObject("Preview key");SceneManager.MoveGameObjectToScene(lightGo,rig.scene);var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(45,-30,0);
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){color=new Color(.15f,.55f,.38f)};
        var arrow=new GameObject("Character forward (+Z)");SceneManager.MoveGameObjectToScene(arrow,rig.scene);var line=arrow.AddComponent<LineRenderer>();line.sharedMaterial=material;line.widthMultiplier=.07f;line.positionCount=5;line.SetPositions(new[]{new Vector3(0,.02f,1.8f),new Vector3(0,.02f,5),new Vector3(-.35f,.02f,4.4f),new Vector3(0,.02f,5),new Vector3(.35f,.02f,4.4f)});
        var rt=RenderTexture.GetTemporary(640,480,24);var pixels=new Texture2D(640,480,TextureFormat.RGB24,false);var active=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,640,480),0,0,false);File.WriteAllBytes(Folder+"/"+name+".png",pixels.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);UnityEngine.Object.DestroyImmediate(arrow);UnityEngine.Object.DestroyImmediate(material);}
    }
    public static object Verify()
    {
        Directory.CreateDirectory(Folder);var checks=new List<string>();var rows=new List<object>();var statistics=new List<object>();
        var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(Previous);var revised=AssetDatabase.LoadAssetAtPath<AnimationClip>(Revised);
        if(source.length!=revised.length || !revised.humanMotion)throw new Exception("Retained timing/humanoid contract");
        var unchanged=AnimationUtility.GetCurveBindings(source).Where(b=>!b.propertyName.StartsWith("Root")).ToArray();
        if(unchanged.Any(b=>!AnimationUtility.GetEditorCurve(source,b).keys.SequenceEqual(AnimationUtility.GetEditorCurve(revised,b).keys)))throw new Exception("Muscle/IK curves changed");
        checks.Add("All "+unchanged.Length+" non-root curves remain identical; same 1.5s source duration");
        foreach(var pair in new[]{new[]{"before",Previous},new[]{"after",Revised}})
        using(var rig=new Rig(AssetDatabase.LoadAssetAtPath<AnimationClip>(pair[1])))
        {
            for(int mirrored=0;mirrored<2;mirrored++)
            {
                float sumForward=0,sumSide=0;int count=0,front=0;
                for(int sample=0;sample<=34;sample++)
                {
                    float phase=.28f+sample*.01f;rig.Sample(phase,mirrored==1);
                    for(int side=0;side<2;side++)
                    {
                        var tip=rig.Tip(side);sumForward+=tip.z;sumSide+=Mathf.Abs(tip.x);count++;if(tip.z>0)front++;
                        rows.Add(new{label=pair[0],mirrored,phase,side,tip=V(tip),angle=Angle(tip)});
                    }
                }
                statistics.Add(new{label=pair[0],mirrored,meanForward=sumForward/count,meanSide=sumSide/count,frontFraction=(float)front/count});
                foreach(float phase in new[]{.28f,.36f,.44f,.52f,.62f})
                {rig.Sample(phase,mirrored==1);string name=pair[0]+"-"+mirrored+"-"+Mathf.RoundToInt(phase*100);Capture(rig,name+"-top",true);if(phase==.44f)Capture(rig,name+"-gameplay",false);}
            }
        }
        using(var before=new Rig(source))using(var after=new Rig(revised))
        {
            foreach(float phase in new[]{0f,1f})
            {before.Sample(phase);after.Sample(phase);for(int side=0;side<2;side++)if(Vector3.Distance(before.Tip(side),after.Tip(side))>.003f)throw new Exception("Entry/recovery seam moved");}
            checks.Add("Source entry/recovery blade endpoints preserved within 3mm in local rig space");
            foreach(bool mirror in new[]{false,true})
            {after.Sample(.44f,mirror);for(int side=0;side<2;side++)if(Mathf.Abs(Angle(after.Tip(side)))>20)throw new Exception("Contact pose must sweep in front on both sides");}
            checks.Add("Both mirrored contact poses place both blade tips within 20 degrees of character forward");
        }
        var result=new{passed=true,checks,statistics,rows};File.WriteAllText(Folder+"/verified.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{passed=true,checks,statistics};
    }
    public static object Install()
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Revised);if(!clip)throw new Exception("Author first");
        var paths=new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller","Assets/Resources/Equipment/M4A1/SahurRifle.controller"};
        foreach(var path in paths)
        {
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            foreach(var state in controller.layers[0].stateMachine.states.Select(s=>s.state).Where(s=>s.name=="Capri Combo 1"||s.name=="Capri Combo 2"))
            {state.motion=clip;EditorUtility.SetDirty(state);}
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        return new{installed=true,clip=Revised,paths};
    }
    static EditorCurveBinding Binding(string n) => EditorCurveBinding.FloatCurve("", typeof(Animator), n);
    public static object Author()
    {
        Directory.CreateDirectory(Folder);
        var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(Previous);
        var clip = UnityEngine.Object.Instantiate(source); clip.name = "BladeSlashV273";
        var q = "xyzw".Select(s=>AnimationUtility.GetEditorCurve(source, Binding("RootQ."+s))).ToArray();
        var p = "xyz".Select(s=>AnimationUtility.GetEditorCurve(source, Binding("RootT."+s))).ToArray();
        var outQ = Enumerable.Range(0,4).Select(_=>new AnimationCurve()).ToArray();
        var outP = Enumerable.Range(0,3).Select(_=>new AnimationCurve()).ToArray();
        Quaternion previous = Quaternion.identity;
        for (int i=0; i<=180; i++)
        {
            float phase = i/180f, t=phase*source.length;
            float weight = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.02f,.24f,phase)) * (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.67f,.98f,phase)));
            var turn = Quaternion.AngleAxis(75f*weight,Vector3.up);
            var rotation = turn * new Quaternion(q[0].Evaluate(t),q[1].Evaluate(t),q[2].Evaluate(t),q[3].Evaluate(t)).normalized;
            var position = turn * new Vector3(p[0].Evaluate(t),p[1].Evaluate(t),p[2].Evaluate(t));
            if (i>0 && Quaternion.Dot(previous,rotation)<0) rotation = new Quaternion(-rotation.x,-rotation.y,-rotation.z,-rotation.w);
            previous = rotation;
            for(int a=0;a<4;a++)outQ[a].AddKey(t,rotation[a]);
            for(int a=0;a<3;a++)outP[a].AddKey(t,position[a]);
        }
        for(int a=0;a<4;a++)AnimationUtility.SetEditorCurve(clip,Binding("RootQ."+"xyzw"[a]),outQ[a]);
        for(int a=0;a<3;a++)AnimationUtility.SetEditorCurve(clip,Binding("RootT."+"xyz"[a]),outP[a]);
        clip.EnsureQuaternionContinuity();
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(Revised);
        if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;}
        else AssetDatabase.CreateAsset(clip,Revised);
        EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        var rows=new List<object>();
        using(var rig=new Rig(clip))foreach(float phase in new[]{.20f,.28f,.36f,.44f,.52f,.62f,.72f})
        {rig.Sample(phase);rows.Add(new{phase,leftTip=V(rig.Tip(0)),rightTip=V(rig.Tip(1)),leftAngle=Angle(rig.Tip(0)),rightAngle=Angle(rig.Tip(1))});}
        File.WriteAllText(Folder+"/candidate.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
    }
    public static object Inspect()
    {
        Directory.CreateDirectory(Folder);
        var rows = new List<object>();
        using (var rig = new Rig(AssetDatabase.LoadAssetAtPath<AnimationClip>(Previous)))
        foreach (float phase in new[] {.20f,.28f,.36f,.44f,.52f,.62f,.72f})
        {
            rig.Sample(phase);
            rows.Add(new {phase, leftHand=V(rig.root.transform.InverseTransformPoint(rig.hands[0].position)),rightHand=V(rig.root.transform.InverseTransformPoint(rig.hands[1].position)),
                leftTip=V(rig.Tip(0)),rightTip=V(rig.Tip(1)),leftAngle=Angle(rig.Tip(0)),rightAngle=Angle(rig.Tip(1))});
        }
        File.WriteAllText(Folder+"/before.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
        return rows;
    }
}
