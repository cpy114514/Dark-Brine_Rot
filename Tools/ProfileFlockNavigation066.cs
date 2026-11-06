using System;
using System.IO;
using UnityEngine;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
public static class ProfileFlockNavigation066
{
 public static object Run(){var s=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var g=new GameObject("Owned flock navigation measurement");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g,s);var surface=g.AddComponent<NavMeshSurface>();g.transform.position=new Vector3(174.28f,48.88f,524.98f);surface.collectObjects=CollectObjects.Volume;surface.center=Vector3.up*8;surface.size=new Vector3(72,36,72);surface.useGeometry=UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;var clock=System.Diagnostics.Stopwatch.StartNew();try{surface.BuildNavMesh();var result=new{synchronousMilliseconds=clock.Elapsed.TotalMilliseconds,vertices=UnityEngine.AI.NavMesh.CalculateTriangulation().vertices.Length};File.WriteAllText(".codex/freeze066/nav-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;}finally{var data=surface.navMeshData;surface.RemoveData();UnityEngine.Object.DestroyImmediate(g);if(data)UnityEngine.Object.DestroyImmediate(data);EditorSceneManager.CloseScene(s,true);}}
}
