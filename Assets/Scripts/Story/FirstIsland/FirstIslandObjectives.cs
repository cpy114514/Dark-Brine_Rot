using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Shore tutorial, then Cappuccino, Nailong and Bombardino in order.</summary>
    [DisallowMultipleComponent]
    public sealed class FirstIslandObjectives : MonoBehaviour
    {
        public enum BossKind { Capri, Nailong, Bombardino }
        [Serializable] public sealed class BossProgress
        {
            public string bossId;
            public BossKind kind;
            public bool defeated;
        }
        [Serializable] public sealed class Progress
        {
            // Retain the original fields so existing saves continue to load.
            public int stage, rewardBaseline;
            public int combatLesson;
            public string bossId;
            public List<BossProgress> bosses = new List<BossProgress>();
        }
        sealed class BossFlow
        {
            public WorldSaveIdentity identity;
            public BossProgress progress;
            public Health health;
            public NailongHealth nailongHealth;
            public CappuccinoAI capri;
            public NailongAI nailong;
            public BombardinoBoss aircraft;
            public bool Fighting => (capri && capri.FightActive) || (nailong && nailong.FightActive) || (aircraft && aircraft.FightActive);
            public bool Dead => (health && health.IsDead) || (nailongHealth && nailongHealth.IsDead);
        }
        Progress progress = new Progress();
        readonly List<BossFlow> flows = new List<BossFlow>(3);
        readonly StringBuilder text = new StringBuilder(256);
        static readonly string[] EnglishBearings = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        ThirdPersonPlayerController player;
        FirstIslandArrival arrival;
        EquipmentInventory inventory;
        SahurAttack attack;
        SahurRifleCombat rifle;
        EnemyLockOn lockOn;
        SahurHealingPacks healing;
        BossFlow nearest;
        Text label;
        GameObject panel;
        RectTransform card;
        Vector3 moveStart;
        float nextRefresh, nextDiscovery, nextSave, doneUntil;
        bool pendingSave;
        public int Stage => progress.stage;
        public string RecommendedBossId => progress.bossId;
        public int DefeatedBossCount
        {
            get
            {
                int count = 0;
                for (int kind = 0; kind < 3; kind++)
                    foreach (var entry in progress.bosses)
                        if ((int)entry.kind == kind && entry.defeated) { count++; break; }
                return count;
            }
        }
        static Progress Copy(Progress source)
        {
            var copy = new Progress { stage = source.stage, rewardBaseline = source.rewardBaseline, bossId = source.bossId,
                combatLesson = Mathf.Clamp(source.combatLesson, 0, 4) };
            if (source.bosses != null)
                foreach (var entry in source.bosses)
                    if (entry != null && !string.IsNullOrEmpty(entry.bossId) && (int)entry.kind >= 0 && (int)entry.kind < 3 &&
                        !copy.bosses.Exists(b => b.bossId == entry.bossId))
                        copy.bosses.Add(new BossProgress { bossId = entry.bossId, kind = entry.kind, defeated = entry.defeated });
            return copy;
        }
        public Progress Capture() => Copy(progress);
        public void Restore(Progress saved)
        {
            progress = saved == null ? new Progress() : Copy(saved);
            progress.stage = Mathf.Clamp(progress.stage, 0, 7);
            // A completed single-boss tutorial must not finish the other two encounters.
            if (progress.stage >= 4) progress.stage = DefeatedBossCount == 3 ? 7 : 4;
            flows.Clear(); nearest = null; nextDiscovery = nextRefresh = 0;
            pendingSave = false; doneUntil = 0; moveStart = transform.position;
        }
        void Start()
        {
            player = GetComponent<ThirdPersonPlayerController>(); arrival = GetComponent<FirstIslandArrival>();
            inventory = GetComponent<EquipmentInventory>(); attack = GetComponent<SahurAttack>();
            rifle = GetComponent<SahurRifleCombat>();
            lockOn = GetComponent<EnemyLockOn>(); healing = GetComponent<SahurHealingPacks>();
            if (inventory) inventory.Collected += OnCollected;
            moveStart = transform.position;
            var canvas = new GameObject("Island objective", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(transform, false); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 80; GameUITheme.Scale(canvas.GetComponent<Canvas>());
            panel = canvas;
            card = GameUITheme.Rect(canvas.transform, "Objective card", new Vector2(0, 1), new Vector2(480, 104));
            card.pivot = new Vector2(0, 1); card.anchoredPosition = new Vector2(28, -132);
            GameUITheme.Image(card, new Color(.035f, .045f, .045f, .82f));
            label = GameUITheme.Label(card, "Current objective", 20, new Vector2(20, -49), new Vector2(440, 92), TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, 1);
            label.rectTransform.pivot = new Vector2(0, .5f);
            DiscoverBosses();
        }
        void DiscoverBosses()
        {
            nextDiscovery = Time.unscaledTime + 2f;
            foreach (var identity in FindObjectsByType<WorldSaveIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (string.IsNullOrEmpty(identity.id) || !identity.gameObject.scene.path.StartsWith("Assets/Scenes/First Island/", StringComparison.Ordinal) ||
                    flows.Exists(f => f.progress.bossId == identity.id)) continue;
                var capri = identity.GetComponent<CappuccinoAI>();
                var nailong = identity.GetComponent<NailongAI>();
                var aircraft = identity.GetComponent<BombardinoBoss>();
                if (!capri && !nailong && !aircraft) continue;
                BossKind kind = capri ? BossKind.Capri : nailong ? BossKind.Nailong : BossKind.Bombardino;
                var saved = progress.bosses.Find(b => b.bossId == identity.id);
                if (saved == null) { saved = new BossProgress { bossId = identity.id, kind = kind }; progress.bosses.Add(saved); }
                saved.kind = kind;
                FirstIslandBossProgression.Register(identity);
                flows.Add(new BossFlow { identity = identity, progress = saved, health = identity.GetComponent<Health>(),
                    nailongHealth = identity.GetComponent<NailongHealth>(), capri = capri, nailong = nailong, aircraft = aircraft });
            }
        }
        static float FlatDistanceSquared(Vector3 a, Vector3 b) { a -= b; return a.x * a.x + a.z * a.z; }
        void RefreshBosses()
        {
            if (Time.unscaledTime >= nextDiscovery) DiscoverBosses();
            nearest = null; float distance = float.PositiveInfinity;
            foreach (var flow in flows)
            {
                if (!flow.progress.defeated && (GameWorldProgress.IsDefeated(flow.progress.bossId) || flow.Dead))
                {
                    flow.progress.defeated = true;
                    QueueSave();
                }
                if (flow.progress.defeated || !flow.identity || !flow.identity.gameObject.activeInHierarchy) continue;
                if (!FirstIslandBossProgression.IsUnlocked(flow.progress.kind)) continue;
                float candidate = FlatDistanceSquared(transform.position, flow.identity.transform.position);
                if (candidate < distance) { distance = candidate; nearest = flow; }
            }
            // Recommend only the next unlocked, living encounter.
            progress.bossId = nearest == null ? null : nearest.progress.bossId;
        }
        void QueueSave()
        {
            if (!pendingSave) nextSave = Time.unscaledTime + 1f;
            pendingSave = true;
        }
        void OnCollected(EquipmentItem item)
        {
            if (progress.stage >= 4 && !GameSaveManager.IsContinuePending && !AdditiveSceneBootstrap.IsLoading) QueueSave();
        }
        void Update()
        {
            if (!player || !label || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .15f;
            bool modal = PauseSettingsMenu.IsOpen || SahurLoadoutUI.IsOpen || IslandMapUI.IsOpen || PlayerDeathRespawn.IsOpen;
            bool lessonVisible = progress.stage >= 4 && progress.stage < 7 && progress.combatLesson < 4;
            panel.SetActive(!modal && (!BossHudPanel.HasActiveEncounter || lessonVisible) && (!arrival || arrival.IsAwake) && (progress.stage < 7 || Time.unscaledTime < doneUntil));
            if (modal || GameSaveManager.IsContinuePending || AdditiveSceneBootstrap.IsLoading || player.ExternalControlLock) return;
            RefreshBosses();
            if (progress.stage == 0 && (!arrival || arrival.IsAwake)) progress.stage = 1;
            if (progress.stage == 1 && (!arrival || arrival.HasStick)) { progress.stage = 2; moveStart = transform.position; }
            if (progress.stage == 2 && FlatDistanceSquared(transform.position, moveStart) >= 36) progress.stage = 3;
            if (progress.stage == 3 && !rifle) rifle = GetComponent<SahurRifleCombat>();
            if (progress.stage == 3 && ((attack && attack.IsCombatMotionActive) || (rifle && rifle.ShotsFired > 0))) progress.stage = 4;
            // Going straight to any boss also ends the practice prompts; no tutorial step locks combat.
            if (progress.stage < 4)
                foreach (var flow in flows)
                    if (flow.Fighting || flow.progress.defeated) { progress.stage = 4; break; }
            if (progress.stage >= 4 && progress.stage < 7 && DefeatedBossCount == 3)
            { progress.stage = 6; if (!pendingSave) QueueSave(); }
            if (progress.stage >= 4 && progress.stage < 7 && DefeatedBossCount < 3)
            {
                if (!healing) healing = GetComponent<SahurHealingPacks>();
                bool practiced = progress.combatLesson == 0 ? lockOn && lockOn.LocksAcquired > 0 :
                    progress.combatLesson == 1 ? player.DodgesPerformed > 0 :
                    progress.combatLesson == 2 ? attack && attack.ChargedAttacksPerformed > 0 :
                    progress.combatLesson == 3 && healing && healing.UseAttempts > 0;
                if (practiced) { progress.combatLesson++; QueueSave(); }
            }
            if (pendingSave && Time.unscaledTime >= nextSave)
            {
                nextSave = Time.unscaledTime + 5f;
                int previousStage = progress.stage;
                if (DefeatedBossCount == 3) progress.stage = 7;
                if (GameSaveManager.SaveCurrentGame()) { pendingSave = false; if (progress.stage == 7) doneUntil = Time.unscaledTime + 6f; }
                else progress.stage = previousStage;
            }
            string value;
            switch (progress.stage)
            {
                case 0: value = "Reach the shore"; break;
                case 1: value = "Recover your stick\nFollow the beach; press F near the stick."; break;
                case 2: value = "Explore the shore\nMove with your movement keys; look with the mouse."; break;
                case 3: value = "Practice an attack\nLeft mouse: attack. Try one swing on the shore."; break;
                default: value = progress.stage < 7 && progress.combatLesson < 4 && DefeatedBossCount < 3 && !NearLockedBoss()
                    ? CombatLessonText() + "\nNext boss: " + NextBossName() : BossText(true); break;
            }
            bool bossesShown = progress.stage >= 4 && (progress.combatLesson >= 4 || DefeatedBossCount == 3 || NearLockedBoss());
            Vector2 size = new Vector2(480, bossesShown ? 240 : progress.stage >= 4 ? 196 : 104);
            if (card.sizeDelta != size)
            {
                card.sizeDelta = size;
                label.rectTransform.sizeDelta = new Vector2(440, size.y - 16);
                label.rectTransform.anchoredPosition = new Vector2(20, -size.y * .5f);
            }
            if (label.text != value) label.text = value;
        }
        string CombatLessonText()
        {
            switch (progress.combatLesson)
            {
                case 0: return "1 / 4 · Lock on\nFace a nearby enemy; middle-click to lock on.\nMiddle-click again to release. Approach when ready.";
                case 1: return "2 / 4 · Dodge\nPress " + GameInputSettings.Get(GameInputSettings.Action.Dodge) + " with a direction to dodge.\nDodging costs stamina; let the SP bar recover.";
                case 2: return "3 / 4 · Charged strike\nHold right mouse for 0.5s, then release.\nUse a melee weapon; save SP for dodging.";
                default: return "4 / 4 · Healing\nPress " + GameInputSettings.Get(GameInputSettings.Action.Heal) + " to try a healing pack (3 per life; refilled on respawn).\nFull health uses none. Shore camp also restores HP/SP.";
            }
        }
        bool NearLockedBoss()
        {
            foreach (var flow in flows)
                if (!flow.progress.defeated && flow.identity && flow.identity.gameObject.activeInHierarchy &&
                    !FirstIslandBossProgression.IsUnlocked(flow.progress.kind) &&
                    FlatDistanceSquared(transform.position, flow.identity.transform.position) < 70f * 70f) return true;
            return false;
        }
        string NextBossName()
        {
            for (int kind = 0; kind < 3; kind++)
                if (!progress.bosses.Exists(b => (int)b.kind == kind && b.defeated)) return BossName((BossKind)kind, true);
            return "All three defeated";
        }
        static string BossName(BossKind kind, bool en) => kind == BossKind.Capri ? "Cappuccino" :
            kind == BossKind.Nailong ? GameLocalization.Text("enemy.name") : ("Bombardino");
        string BossText(bool en)
        {
            text.Clear();
            text.Append("Island bosses  ").Append(DefeatedBossCount).Append(" / 3\n");
            if (nearest != null)
            {
                text.Append("Next: ").Append(BossName(nearest.progress.kind, en));
                if (nearest.identity) text.Append(" · ").Append(Bearing(nearest.identity.transform.position - transform.position, en));
                text.Append('\n');
            }
            else text.Append(en ? (DefeatedBossCount == 3 ? "All three defeated\n" : "Finding nearby bosses...\n") :
                (DefeatedBossCount == 3 ? "All three defeated\n" : "Finding nearby bosses...\n"));
            for (int kind = 0; kind < 3; kind++)
            {
                var flow = flows.Find(f => (int)f.progress.kind == kind);
                var saved = flow == null ? progress.bosses.Find(b => (int)b.kind == kind) : flow.progress;
                text.Append(BossName((BossKind)kind, en)).Append(" · ");
                if (saved != null && saved.defeated) text.Append("Defeated");
                else if (!FirstIslandBossProgression.IsUnlocked((BossKind)kind))
                    text.Append("Locked — defeat ").Append(FirstIslandBossProgression.RequiredBossName((BossKind)kind));
                else if (flow != null && flow.identity && flow.identity.gameObject.activeInHierarchy)
                {
                    text.Append(Mathf.RoundToInt(Mathf.Sqrt(FlatDistanceSquared(transform.position, flow.identity.transform.position)))).Append(" m · ");
                    text.Append(flow.Fighting ? ("In combat") : ("Available"));
                }
                else text.Append("Not available");
                text.Append('\n');
            }
            string guidance = "M: map · Follow signs to ammo supply points";
            var weapons = GetComponent<SahurWeaponLoadout>();
            if (!FirstIslandBossProgression.CanObtainRifle(this)) guidance = FirstIslandBossProgression.RifleUnlockHint;
            else if (weapons && !weapons.HasM4A1) guidance = "M4A1 unlocked — collect the rifle on the island";
            text.Append(DefeatedBossCount == 3 ? (en ? (progress.stage == 7 ? "Progress saved" : "Saving progress...") :
                (progress.stage == 7 ? "Progress saved" : "Saving progress...")) : guidance);
            return text.ToString();
        }
        public static string Bearing(Vector3 direction, bool english)
        {
            int index = Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 360) / 45) % 8;
            return EnglishBearings[index];
        }
        void OnDisable() { if (panel) panel.SetActive(false); }
        void OnDestroy() { if (inventory) inventory.Collected -= OnCollected; }
    }
}
