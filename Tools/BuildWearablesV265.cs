using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Mavis;
public static class BuildWearablesV265 {
 const string Folder="Assets/Game/Equipment/Resources/Equipment/Wearables";
 static void Ensure(string p){if(AssetDatabase.IsValidFolder(p))return;Ensure(Path.GetDirectoryName(p).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\','/'),Path.GetFileName(p));}
 public static object Run(){
 if(!Application.isPlaying||!CombatFeelPreview.Player)throw new Exception("Use the prepared native Play actor for wearable bind calibration; the Edit prefab uses a different pose.");Ensure(Folder);bool live=true;
 var player=live?CombatFeelPreview.Player:UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"));
 if(!live){player.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);player.transform.localScale=Vector3.one;}
 var anim=player.transform.Find("Pbr Sahur Visual").GetComponent<Animator>();if(!live){if(!anim.avatar)anim.avatar=player.GetComponent<Animator>().avatar;anim.Rebind();anim.Update(0);}
 var reports=new System.Collections.Generic.List<object>();
 try{foreach(var item in Resources.LoadAll<EquipmentItem>("Equipment/Nailong").Where(i=>i.id!="nailong_weakness_glasses")){
 string profilePath=Folder+"/"+item.id+".asset";
 var source=UnityEngine.Object.Instantiate(item.worldModel);source.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
 try{
 var filter=source.GetComponentInChildren<MeshFilter>();var original=filter.sharedMesh;
 string lod="Assets/AnimationStaging/NailongWearablesV265/"+item.name+".fbx";
 AssetDatabase.ImportAsset(lod,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
 var src=AssetDatabase.LoadAllAssetsAtPath(lod).OfType<Mesh>().First();
 Vector3 Remap(Vector3 v){var a=original.bounds;var z=src.bounds;return a.center+Vector3.Scale(v-z.center,new Vector3(a.size.x/z.size.x,a.size.y/z.size.y,a.size.z/z.size.z));}
 var verts=src.vertices.Select(v=>filter.transform.TransformPoint(Remap(v))).ToArray();
 var normals=src.normals.Select(n=>filter.transform.TransformDirection(n).normalized).ToArray();
 if(item.slot==SahurLoadoutUI.EquipmentSlot.Feet){var turn=Quaternion.Euler(0,90,0);verts=verts.Select(v=>turn*v).ToArray();normals=normals.Select(v=>turn*v).ToArray();}
 Bounds b=new Bounds(verts[0],Vector3.zero);foreach(var v in verts)b.Encapsulate(v);
 var parts=new System.Collections.Generic.List<SahurWearableModel.Part>();
 bool shoes=item.slot==SahurLoadoutUI.EquipmentSlot.Feet,legs=item.slot==SahurLoadoutUI.EquipmentSlot.Legs;
 int partCount=shoes?2:1;
 for(int part=0;part<partCount;part++){
 bool left=part==0;var bones=shoes?new[]{left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot}:legs?new[]{HumanBodyBones.Hips,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg}:new[]{HumanBodyBones.Head};
 var transforms=bones.Select(x=>anim.GetBoneTransform(x)).ToArray();
 var fitted=new Vector3[verts.Length];var weights=new BoneWeight[verts.Length];
 Vector3 hip=player.transform.InverseTransformPoint(anim.GetBoneTransform(HumanBodyBones.Hips).position);
 Vector3 foot=player.transform.InverseTransformPoint(anim.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot).position);
 for(int i=0;i<verts.Length;i++){
 Vector3 n=verts[i]-b.center;float x=n.x/Mathf.Max(.001f,b.size.x),y=n.y/Mathf.Max(.001f,b.size.y),z=n.z/Mathf.Max(.001f,b.size.z);
 if(shoes){
 // Source is one pair. Re-centre each half, reverse its -Z toe direction.
 float sign=left?-1:1;float halfCenter=sign*.25f;
 var toeBone=anim.GetBoneTransform(left?HumanBodyBones.LeftToes:HumanBodyBones.RightToes);
 Vector3 forward=toeBone?Vector3.ProjectOnPlane(player.transform.InverseTransformPoint(toeBone.position)-foot,Vector3.up).normalized:Vector3.forward;
 if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
 Vector3 across=Vector3.Cross(Vector3.up,forward);
 fitted[i]=foot+across*(-(x-halfCenter)*.58f)+Vector3.up*(y*.18f-.03f)+forward*(-z*.65f+.10f);
 weights[i]=new BoneWeight{boneIndex0=0,weight0=1};
 }else if(legs){
 fitted[i]=new Vector3(-x*.64f,hip.y-.10f+y*.46f,-z*.46f);
 float bend=Mathf.Clamp01((-y-.05f)*1.8f)*.35f;
 weights[i]=new BoneWeight{boneIndex0=0,weight0=1-bend,boneIndex1=fitted[i].x>0?1:2,weight1=bend};
 }else{
 Vector3 head=player.transform.InverseTransformPoint(anim.GetBoneTransform(HumanBodyBones.Head).position);
 fitted[i]=head+new Vector3(-x*.43f,y*.18f+.10f,z*.38f+.08f);
 weights[i]=new BoneWeight{boneIndex0=0,weight0=1};
 }
 }
 var mesh=new Mesh{indexFormat=src.indexFormat};mesh.name=item.id+"-wearable-"+part;mesh.vertices=fitted;mesh.uv=src.uv;mesh.subMeshCount=src.subMeshCount;for(int sub=0;sub<src.subMeshCount;sub++)mesh.SetTriangles(src.GetTriangles(sub),sub);mesh.normals=normals.Select(n=>new Vector3(-n.x,n.y,-n.z)).ToArray();mesh.boneWeights=weights;
 mesh.bindposes=transforms.Select(t=>t.worldToLocalMatrix*player.transform.localToWorldMatrix).ToArray();
 if(shoes)for(int sub=0;sub<mesh.subMeshCount;sub++){
 var tri=src.GetTriangles(sub);var selected=new System.Collections.Generic.List<int>();
 for(int i=0;i<tri.Length;i+=3){float cx=(verts[tri[i]].x+verts[tri[i+1]].x+verts[tri[i+2]].x)/3-b.center.x;if((cx<0)==left){selected.Add(tri[i]);selected.Add(tri[i+1]);selected.Add(tri[i+2]);}}
 mesh.SetTriangles(selected,sub);
 }
 mesh.RecalculateBounds();mesh.bounds=new Bounds(new Vector3(0,.9f,0),new Vector3(3,4,3));
 string mp=Folder+"/"+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(mp);
 if(old){
 old.Clear();old.indexFormat=mesh.indexFormat;old.vertices=mesh.vertices;old.normals=mesh.normals;old.uv=mesh.uv;
 old.boneWeights=mesh.boneWeights;old.bindposes=mesh.bindposes;old.subMeshCount=mesh.subMeshCount;
 for(int sub=0;sub<mesh.subMeshCount;sub++)old.SetTriangles(mesh.GetTriangles(sub),sub);
 old.bounds=mesh.bounds;UnityEngine.Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(old);
 }else AssetDatabase.CreateAsset(mesh,mp);
 parts.Add(new SahurWearableModel.Part{mesh=mesh,materials=filter.GetComponent<Renderer>().sharedMaterials,bones=bones});
 }
 var model=ScriptableObject.CreateInstance<SahurWearableModel>();model.item=item;model.parts=parts.ToArray();var previous=AssetDatabase.LoadAssetAtPath<SahurWearableModel>(profilePath);
 if(previous){EditorUtility.CopySerialized(model,previous);UnityEngine.Object.DestroyImmediate(model);EditorUtility.SetDirty(previous);}else AssetDatabase.CreateAsset(model,profilePath);reports.Add(new{item.id,parts=parts.Count,vertices=verts.Length});
 }finally{UnityEngine.Object.DestroyImmediate(source);}
 }
 AssetDatabase.SaveAssets();File.WriteAllText(".codex/combat-v265/wearable-build.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));return reports;
 }finally{if(!live)UnityEngine.Object.DestroyImmediate(player);}
 }
}

