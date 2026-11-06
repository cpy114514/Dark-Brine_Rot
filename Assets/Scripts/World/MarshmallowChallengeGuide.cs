using System.Collections;
using Mavis;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The fairy introduces the moving-marshmallow course when approached.</summary>
[DisallowMultipleComponent]
public sealed class MarshmallowChallengeGuide : MonoBehaviour
{
    public float triggerRadius = 8f;
    public float resetRadius = 15f;
    public float maxHeightDifference = 5f;

    static readonly SecondIslandDialogue.Line[] Rules = {
        new SecondIslandDialogue.Line(true, "Welcome! Can you reach the end of the moving marshmallows?"),
        new SecondIslandDialogue.Line(true, "They move and sometimes swap places. Watch them carefully before you jump."),
        new SecondIslandDialogue.Line(true, "Low gravity makes it easier to jump across. Press B to change gravity if you have the wristband."),
        new SecondIslandDialogue.Line(true, "If you fall from rows 1 to 9, you return to the first row. From row 10 onward, you go back five rows."),
        new SecondIslandDialogue.Line(true, "After three falls without new progress, the platforms slow down and a rescue point helps you catch up."),
        new SecondIslandDialogue.Line(true, "Collect five optional stars along the way. Some are only on one side, so choose your path!"),
        new SecondIslandDialogue.Line(true, "Reach the last row to find the Skyboard and speed scrolls. Good luck!")
    };

    ThirdPersonPlayerController player;
    CharacterController body;
    PlayerHealth health;
    Canvas canvas;
    bool inConversation, needsExit, ownsLocks;
    public bool InConversation => inConversation;
    public static bool AnyConversation { get; private set; }

    void Update()
    {
        if (canvas) canvas.enabled = !GameUITheme.ModalOpen;
        if (!player)
        {
            player = FindFirstObjectByType<ThirdPersonPlayerController>();
            if (player) { body = player.GetComponent<CharacterController>(); health = player.GetComponent<PlayerHealth>(); }
        }
        if (!player || !body) return;
        Vector3 delta = player.transform.position - transform.position;
        float distance = new Vector2(delta.x, delta.z).magnitude;
        if (inConversation)
        {
            if (!player.isActiveAndEnabled || !body.enabled || health && health.currentHealth <= 0)
            { StopAllCoroutines(); EndConversation(); }
            return;
        }
        if (needsExit)
        {
            if (distance >= resetRadius) needsExit = false;
            return;
        }
        if (distance > triggerRadius || Mathf.Abs(body.bounds.min.y - transform.position.y) > maxHeightDifference || !body.enabled || !body.isGrounded ||
            !player.isActiveAndEnabled || player.ExternalControlLock || player.ExternalMovementLock ||
            health && health.currentHealth <= 0 || GameUITheme.ModalOpen || Time.timeScale <= 0) return;
        StartCoroutine(Explain());
    }

    IEnumerator Explain()
    {
        inConversation = needsExit = ownsLocks = AnyConversation = true;
        player.ExternalControlLock = true;
        player.ExternalMovementLock = true;
        var screen = new GameObject("Marshmallow challenge conversation", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        screen.transform.SetParent(transform, false);
        canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        GameUITheme.Scale(canvas);
        var dialogue = screen.AddComponent<SecondIslandDialogue>();
        yield return dialogue.Play(screen.transform, Rules);
        // The dismiss button must not also cause a jump or attack.
        yield return null;
        EndConversation();
    }

    void EndConversation()
    {
        if (canvas) Destroy(canvas.gameObject);
        canvas = null;
        if (ownsLocks && player)
        {
            player.ExternalMovementLock = false;
            player.ExternalControlLock = false;
        }
        ownsLocks = inConversation = AnyConversation = false;
    }

    void OnDisable() { StopAllCoroutines(); EndConversation(); }
}
