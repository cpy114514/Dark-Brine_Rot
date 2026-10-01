using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Mavis;

public static class VerifyEnemyLockOn
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task<object> Verify()
    {
        Check(Application.isPlaying, "Requires play mode");
        var savedCamera = Camera.main;
        if (savedCamera != null) savedCamera.gameObject.SetActive(false);
        var camObject = new GameObject("Isolated lock camera", typeof(Camera)); camObject.tag = "MainCamera";
        var camera = camObject.GetComponent<Camera>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var player = UnityEngine.Object.Instantiate(prefab, new Vector3(0, 500, 0), Quaternion.identity);
        var centered = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centered.name = "Centered test enemy"; centered.tag = "Enemy";
        centered.transform.position = new Vector3(0, 501, 12); centered.AddComponent<Health>();
        var nearer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nearer.name = "Side test enemy"; nearer.tag = "Enemy";
        nearer.transform.position = new Vector3(3, 501, 8); nearer.AddComponent<Health>();
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0, 501, 6); wall.transform.localScale = new Vector3(3, 5, .5f); wall.SetActive(false);
        var mouse = InputSystem.AddDevice<Mouse>();
        float scale = Time.timeScale; var cursor = Cursor.lockState; bool visible = Cursor.visible;
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        async Task Click()
        {
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Middle)); await Task.Delay(120);
            InputSystem.QueueStateEvent(mouse, new MouseState()); await Task.Delay(80);
        }
        void ResetCamera() { camera.transform.position = new Vector3(0, 501, -4); camera.transform.rotation = Quaternion.identity; }
        try
        {
            await Task.Delay(80);
            var movement = player.GetComponent<ThirdPersonPlayerController>(); movement.enabled = false;
            var lockOn = player.GetComponent<EnemyLockOn>(); Check(lockOn != null, "Lock-on not auto-installed");
            ResetCamera(); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; Physics.SyncTransforms();
            await Click();
            Check(lockOn.Target == centered.transform, "Middle button did not prioritize screen center");
            Check(GameObject.Find("Enemy lock-on marker") != null, "Target marker missing");
            await Click(); Check(!lockOn.IsLocked, "Second middle click did not unlock");
            wall.SetActive(true); Physics.SyncTransforms();
            lockOn.Toggle(camera); Check(lockOn.Target != centered.transform, "Locked through wall"); lockOn.Clear();
            wall.SetActive(false); Physics.SyncTransforms(); lockOn.Toggle(camera);
            centered.GetComponent<Health>().currentHealth = 0;
            lockOn.Validate(0f); Check(!lockOn.IsLocked, "Dead target remained locked");
            centered.GetComponent<Health>().currentHealth = 100;
            lockOn.Toggle(camera); centered.transform.position += Vector3.forward * 50f;
            lockOn.Validate(0f); Check(!lockOn.IsLocked, "Out-of-range target retained");
            centered.transform.position = new Vector3(0, 501, 12); Physics.SyncTransforms(); lockOn.Toggle(camera);
            wall.SetActive(true); Physics.SyncTransforms(); lockOn.Validate(.2f);
            Check(lockOn.IsLocked, "Brief obstruction immediately unlocked");
            lockOn.Validate(.7f); Check(!lockOn.IsLocked, "Persistent obstruction retained lock");
            wall.SetActive(false); Physics.SyncTransforms(); lockOn.Toggle(camera);
            var map = player.GetComponent<IslandMapUI>(); map.SetOpen(true);
            await Click(); Check(lockOn.IsLocked, "Map input toggled combat lock"); map.SetOpen(false);
            await Task.Delay(100); ResetCamera();
            player.transform.rotation = Quaternion.Euler(0, 90, 0);
            movement.BeginAttackFacing();
            Check(Vector3.Angle(player.transform.forward, Vector3.forward) < .1f, "Attack did not aim at locked enemy");
            centered.transform.position = new Vector3(12, 501, 0);
            Physics.SyncTransforms();
            movement.BeginAttackFacing();
            Check(Vector3.Angle(player.transform.forward, Vector3.forward) < .1f, "Combo heading changed midway");
            movement.EndAttackFacing(true);
            typeof(ThirdPersonPlayerController).GetField("playerCamera", Private).SetValue(movement, camera);
            typeof(ThirdPersonPlayerController).GetField("yaw", Private).SetValue(movement, 0f);
            movement.cameraCollision = false;
            typeof(ThirdPersonPlayerController).GetMethod("LateUpdate", Private).Invoke(movement, null);
            Check((float)typeof(ThirdPersonPlayerController).GetField("yaw", Private).GetValue(movement) > 0f,
                "Camera does not follow target: target=" + lockOn.Target + " dt=" + Time.deltaTime + " timeScale=" + Time.timeScale + " blocks=" + SahurLoadoutUI.BlocksInput + " aim=" + lockOn.AimPoint + " player=" + player.transform.position);
            player.GetComponent<PlayerHealth>().ApplyDamage(10000f, player.transform.position);
            Check(!lockOn.IsLocked, "Death did not clear lock"); player.GetComponent<PlayerDeathRespawn>().Respawn();
            await Task.Delay(100); movement.RestoreSavedPose(new Vector3(0, 500, 0), Quaternion.identity);
            ResetCamera(); centered.transform.position = new Vector3(0, 501, 12); Physics.SyncTransforms();
            Check(lockOn.Toggle(camera), "Lock cannot be reacquired");
            UnityEngine.Object.Destroy(centered); await Task.Delay(100); lockOn.Validate(0f);
            Check(!lockOn.IsLocked, "Destroyed target retained lock");
            return new { success = true, middleToggle = true, centerPriority = true, wallBlocks = true,
                deathRangeAndDestructionRelease = true, obstructionGrace = true, mapInputSafe = true,
                attackFacing = true, comboHeadingPreserved = true, cameraTracking = true };
        }
        finally
        {
            UnityEngine.Object.Destroy(player); UnityEngine.Object.Destroy(centered); UnityEngine.Object.Destroy(nearer);
            UnityEngine.Object.Destroy(wall); UnityEngine.Object.Destroy(camObject); InputSystem.RemoveDevice(mouse);
            if (savedCamera != null) savedCamera.gameObject.SetActive(true);
            Time.timeScale = scale; Cursor.lockState = cursor; Cursor.visible = visible;
        }
    }
}
