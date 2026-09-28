using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Story1 controls for Sahur while the ship carries the character.</summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class ShipboardSahurController : MonoBehaviour
{
    [Header("Movement")]
    [Min(0.1f)] public float walkSpeed = 5f;
    [Min(1f)] public float sprintMultiplier = 1.6f;
    [Min(0.1f)] public float acceleration = 20f;
    [Min(0.1f)] public float gravity = 28f;
    [Min(0.1f)] public float jumpHeight = 1.4f;
    [Min(1f)] public float turnSpeed = 540f;

    [Header("Character camera")]
    [Min(1f)] public float cameraDistance = 8f;
    [Min(1f)] public float cameraHeight = 2.3f;
    [Min(0.01f)] public float mouseSensitivity = 0.18f;
    [Min(0.01f)] public float zoomSensitivity = 0.01f;
    [Min(0.1f)] public float cameraSharpness = 12f;

    [Header("Ship")]
    public Transform ship;

    CharacterController controller;
    Animator animator;
    Camera viewCamera;
    ShipFollowCamera shipCamera;
    ShipLadder[] ladders;
    ShipLadder activeLadder;
    Vector3 spawnShipLocalPosition;
    Vector3 lastShipLocalPosition;
    Vector3 planarVelocity;
    float verticalSpeed;
    float yaw;
    float pitch = 14f;
    bool wideView;
    bool cameraInitialized;
    int jumpState;
    float landingTimer;
    float ladderDistance;
    int ladderDirection;
    bool groundedLastFrame;

    static readonly int SpeedId = Animator.StringToHash("Speed");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (ship == null) ship = transform.parent;
        if (ship != null)
        {
            spawnShipLocalPosition = ship.InverseTransformPoint(transform.position);
            lastShipLocalPosition = spawnShipLocalPosition;
            ladders = ship.GetComponents<ShipLadder>();
        }

        // The imported animation curves must drive the model, not its movement root.
        Animator root = GetComponent<Animator>();
        Animator visual = transform.Find("Pbr Sahur Visual")?.GetComponent<Animator>();
        if (root != null && visual != null)
        {
            visual.enabled = false;
            visual.runtimeAnimatorController = root.runtimeAnimatorController;
            visual.avatar = root.avatar;
            visual.applyRootMotion = false;
            visual.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            root.enabled = false;
            visual.enabled = true;
            visual.Rebind();
            visual.Update(0f);
            animator = visual;
        }
        else animator = visual != null ? visual : root;

        Vector3 heading = ship != null ? Vector3.ProjectOnPlane(ship.right, Vector3.up) : transform.forward;
        yaw = Quaternion.LookRotation(heading, Vector3.up).eulerAngles.y;
    }

    void Start()
    {
        viewCamera = Camera.main;
        if (viewCamera != null)
        {
            shipCamera = viewCamera.GetComponent<ShipFollowCamera>();
            if (shipCamera != null) shipCamera.enabled = false;
        }
        if (animator != null) animator.Play("Locomotion", 0, 0f);
    }

    void Update()
    {
        // CharacterController does not inherit a moving parent's physics position
        // reliably. Keep Sahur independent and explicitly carry him with the deck.
        if (activeLadder == null && ship != null)
        {
            Vector3 carriedPosition = ship.TransformPoint(lastShipLocalPosition);
            controller.Move(carriedPosition - transform.position);
            lastShipLocalPosition = ship.InverseTransformPoint(transform.position);
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || PauseSettingsMenu.IsOpen)
        {
            if (activeLadder != null) PlaceLadderFeet();
            return;
        }

        if (keyboard.vKey.wasPressedThisFrame)
        {
            wideView = !wideView;
            if (shipCamera != null) shipCamera.enabled = wideView;
            if (!wideView) cameraInitialized = false;
        }

        if (!wideView && mouse != null)
        {
            if (mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -35f, 70f);
            }
            cameraDistance = Mathf.Clamp(cameraDistance - mouse.scroll.ReadValue().y * zoomSensitivity, 3.5f, 16f);
        }

        Vector2 input = Vector2.zero;
        if (GameInputSettings.Pressed(GameInputSettings.Action.Forward)) input.y += 1f;
        if (GameInputSettings.Pressed(GameInputSettings.Action.Back)) input.y -= 1f;
        if (GameInputSettings.Pressed(GameInputSettings.Action.Right)) input.x += 1f;
        if (GameInputSettings.Pressed(GameInputSettings.Action.Left)) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        float speed = walkSpeed * (GameInputSettings.Pressed(GameInputSettings.Action.Sprint) ? sprintMultiplier : 1f);
        Vector3 desired = (forward * input.y + right * input.x) * speed;

        if (activeLadder != null)
        {
            UpdateLadder();
            return;
        }
        if (TryStartLadder(desired))
        {
            UpdateLadder();
            return;
        }

        // The mouse controls the third-person heading even while standing still.
        // WASD moves relative to that heading; strafing or backing up no longer
        // turns Sahur away from the direction the player is looking.
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        planarVelocity = Vector3.MoveTowards(planarVelocity, desired, acceleration * Time.deltaTime);

        bool grounded = groundedLastFrame || controller.isGrounded;
        if (grounded && verticalSpeed < 0f) verticalSpeed = -2f;
        if (grounded && GameInputSettings.PressedThisFrame(GameInputSettings.Action.Jump))
        {
            verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
            jumpState = 1;
            if (animator != null) animator.CrossFadeInFixedTime("Jump Start", 0.06f);
        }
        verticalSpeed -= gravity * Time.deltaTime;
        CollisionFlags collision = controller.Move((planarVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
        groundedLastFrame = (collision & CollisionFlags.Below) != 0;

        if (jumpState == 1 && verticalSpeed <= 0f)
        {
            jumpState = 2;
            if (animator != null) animator.CrossFadeInFixedTime("Jump Loop", 0.08f);
        }
        if (jumpState > 0 && jumpState < 3 && (collision & CollisionFlags.Below) != 0)
        {
            jumpState = 3;
            landingTimer = 0.15f;
            if (animator != null) animator.CrossFadeInFixedTime("Land", 0.05f);
        }
        if (jumpState == 3)
        {
            landingTimer -= Time.deltaTime;
            if (landingTimer <= 0f)
            {
                jumpState = 0;
                if (animator != null) animator.CrossFadeInFixedTime("Locomotion", 0.08f);
            }
        }

        if (animator != null)
            animator.SetFloat(SpeedId, planarVelocity.magnitude, 0.08f, Time.deltaTime);

        // A missed jump or step off the rail should not strand the player in the ocean.
        if (ship != null && transform.position.y < ship.TransformPoint(spawnShipLocalPosition).y - 9f)
        {
            controller.enabled = false;
            transform.position = ship.TransformPoint(spawnShipLocalPosition);
            controller.enabled = true;
            planarVelocity = Vector3.zero;
            verticalSpeed = 0f;
            jumpState = 0;
            groundedLastFrame = false;
            if (animator != null) animator.CrossFadeInFixedTime("Locomotion", 0.08f);
        }
        if (ship != null) lastShipLocalPosition = ship.InverseTransformPoint(transform.position);
    }

    bool TryStartLadder(Vector3 desired)
    {
        if (ladders == null || desired.sqrMagnitude < 0.1f ||
            !GameInputSettings.Pressed(GameInputSettings.Action.Forward))
            return false;

        Vector3 feet = FeetPosition();
        foreach (ShipLadder ladder in ladders)
        {
            if (ladder == null || !ladder.enabled) continue;
            if (Vector3.Distance(feet, ladder.Bottom) <= ladder.entryDistance &&
                Vector3.Dot(desired.normalized, ladder.HorizontalUp) > 0.45f)
            {
                BeginLadder(ladder, true);
                return true;
            }
            if (Vector3.Distance(feet, ladder.DeckExit) <= ladder.entryDistance &&
                Vector3.Dot(desired.normalized, -ladder.HorizontalUp) > 0.45f)
            {
                BeginLadder(ladder, false);
                return true;
            }
        }
        return false;
    }

    void BeginLadder(ShipLadder ladder, bool ascending)
    {
        activeLadder = ladder;
        ladderDirection = ascending ? 1 : -1;
        ladderDistance = ascending ? 0f : ladder.Length;
        controller.enabled = false;
        planarVelocity = Vector3.zero;
        verticalSpeed = 0f;
        groundedLastFrame = false;
        jumpState = 0;
        if (animator != null) animator.CrossFadeInFixedTime("Locomotion", 0.08f);
        PlaceLadderFeet();
    }

    void UpdateLadder()
    {
        float input = (GameInputSettings.Pressed(GameInputSettings.Action.Forward) ? 1f : 0f) -
                      (GameInputSettings.Pressed(GameInputSettings.Action.Back) ? 1f : 0f);
        ladderDistance = Mathf.Clamp(ladderDistance +
            input * ladderDirection * activeLadder.climbSpeed * Time.deltaTime,
            0f, activeLadder.Length);
        PlaceLadderFeet();

        Vector3 horizontal = activeLadder.HorizontalUp * ladderDirection;
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(horizontal, Vector3.up), turnSpeed * Time.deltaTime);
        if (animator != null)
            animator.SetFloat(SpeedId, Mathf.Abs(input) * 1.5f, 0.08f, Time.deltaTime);

        bool reachedTop = ladderDistance >= activeLadder.Length - 0.001f &&
                          input * ladderDirection > 0f;
        bool reachedBottom = ladderDistance <= 0.001f &&
                             input * ladderDirection < 0f;
        if (!reachedTop && !reachedBottom) return;

        controller.enabled = true;
        activeLadder = null;
        verticalSpeed = -2f;
        lastShipLocalPosition = ship.InverseTransformPoint(transform.position);
    }

    Vector3 FeetPosition()
    {
        return transform.position + Vector3.up *
            ((controller.center.y - controller.height * 0.5f) * transform.lossyScale.y);
    }

    void PlaceLadderFeet()
    {
        transform.position = activeLadder.PointAt(ladderDistance) - Vector3.up *
            ((controller.center.y - controller.height * 0.5f) * transform.lossyScale.y);
        if (ship != null) lastShipLocalPosition = ship.InverseTransformPoint(transform.position);
    }

    void LateUpdate()
    {
        if (viewCamera == null) return;
        if (wideView)
        {
            // The ship overview has its own orbit camera. Follow its actual
            // horizontal view so the same mouse drag turns Sahur in this mode.
            Vector3 cameraForward = Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up);
            if (cameraForward.sqrMagnitude > 0.001f)
                yaw = Quaternion.LookRotation(cameraForward, Vector3.up).eulerAngles.y;
            if (activeLadder == null)
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return;
        }

        Vector3 feet = transform.position + Vector3.up *
            ((controller.center.y - controller.height * 0.5f) * transform.lossyScale.y);
        Vector3 focus = feet + Vector3.up * cameraHeight;
        Quaternion look = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desired = focus - look * Vector3.forward * cameraDistance;

        // Keep the camera in front of nearby ship parts without reacting to Sahur's colliders.
        Vector3 direction = desired - focus;
        float closest = direction.magnitude;
        foreach (RaycastHit hit in Physics.SphereCastAll(focus, 0.12f, direction.normalized,
                     closest, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            closest = Mathf.Min(closest, Mathf.Max(0.5f, hit.distance - 0.08f));
        }
        desired = focus + direction.normalized * closest;

        if (!cameraInitialized)
        {
            viewCamera.transform.SetPositionAndRotation(desired, look);
            cameraInitialized = true;
            return;
        }
        float blend = 1f - Mathf.Exp(-cameraSharpness * Time.deltaTime);
        viewCamera.transform.SetPositionAndRotation(
            Vector3.Lerp(viewCamera.transform.position, desired, blend),
            Quaternion.Slerp(viewCamera.transform.rotation, look, blend));
    }
}
