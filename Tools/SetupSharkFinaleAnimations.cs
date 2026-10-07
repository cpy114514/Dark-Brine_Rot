using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class SetupSharkFinaleAnimations
{
    public static object Setup()
    {
        if(EditorApplication.isPlaying)throw new Exception("Requires Edit Mode.");
        const string sprayPath="Assets/Resources/Story1Wreck/WreckWaterSpray.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(sprayPath)==null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")){name="Wreck Water Spray"},sprayPath);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx").OfType<AnimationClip>().ToArray();
        var machine=controller.layers[0].stateMachine;
        var standard=AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/QuaterniusUniversalAnimations/Quaternius_Universal_Standard.fbx").OfType<AnimationClip>().ToArray();
        void State(string name,AnimationClip clip,float speed)
        {
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);
            state.motion=clip;state.speed=speed;state.writeDefaultValues=true;
            // Cinematic recovery is directed by the sequence, without gameplay exits.
            foreach(var transition in state.transitions)state.RemoveTransition(transition);
            EditorUtility.SetDirty(state);
        }
        State("Finale Guard",standard.Single(c=>c.name=="Rig|Sword_Idle"),1);
        State("Finale Block",clips.Single(c=>c.name=="Armature|Sword_Block"),1.5f);
        State("Finale Stagger",standard.Single(c=>c.name=="Rig|Hit_Chest"),.8f);
        // The director samples existing attack/movement clips in its own graph.
        // Only add states needed to retain additional clips in the runtime controller.
        foreach(var obsolete in new[]{"Finale Walk","Finale Strike","Finale Dodge","Finale Throw","Finale Jump Strike","Finale Chop"})
        {
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==obsolete);
            if(state!=null)machine.RemoveState(state);
        }
        // A recorded fall has the impact and bracing absent from reversed standing up.
        var death=standard.Single(c=>c.name=="Rig|Death01");
        State("Finale Knockdown",death,death.length/2.1f);
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        return new{guard=true,stagger=true,authoredKnockdownClip=death.name,knockdownSeconds=2.1f};
    }
}
