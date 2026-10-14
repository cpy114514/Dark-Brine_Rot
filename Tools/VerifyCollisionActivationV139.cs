using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class VerifyCollisionActivationV139
{
    public static object Check()
    {
        if(!Application.isPlaying)throw new Exception("Native Play Mode required for an isolated local physics scene");
        var scene=SceneManager.CreateScene("Isolated collision activation v139",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        GameObject actor=null,piece=null;
        try
        {
            actor=new GameObject("Diagnostic animated body",typeof(CapsuleCollider));SceneManager.MoveGameObjectToScene(actor,scene);
            piece=new GameObject("Diagnostic original cut hull",typeof(Rigidbody));SceneManager.MoveGameObjectToScene(piece,scene);
            var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
            var geometry=Story1ConvexMeshParts.Get(asset.decks[0].mesh)[0];
            var mesh=piece.AddComponent<MeshCollider>();mesh.convex=true;mesh.sharedMesh=geometry;
            var body=actor.GetComponent<CapsuleCollider>();
            Physics.IgnoreCollision(body,mesh,true);bool active=Physics.GetIgnoreCollision(body,mesh);
            piece.SetActive(false);Physics.IgnoreCollision(body,mesh,true);piece.SetActive(true);
            bool setWhileInactiveThenReleased=Physics.GetIgnoreCollision(body,mesh);
            Physics.IgnoreCollision(body,mesh,true);bool setAfterRelease=Physics.GetIgnoreCollision(body,mesh);
            body.enabled=false;body.enabled=true;bool afterBodyReenabled=Physics.GetIgnoreCollision(body,mesh);
            var result=new{native=true,isolatedPhysicsScene=true,originalCutMesh=true,active,setWhileInactiveThenReleased,setAfterRelease,afterBodyReenabled};
            File.WriteAllText(".codex/opening-optimization-20261012/collision-activation-v139.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally{if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);if(piece!=null)UnityEngine.Object.DestroyImmediate(piece);SceneManager.UnloadSceneAsync(scene);}
    }
}
