// One-shot asset refresh trigger for the promo capture pipeline.
// New files are picked up by the asset database more reliably than edits to
// existing ones when the editor refresh mode is "refresh on focus".
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PromoRefreshTrigger
{
    static PromoRefreshTrigger()
    {
        Debug.Log("[promo] capture hook loaded");
    }
}
