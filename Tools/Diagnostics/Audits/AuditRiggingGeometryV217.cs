using System.IO;
using UnityEngine;
public static class AuditRiggingGeometryV217
{
    public static object Start(string take)
    {
        var observer=new GameObject("Native rigging geometry audit v217").AddComponent<OpeningRiggingGeometryV217>();observer.take=take;
        return new{started=true,take};
    }
}
[DefaultExecutionOrder(32600)]
public sealed class OpeningRiggingGeometryV217:MonoBehaviour
{
    public string take;
    void LateUpdate()
    {
        var b=Object.FindFirstObjectByType<Story1WreckBattle>();
        if(b==null||!b.RedesignActive||b.RedesignClock>=3)return;
        var result=b.DiagnosticPrepareRiggingGeometry();
        var text=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented);
        var folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,".codex","encounter-redesign-20261017",take);
        Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"rigging-geometry-v217.json"),text);
        if(text.Contains("true"))enabled=false;
    }
}
