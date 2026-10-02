using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>Continues the shark encounter with unconscious wave drift onto First Island.</summary>
[DisallowMultipleComponent]
public sealed class Story1OceanAwakening : MonoBehaviour
{
    public ThirdPersonPlayerController player;
    public Camera storyCamera;
    public GameObject ship;
    public GameObject shark;
    public GameObject looseShipDecoration;
    public Story1FloatingPlank plank;
    public bool driftToFirstIsland = true;
    [Min(0f)] public float blackHoldSeconds = 3f;
    [Min(0.01f)] public float eyesOpenSeconds = 2.4f;
    [Min(0.01f)] public float climbSeconds = 1.15f;
    public bool HasBegun { get; private set; }
    public bool HasRecovered { get; private set; }

    public void BeginBlackout(Image blackout, Vector3 impactPosition, Vector3 heading)
    {
        if (HasBegun) return;
        if (player == null || storyCamera == null || (!driftToFirstIsland && plank == null) || blackout == null)
        {
            Debug.LogError("Story1 ocean awakening is missing a scene reference.", this);
            return;
        }
        HasBegun = true;
        StartCoroutine(Recover(blackout, impactPosition, heading));
    }

    IEnumerator Recover(Image blackout, Vector3 impactPosition, Vector3 heading)
    {
        player.enabled = false;
        var capsule = player.GetComponent<CharacterController>();
        if (capsule != null) capsule.enabled = false;
        var passenger = player.GetComponent<Story1SahurPassenger>();
        if (passenger != null) passenger.enabled = false;
        var follow = storyCamera.GetComponent<ShipFollowCamera>();
        if (follow != null) follow.enabled = false;
        blackout.enabled = true;
        blackout.color = Color.black;
        yield return new WaitForSeconds(Mathf.Max(0f, blackHoldSeconds));

        if (driftToFirstIsland)
        {
            const string islandScene = "Assets/Scenes/First Island/Main.unity";
            if (Application.CanStreamedLevelBeLoaded(islandScene))
            {
                Mavis.GameSaveManager.CancelPendingContinue();
                SceneManager.LoadSceneAsync(islandScene, LoadSceneMode.Single);
                yield break;
            }
            Debug.LogError("First Island/Main is missing from Build Settings.", this);
            yield break;
        }

        if (ship != null) ship.SetActive(false);
        if (shark != null) shark.SetActive(false);
        if (looseShipDecoration != null) looseShipDecoration.SetActive(false);
        heading = Vector3.ProjectOnPlane(heading, Vector3.up).normalized;
        if (heading.sqrMagnitude < 0.01f) heading = Vector3.forward;
        var ocean = FindFirstObjectByType<OceanWorld>();
        Vector3 wreckPosition = impactPosition + heading * 12f;
        wreckPosition.y = (ocean != null ? ocean.SampleSurfaceHeight(wreckPosition, Time.time) : player.seaLevel) + plank.freeboard;
        plank.transform.SetPositionAndRotation(wreckPosition, Quaternion.LookRotation(heading));
        plank.gameObject.SetActive(true);
        Physics.SyncTransforms();

        Vector3 swimPosition = wreckPosition - heading * 4.5f - Vector3.up * 0.7f;
        player.transform.SetPositionAndRotation(swimPosition, Quaternion.LookRotation(heading));
        for (float elapsed = 0f; elapsed < eyesOpenSeconds; elapsed += Time.deltaTime)
        {
            storyCamera.transform.SetPositionAndRotation(swimPosition - heading * 6f + Vector3.up * 2.5f,
                Quaternion.LookRotation(heading + Vector3.down * 0.15f));
            blackout.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, eyesOpenSeconds)));
            yield return null;
        }
        blackout.color = Color.clear;
        blackout.enabled = false;

        float footOffset = capsule != null ? (capsule.height * 0.5f - capsule.center.y) * Mathf.Abs(player.transform.lossyScale.y) : 0f;
        for (float elapsed = 0f; elapsed < climbSeconds; elapsed += Time.deltaTime)
        {
            Vector3 destination = DeckPosition(footOffset);
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, climbSeconds));
            player.transform.position = Vector3.Lerp(swimPosition, destination, progress) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.5f);
            yield return null;
        }
        player.RestoreSavedPose(DeckPosition(footOffset), Quaternion.LookRotation(heading));
        if (capsule != null) capsule.enabled = true;
        player.enabled = true;
        HasRecovered = true;
    }

    Vector3 DeckPosition(float footOffset)
    {
        Vector3 position = plank.deck != null ? plank.deck.bounds.center : plank.transform.position;
        position.y = (plank.deck != null ? plank.deck.bounds.max.y : position.y) + footOffset + 0.05f;
        return position;
    }
}
