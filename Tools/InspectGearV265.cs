using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class InspectGearV265 {
 public static object Run(){var result=Resources.LoadAll<Mavis.EquipmentItem>("Equipment/Nailong").Select(i=>new{i.id,i.displayName,i.slot,model=i.worldModel?AssetDatabase.GetAssetPath(i.worldModel):null,renderers=i.worldModel?i.worldModel.GetComponentsInChildren<Renderer>(true).Select(r=>new{r.name,bounds=r.bounds.ToString("F5"),local=r.localBounds.ToString("F5"),mesh=r.GetComponent<MeshFilter>()?.sharedMesh?.name,meshPath=AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh),scale=r.transform.localScale.ToString("F4"),rotation=r.transform.localEulerAngles.ToString("F4")}).ToArray():null}).ToArray();File.WriteAllText(".codex/combat-v265/gear-inspect.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;}
}

