using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ConfigureStorySailingFeedback
{
    public static object Run()
    {
        if (Application.isPlaying) throw new Exception("Stop Play mode before saving sailing speed.");
        var ship = UnityEngine.Object.FindFirstObjectByType<ShipSailingMotion>();
        if (ship == null || !ship.gameObject.scene.path.EndsWith("Story1.unity"))
            throw new Exception("Story1 must already be loaded.");
        Undo.RecordObject(ship, "Improve Story1 sailing speed");
        float previous = ship.forwardSpeed;
        ship.forwardSpeed = 8f;
        EditorUtility.SetDirty(ship);
        EditorSceneManager.MarkSceneDirty(ship.gameObject.scene);
        EditorSceneManager.SaveScene(ship.gameObject.scene);
        return new { previousSpeed = previous, speed = ship.forwardSpeed, hullLength = ship.hullLength };
    }
}
