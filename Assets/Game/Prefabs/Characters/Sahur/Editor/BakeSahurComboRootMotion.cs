using UnityEditor;
using UnityEngine;

namespace Mavis
{
    /// <summary>Legacy menu kept for projects that used the old baked-step workflow.</summary>
    public static class BakeSahurComboRootMotion
    {
        [MenuItem("Mavis/Sahur/Legacy/Bake Sword-And-Shield Root Motion")]
        public static void Run()
        {
            Debug.LogWarning("Sahur now uses the imported Animator root motion directly. " +
                "Run Mavis/Sahur/Setup Sword-And-Shield Combo to restore its clips and grip.");
        }
    }
}
