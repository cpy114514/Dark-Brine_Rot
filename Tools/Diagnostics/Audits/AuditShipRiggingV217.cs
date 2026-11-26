using System.Linq;
using UnityEngine;
public static class AuditShipRiggingV217
{
    public static object Inspect()
    {
        var owner=Object.FindFirstObjectByType<Story1SharkCollisionSequence>();var asset=Resources.Load<Story1ShipWreckAsset>("Story1Wreck/ShipWreckPieces");
        var matrix=Story1ShipWreckAsset.ModelToWorld(owner.ship.transform);
        var sails=Resources.Load<ShipSailSet>("Sailing/v039/ShipSails");
        return new{source=asset.sourceSha256,sails=sails.panels.Select(p=>new{p.node,mesh=p.mesh.name,p.mesh.vertexCount}).ToArray(),
            originals=owner.ship.GetComponentsInChildren<MeshFilter>(true).Select(m=>new{m.name,mesh=m.sharedMesh.name,m.sharedMesh.vertexCount,triangles=m.sharedMesh.triangles.Length,submeshes=m.sharedMesh.subMeshCount,m.sharedMesh.isReadable,bounds=m.GetComponent<Renderer>().bounds.ToString("F2"),material=m.GetComponent<Renderer>().sharedMaterial.name}).ToArray(),
            pieces=asset.details.GroupBy(p=>p.sourceObject).Select(g=>new{source=g.Key,count=g.Count(),names=g.Select(p=>p.name).ToArray(),first=g.Select(p=>new{p.name,centre=matrix.MultiplyPoint3x4(p.sourceCenter).ToString("F2"),size=Vector3.Scale(p.sourceSize,owner.ship.transform.lossyScale*.01f).ToString("F2"),material=p.material.name}).Take(4).ToArray()}).ToArray()};
    }
}
