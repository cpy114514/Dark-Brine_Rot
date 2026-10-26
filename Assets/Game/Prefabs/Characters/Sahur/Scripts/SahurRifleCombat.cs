using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>M4A1 magazine, automatic fire and swept projectiles. All visual objects are reused.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(1300)]
    public sealed class SahurRifleCombat : MonoBehaviour
    {
        public const int MagazineCapacity = 30, ReserveCapacity = 210;
        public const int StartingReserveRounds = 90;
        public const float BulletDamage = 20f, EmptyReloadSeconds = 7f;
        [Min(1)] public float roundsPerSecond = 10f;
        [Min(20)] public float bulletSpeed = 240f;
        [Min(10)] public float range = 180f;
        public int Rounds { get; private set; } = MagazineCapacity;
        public int ReserveRounds { get; private set; } = StartingReserveRounds;
        public int ShotsFired { get; private set; }
        public int Hits { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadDuration { get; private set; }
        public float ReloadRemaining { get; private set; }
        public float ReloadProgress => IsReloading ? 1f - ReloadRemaining / Mathf.Max(.001f, ReloadDuration) : 0;
        public bool FirePoseActive => !IsReloading && CanOperate && Time.time < firePoseUntil;
        public bool IsAiming => CanOperate && !IsReloading && movement.RifleAimRequested;
        public static float ReloadSecondsFor(int remaining) => EmptyReloadSeconds * (MagazineCapacity - Mathf.Clamp(remaining, 0, MagazineCapacity)) / MagazineCapacity;

        sealed class Bullet
        {
            public Vector3 position, direction;
            public float traveled;
            public bool active;
            public LineRenderer line;
        }
        struct Impact { public LineRenderer line; public float until; }
        readonly Bullet[] bullets = new Bullet[32];
        readonly Impact[] impacts = new Impact[12];
        readonly RaycastHit[] castHits = new RaycastHit[128];
        readonly Collider[] overlaps = new Collider[64];
        SahurWeaponLoadout loadout;
        ThirdPersonPlayerController movement;
        PlayerHealth health;
        Vector3 cachedAimPoint;
        int aimFrame = -1;
        public static readonly Vector3 MuzzleLocal = new Vector3(-.425f, .037f, 0);
        public Ray AimRay { get { if (!viewer) viewer = Camera.main; return viewer ? viewer.ViewportPointToRay(new Vector3(.5f, .5f, 0)) : new Ray(transform.position, transform.forward); } }
        PlayerDeathRespawn death;
        Transform chest;
        Camera viewer;
        Material tracerMaterial;
        GameObject visuals, hud;
        LineRenderer flash;
        Text ammo, status;
        Image reloadFill;
        RectTransform reticle;
        readonly Image[] reticleArms = new Image[4];
        float nextShot, firePoseUntil, flashUntil, hitUntil, nextHud;
        int bulletCursor, impactCursor, shownAmmo = -1, shownReserve = -1;
        bool pendingShot, wasDead, shownReload, shownBlocked;
        bool CanOperate => enabled && loadout && loadout.UsesRifle && loadout.HeldRifle && health && health.currentHealth > 0 && movement && movement.RifleLocomotionAllowed && !(death && death.IsDead);

        void Awake()
        {
            loadout = GetComponent<SahurWeaponLoadout>(); movement = GetComponent<ThirdPersonPlayerController>();
            health = GetComponent<PlayerHealth>(); death = GetComponent<PlayerDeathRespawn>();
        }
        void Update()
        {
            bool dead = !health || health.currentHealth <= 0 || (death && death.IsDead);
            if (dead) wasDead = true;
            else if (wasDead) { wasDead = false; CancelReload(); firePoseUntil = 0; }
            bool paused = Time.timeScale <= 0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput;
            // A zero-delta controller move can temporarily clear grounding during pause.
            // Preserve the reload until locomotion resumes; death and unequipping still cancel it.
            if (!CanOperate && (!paused || dead || !loadout || !loadout.UsesRifle))
            { CancelReload(); firePoseUntil = 0; pendingShot = false; }
            if (!visuals && loadout && loadout.UsesRifle) BuildVisuals();
            RefreshHud();
            // Pausing freezes both the reload and bullets, including the frame a menu closes.
            if (paused) return;
            if (!CanOperate) return;
            if (IsReloading)
            {
                ReloadRemaining = Mathf.Max(0, ReloadRemaining - Time.deltaTime);
                if (ReloadRemaining <= 0)
                {
                    int loaded = Mathf.Min(MagazineCapacity - Rounds, ReserveRounds);
                    Rounds += loaded; ReserveRounds -= loaded; IsReloading = false;
                }
                return;
            }
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && TryReload()) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (Rounds <= 0) { TryReload(); return; }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) TryFire();
        }
        public bool TryReload()
        {
            if (!CanOperate || IsReloading || Rounds >= MagazineCapacity || ReserveRounds <= 0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput || Time.timeScale <= 0) return false;
            ReloadDuration = ReloadSecondsFor(Rounds); ReloadRemaining = ReloadDuration;
            IsReloading = true; firePoseUntil = 0;
            GameplayAudio.Play(GameplayAudio.Cue.Reload, transform.position); return true;
        }
        public int AddReserveAmmo(int amount)
        {
            int added = Mathf.Min(Mathf.Max(0, amount), ReserveCapacity - ReserveRounds);
            ReserveRounds += added; return added;
        }
        public void RestoreAmmo(int magazine, int reserve)
        {
            CancelReload(); pendingShot = false;
            Rounds = Mathf.Clamp(magazine, 0, MagazineCapacity);
            ReserveRounds = Mathf.Clamp(reserve, 0, ReserveCapacity);
        }
        public void CancelReload() { IsReloading = false; ReloadRemaining = 0; }
        public bool TryFire()
        {
            if (!CanOperate || IsReloading || Rounds <= 0 || pendingShot || Time.time < nextShot || Time.timeScale <= 0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput) return false;
            if (!visuals) BuildVisuals();
            Rounds--; ShotsFired++; pendingShot = true;
            GetComponent<PlayerStamina>()?.NotifyCombat();
            GameplayAudio.Play(GameplayAudio.Cue.Gunshot, transform.position);
            nextShot = Time.time + 1f / Mathf.Max(1, roundsPerSecond);
            firePoseUntil = Time.time + .16f;
            return true;
        }
        void LateUpdate()
        {
            if (!visuals || Time.timeScale <= 0 || PauseSettingsMenu.IsOpen || SahurLoadoutUI.BlocksInput) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < bullets.Length; i++) if (bullets[i].active) Step(bullets[i], dt);
            if (pendingShot) { pendingShot = false; Launch(); }
            if (flash) flash.enabled = Time.time < flashUntil && CanOperate;
            for (int i = 0; i < impacts.Length; i++) impacts[i].line.enabled = Time.time < impacts[i].until;
        }
        bool Valid(Collider other, out IDamageable target)
        {
            target = null;
            if (!other || other.transform.IsChildOf(transform) || other.GetComponentInParent<PlayerHealth>()) return false;
            target = other.GetComponentInParent<IDamageable>();
            // Attack range sensors should not become a boss's bullet hitbox.
            if (other.isTrigger && (target == null || (!CombatBodyCollision.IsAnatomy(other) && !other.GetComponent<BossWeakPoint>() && other.GetComponent<IDamageable>() == null))) return false;
            return true;
        }
        bool Trace(Vector3 from, Vector3 direction, float distance, float radius, out Vector3 point, out Collider collider)
        {
            collider = null; point = from + direction * distance;
            float nearest = distance + .001f;
            int count = radius > 0 ? Physics.SphereCastNonAlloc(from, radius, direction, castHits, distance, ~0, QueryTriggerInteraction.Collide)
                : Physics.RaycastNonAlloc(from, direction, castHits, distance, ~0, QueryTriggerInteraction.Collide);
            // Rare dense overlaps must still return the closest wall, even if the reusable buffer fills.
            RaycastHit[] hits = castHits;
            if (count == castHits.Length) { hits = radius > 0 ? Physics.SphereCastAll(from, radius, direction, distance, ~0, QueryTriggerInteraction.Collide) : Physics.RaycastAll(from, direction, distance, ~0, QueryTriggerInteraction.Collide); count = hits.Length; }
            for (int i = 0; i < count; i++) if (hits[i].distance < nearest && Valid(hits[i].collider, out _))
            { nearest = hits[i].distance; collider = hits[i].collider; point = hits[i].point; }
            return collider;
        }
        bool StartingInside(Vector3 origin, out Collider collider)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, .045f, overlaps, ~0, QueryTriggerInteraction.Collide);
            Collider[] candidates = overlaps;
            if (count == overlaps.Length) { candidates = Physics.OverlapSphere(origin, .045f, ~0, QueryTriggerInteraction.Collide); count = candidates.Length; }
            collider = null; float nearest = float.MaxValue;
            for (int i = 0; i < count; i++) if (Valid(candidates[i], out _))
            { float d = (candidates[i].ClosestPoint(origin) - origin).sqrMagnitude; if (d < nearest) { nearest = d; collider = candidates[i]; } }
            return collider;
        }
        public Vector3 GetAimPoint(bool refresh = false)
        {
            if (!refresh && aimFrame == Time.frameCount) return cachedAimPoint;
            aimFrame = Time.frameCount;
            Ray ray = AimRay;
            cachedAimPoint = Trace(ray.origin, ray.direction, range, 0, out var point, out _) ? point : ray.GetPoint(range);
            return cachedAimPoint;
        }
        void Launch()
        {
            if (!loadout || !loadout.HeldRifle) return;
            if (!viewer) viewer = Camera.main;
            var rifle = loadout.HeldRifle;
            Vector3 origin = rifle.TransformPoint(MuzzleLocal);
            // The visible bore is the projectile's direction; the pose solver aims this same line at GetAimPoint().
            Vector3 direction = rifle.TransformDirection(Vector3.left).normalized;
            flashUntil = Time.time + .045f;
            flash.SetPosition(0, origin + direction * .7f); flash.SetPosition(1, origin - viewerRight() * .18f);
            flash.SetPosition(2, origin + viewerRight() * .18f); flash.SetPosition(3, origin + direction * .7f);
            flash.SetPosition(4, origin - Vector3.up * .16f); flash.SetPosition(5, origin + Vector3.up * .16f);
            // The barrel cannot protrude through a nearby wall and spawn bullets behind it.
            if (!chest && movement.CharacterAnimator) chest = movement.CharacterAnimator.GetBoneTransform(HumanBodyBones.Chest);
            Vector3 basePoint = chest ? chest.position : transform.position;
            Vector3 barrel = origin - basePoint;
            if (StartingInside(origin, out var overlap)) { ImpactAt(overlap, origin); return; }
            if (barrel.sqrMagnitude > .001f && Trace(basePoint, barrel.normalized, barrel.magnitude, .045f, out var blocked, out var wall)) { ImpactAt(wall, blocked); return; }
            var bullet = bullets[bulletCursor++ % bullets.Length];
            bullet.position = origin; bullet.direction = direction; bullet.traveled = 0; bullet.active = true;
            bullet.line.enabled = true; bullet.line.SetPosition(0, origin); bullet.line.SetPosition(1, origin + direction * .25f);
        }
        Vector3 viewerRight() => viewer ? viewer.transform.right : transform.right;
        void Step(Bullet bullet, float dt)
        {
            float distance = Mathf.Min(bulletSpeed * dt, range - bullet.traveled);
            if (Trace(bullet.position, bullet.direction, distance, .045f, out var point, out var collider))
            { ImpactAt(collider, point, bullet.traveled + Vector3.Distance(bullet.position, point)); bullet.active = false; bullet.line.enabled = false; return; }
            bullet.position += bullet.direction * distance; bullet.traveled += distance;
            bullet.active = bullet.traveled < range; bullet.line.enabled = bullet.active;
            bullet.line.SetPosition(0, bullet.position - bullet.direction * Mathf.Min(1.8f, bullet.traveled)); bullet.line.SetPosition(1, bullet.position);
        }
        public static float DamageAtDistance(float distance, bool aircraft, bool counter, bool engine)
        {
            float falloff = aircraft ? Mathf.Lerp(1f, .7f, Mathf.InverseLerp(100f, 180f, distance)) :
                Mathf.Lerp(1f, .4f, Mathf.InverseLerp(20f, 100f, distance));
            return BulletDamage * falloff * (aircraft && counter ? 1.25f : 1f) * (aircraft && engine ? BossWeakPoint.DamageMultiplier : 1f);
        }
        void ImpactAt(Collider collider, Vector3 point, float distance = 0f)
        {
            bool damaged = Valid(collider, out var target) && target != null;
            if (damaged)
            {
                var aircraft = collider.GetComponentInParent<BombardinoBoss>();
                bool engine = aircraft && (collider.GetComponent<BossWeakPoint>() || BossWeakPoint.Contains(target as Component, point));
                damaged=CombatHitFeedback.Apply(gameObject,target,DamageAtDistance(distance, aircraft, aircraft && aircraft.IsExposed, engine),point,CombatHitKind.Rifle,bulletDirection(point));
                if(damaged){Hits++; hitUntil = Time.time + .16f;}
            }
            int index = impactCursor++ % impacts.Length;
            var impact = impacts[index]; impact.until = Time.time + .11f; var line = impact.line;
            float size = damaged ? .3f : .18f;
            line.SetPosition(0, point - viewerRight() * size); line.SetPosition(1, point + viewerRight() * size);
            line.SetPosition(2, point); line.SetPosition(3, point - Vector3.up * size); line.SetPosition(4, point + Vector3.up * size);
            line.startColor = line.endColor = damaged ? new Color(1, .55f, .2f) : new Color(1, .9f, .6f);
            impacts[index] = impact;
        }
        Vector3 bulletDirection(Vector3 point)=>(point-transform.position).normalized;
        LineRenderer Line(string name, int points, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(visuals.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = tracerMaterial; line.positionCount = points;
            line.useWorldSpace = true; line.startWidth = width; line.endWidth = width * .4f;
            line.startColor = new Color(1, .7f, .25f); line.endColor = new Color(1, .95f, .75f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            line.enabled = false; return line;
        }
        void BuildVisuals()
        {
            visuals = new GameObject("M4A1 projectile pool");
            tracerMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            for (int i = 0; i < bullets.Length; i++) bullets[i] = new Bullet { line = Line("Bullet " + i, 2, .065f) };
            for (int i = 0; i < impacts.Length; i++) impacts[i].line = Line("Impact " + i, 5, .045f);
            flash = Line("Muzzle flash", 6, .12f);
            hud = new GameObject("M4A1 Ammo HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; hud.GetComponent<Canvas>().sortingOrder = 25; GameUITheme.Scale(hud.GetComponent<Canvas>());
            var card = GameUITheme.Rect(hud.transform, "Ammo card", new Vector2(1, 0), new Vector2(260, 110));
            card.pivot = new Vector2(1, 0); card.anchoredPosition = new Vector2(-38, 38); card.localRotation = Quaternion.Euler(0, 0, -.7f);
            GameUITheme.Image(card, new Color(.88f, .83f, .70f, .94f));
            ammo = GameUITheme.Label(card, "Ammunition", 27, new Vector2(-108, -29), new Vector2(230, 42), TextAnchor.MiddleLeft); ammo.color = new Color(.22f, .17f, .12f);
            status = GameUITheme.Label(card, "Reload status", 17, new Vector2(-108, -65), new Vector2(235, 32), TextAnchor.MiddleLeft); status.color = ammo.color;
            var bar = GameUITheme.Rect(card, "Reload track", new Vector2(.5f, 0), new Vector2(220, 3)); bar.anchoredPosition = new Vector2(0, 16); GameUITheme.Image(bar, new Color(.4f, .32f, .22f, .3f));
            reloadFill = GameUITheme.Fill(bar, "Reload progress", new Color(.5f, .24f, .12f));
            reticle = GameUITheme.Rect(hud.transform, "Rifle crosshair", new Vector2(.5f, .5f), new Vector2(32, 32));
            for (int i = 0; i < reticleArms.Length; i++)
            {
                Vector2 direction = i == 0 ? Vector2.left : i == 1 ? Vector2.right : i == 2 ? Vector2.up : Vector2.down;
                Vector2 size = i < 2 ? new Vector2(10, 2) : new Vector2(2, 10);
                var outline = GameUITheme.Rect(reticle, "Crosshair outline " + i, new Vector2(.5f, .5f), size + Vector2.one * 2);
                outline.anchoredPosition = direction * 10;
                GameUITheme.Image(outline, new Color(0, 0, 0, .8f));
                var arm = GameUITheme.Rect(outline, "Crosshair arm", new Vector2(.5f, .5f), size);
                reticleArms[i] = GameUITheme.Image(arm, Color.white);
            }
        }
        void RefreshHud()
        {
            if (!hud) return;
            bool visible = loadout && loadout.UsesRifle && health && health.currentHealth > 0 && movement && movement.enabled && !movement.Swimming && !PauseSettingsMenu.IsOpen && !SahurLoadoutUI.BlocksInput;
            if (hud.activeSelf != visible) hud.SetActive(visible);
            if (!visible) return;
            bool hit = Time.time < hitUntil;
            Color crosshairColor = hit ? new Color(1, .55f, .2f) : Color.white;
            for (int i = 0; i < reticleArms.Length; i++) reticleArms[i].color = crosshairColor;
            reticle.localScale = Vector3.one * (hit ? 1.15f : IsAiming ? .8f : movement && movement.IsSprinting ? 1.35f : 1);
            reloadFill.fillAmount = ReloadProgress;
            bool blocked = !CanOperate;
            if (ReserveRounds == shownReserve && Rounds == shownAmmo && IsReloading == shownReload && blocked == shownBlocked && Time.unscaledTime < nextHud) return;
            shownReserve = ReserveRounds; shownAmmo = Rounds; shownReload = IsReloading; shownBlocked = blocked; nextHud = Time.unscaledTime + .1f;
            ammo.text = "M4A1    " + Rounds.ToString("00") + " / 30";
            status.text = IsReloading ? GameLocalization.Text("Reloading ") + ReloadRemaining.ToString("0.0") + "s" : ReserveRounds > 0 ? GameLocalization.Format("rifle.reserve", ReserveRounds) : GameLocalization.Text("rifle.emptyReserve");
        }
        void OnDisable()
        {
            CancelReload(); pendingShot = false; firePoseUntil = 0;
            if (hud) hud.SetActive(false);
            if (visuals) { foreach (var bullet in bullets) { bullet.active = false; bullet.line.enabled = false; } foreach (var impact in impacts) impact.line.enabled = false; flash.enabled = false; }
        }
        void OnDestroy() { if (hud) Destroy(hud); if (visuals) Destroy(visuals); if (tracerMaterial) Destroy(tracerMaterial); }
    }
}
