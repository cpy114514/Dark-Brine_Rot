using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallPaddleV054
{
    public static object Run()
    {
        if(EditorApplication.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        const string folder="Assets/Resources/Encounter/v054";
        const string path="Assets/Resources/Encounter/v006/SahurGameplay.controller";
        if(!File.Exists("ArtSource/Encounter/v054/native-review.json"))throw new Exception("Actual native review is required before a runtime trial");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        var states=controller.layers[0].stateMachine.states.Select(s=>s.state).ToArray();
        var preserved=states.ToDictionary(s=>s.name,s=>s.motion);
        var surf=states.Single(s=>s.name=="Wreck Surf");var tree=surf.motion as BlendTree;
        if(tree==null || !surf.timeParameterActive || surf.timeParameter!="BoardStrokePhase")throw new Exception("Expected original physical stroke clock");
        var children=tree.children;var otherChildren=children.Where(c=>c.position!=Vector2.up && c.position!=Vector2.down).ToArray();
        if(children.Count(c=>c.position==Vector2.up)!=1 || children.Count(c=>c.position==Vector2.down)!=1)throw new Exception("Unexpected paddle blend tree");
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v054");
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"/PreviousSahurGameplay.controller")==null && !AssetDatabase.CopyAsset(path,folder+"/PreviousSahurGameplay.controller"))throw new Exception("Native versioned backup failed");
        AnimationClip Clip(string name)
        {
            string destination=folder+"/"+name;
            if(!AssetDatabase.IsValidFolder(destination))
            {
                string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,destination);
                if(!string.IsNullOrEmpty(error))throw new Exception(error);
            }
            var clip=AssetDatabase.LoadAllAssetsAtPath(destination+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            if(!clip.isHumanMotion || !clip.isLooping || Mathf.Abs(clip.length-1.2f)>.001f || clip.frameRate!=60)throw new Exception("Unexpected native paddle clip");
            return clip;
        }
        var forward=Clip("PLAYER_BoardPaddle_v054");var reverse=Clip("PLAYER_BoardPaddleReverse_v054");
        for(int i=0;i<children.Length;i++)
        {
            if(children[i].position==Vector2.up)children[i].motion=forward;
            if(children[i].position==Vector2.down)children[i].motion=reverse;
        }
        tree.children=children;EditorUtility.SetDirty(tree);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        if(states.Any(s=>s.motion!=preserved[s.name]))throw new Exception("An unrelated state motion changed");
        var retained=tree.children.Where(c=>c.position!=Vector2.up && c.position!=Vector2.down).ToArray();
        if(retained.Length!=otherChildren.Length || retained.Where((c,i)=>c.motion!=otherChildren[i].motion).Any())throw new Exception("An unrelated board blend changed");
        var result=new {forward=forward.name,reverse=reverse.name,seconds=forward.length,fps=forward.frameRate,originalWalkRunRollRetained=true,otherStateMotionsRetained=true,otherBoardMotionsRetained=true,nativeBackup=folder+"/PreviousSahurGameplay.controller",status="Reviewed SECOND PASS integration trial; actual physical controls regression pending"};
        File.WriteAllText("ArtSource/Encounter/v054/install-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
