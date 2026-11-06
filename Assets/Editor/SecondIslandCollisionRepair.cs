using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adds precise physical or non-blocking pickup volumes to audited second-island assets.</summary>
public static class SecondIslandCollisionRepair
{
    const string ScenePath="Assets/Scenes/Second Island/Second Island.unity";
    static void AddBox(GameObject obj,bool trigger=false,Vector3? center=null,Vector3? size=null)
    {
        if(!obj)throw new Exception("Collision repair target is missing.");
        var box=obj.GetComponent<BoxCollider>();if(!box)box=obj.AddComponent<BoxCollider>();
        box.isTrigger=trigger;if(center.HasValue)box.center=center.Value;if(size.HasValue)box.size=size.Value;
    }
    static void Modify(string path,Action<GameObject> change)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try{change(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static string Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.path!=ScenePath || scene.isDirty)throw new Exception("Open the clean second island in edit mode before repairing collision.");
        Modify("Assets/Game/Prefabs/Characters/NealFairy/Neal Fairy.prefab",root=>{
            var hover=root.GetComponent<NealFairyHover>();if(!hover || !hover.visual)throw new Exception("Fairy rig missing.");
            var capsule=hover.visual.GetComponent<CapsuleCollider>();if(!capsule)capsule=hover.visual.gameObject.AddComponent<CapsuleCollider>();
            capsule.isTrigger=true;capsule.center=new Vector3(0,.57f,0);capsule.height=1.2f;capsule.radius=.28f;
        });
        Modify("Assets/Resources/SecondIslandCandyHouse/Candy Cottage.prefab",root=>{
            var pickups=root.GetComponentsInChildren<CandyHouseHealingPickup>(true);if(pickups.Length!=3)throw new Exception("Unexpected healing pickup count.");
            foreach(var pickup in pickups)AddBox(pickup.gameObject,true,new Vector3(0,.93f,0),new Vector3(1.6f,1.4f,.95f));
        });
        Modify("Assets/Resources/SecondIslandCandyHunt/Candy Hunt.prefab",root=>{
            var hunt=root.GetComponent<CandyHuntQuest>();if(!hunt || hunt.candies.Length!=10)throw new Exception("Candy hunt is missing.");
            foreach(var candy in hunt.candies){if(!candy)throw new Exception("Candy hunt pickup missing.");AddBox(candy.gameObject,true,Vector3.zero,new Vector3(3.4f,1.55f,1.55f));}
        });
        Modify("Assets/Game/Prefabs/Environment/PlainMarshmallows/Marshmallow Hop Challenge.prefab",root=>{
            foreach(string n in new[]{"Start sign","Finish sign"})
            {
                var sign=root.transform.Find(n);if(!sign)throw new Exception(n+" missing.");
                AddBox(sign.gameObject);var board=sign.Find("Sign board");if(!board)throw new Exception(n+" board missing.");AddBox(board.gameObject);
            }
        });
        var blackboard=GameObject.Find("Marshmallow Challenge Blackboard");if(!blackboard)throw new Exception("Challenge blackboard missing.");
        foreach(string n in new[]{"Wooden frame","Left post","Right post"})
        {
            var part=blackboard.transform.Find(n);if(!part)throw new Exception("Blackboard part missing: "+n);
            if(!part.GetComponent<BoxCollider>())Undo.AddComponent<BoxCollider>(part.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save blackboard colliders.");
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        return "Added solid blackboard and course-sign colliders, fairy trigger volumes, and trigger volumes to ten quest candies and three healing packs.";
    }
}
