using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Mavis;
public static class ReadDrawMeshV254
{
 public static object Prepare()
 {
  var rig=Resources.Load<EquipmentItem>("Equipment/Capri/CapriTwinBlades").worldModel.GetComponent<CapriTwinBladeEquipment>().handRig;
  Mesh copy=UnityEngine.Object.Instantiate(rig.skinnedMesh);
  try
  {
   var edit=new SerializedObject(copy);edit.FindProperty("m_IsReadable").boolValue=true;edit.ApplyModifiedPropertiesWithoutUndo();
   var weights=copy.boneWeights;var tris=copy.triangles;
   if(weights.Length!=copy.vertexCount||tris.Length==0)throw new Exception("Native clone CPU data unavailable");
   using(var writer=new BinaryWriter(File.Create(".codex/draw-retrieve-v254/finger-topology.bin")))
   {
    writer.Write(weights.Length);foreach(var w in weights){writer.Write(w.boneIndex0);writer.Write(w.weight0);writer.Write(w.boneIndex1);writer.Write(w.weight1);writer.Write(w.boneIndex2);writer.Write(w.weight2);writer.Write(w.boneIndex3);writer.Write(w.weight3);}
    writer.Write(tris.Length);foreach(int i in tris)writer.Write(i);
   }
   return new {copy.vertexCount,weights=weights.Length,triangles=tris.Length/3,sourceReadable=rig.sourceMesh.isReadable,sourceVertices=rig.sourceMesh.vertexCount};
  }
  finally{UnityEngine.Object.DestroyImmediate(copy);}
 }
}
