using UnityEngine;
public static class AuditShipRiggingMappingV217
{
    public static object Inspect()
    {
        var s=Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
        return Story1WreckBattle.DiagnosticRiggingMeshes(s.ship.transform);
    }
}
