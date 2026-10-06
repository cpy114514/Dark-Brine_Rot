using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupStory1WreckTraversal
{
    public static object Setup()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before editing animation assets.");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller");
        var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx").OfType<AnimationClip>().ToArray();
        var machine = controller.layers[0].stateMachine;
        AnimatorState Add(string name, string clip, float speed)
        {
            var motion = clips.Single(c => c.name == clip);
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
            state.motion = motion; state.speed = speed; state.writeDefaultValues = true;
            EditorUtility.SetDirty(state);
            return state;
        }
        Add("Wreck Climb", "Armature|ClimbUp_1m", .6666667f / .95f);
        Add("Wreck Surf", "Armature|Idle_Shield_Loop", 1f);
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
        return new { climbAnimation = "ClimbUp_1m", climbSeconds = .95f, surfAnimation = "Idle_Shield_Loop" };
    }
}
