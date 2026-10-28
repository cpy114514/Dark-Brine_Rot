using System.Collections.Generic;
using UnityEngine;

namespace Mavis
{
    /// <summary>Shared slots for one Chicken Bro group. At most a few may swing at once.</summary>
    public sealed class ChickenBroFlock
    {
        public int maxAttackers = 2;
        public float attackResumeAt;
        readonly List<ChickenBroAI> members = new List<ChickenBroAI>(16);
        readonly List<ChickenBroAI> attackers = new List<ChickenBroAI>(4);
        readonly List<ChickenBroAI> expired = new List<ChickenBroAI>(4);
        public static ChickenBroFlock Active { get; private set; }
        public IReadOnlyList<ChickenBroAI> Members => members;
        public bool AttacksPaused => Time.time < attackResumeAt;

        public void Open() => Active = this;
        public void Close()
        {
            if (Active == this) Active = null;
            members.Clear();
            attackers.Clear();
        }

        public void Add(ChickenBroAI bro)
        {
            if (bro && !members.Contains(bro)) members.Add(bro);
        }

        public void Remove(ChickenBroAI bro)
        {
            members.Remove(bro);
            attackers.Remove(bro);
        }

        public void Courtesy(float seconds) => attackResumeAt = Mathf.Max(attackResumeAt, Time.time + seconds);

        public bool TryAttack(ChickenBroAI bro)
        {
            expired.Clear();
            for (int i = 0; i < attackers.Count; i++)
                if (!attackers[i] || !attackers[i].HoldsAttack) expired.Add(attackers[i]);
            for (int i = 0; i < expired.Count; i++) attackers.Remove(expired[i]);
            if (!bro) return false;
            if (attackers.Contains(bro)) return true;
            if (attackers.Count >= maxAttackers) return false;
            attackers.Add(bro);
            return true;
        }

        public void ReleaseAttack(ChickenBroAI bro) => attackers.Remove(bro);

        public Vector3 SurroundPoint(ChickenBroAI bro, Vector3 player, float radius)
        {
            int index = Mathf.Max(0, members.IndexOf(bro));
            int count = Mathf.Max(1, members.Count);
            float angle = index / (float)count * Mathf.PI * 2f + Time.time * .18f;
            return player + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }

        public Vector3 Separation(ChickenBroAI bro, float radius)
        {
            Vector3 push = Vector3.zero;
            Vector3 at = bro.transform.position;
            for (int i = 0; i < members.Count; i++)
            {
                var other = members[i];
                if (!other || other == bro) continue;
                Vector3 delta = at - other.transform.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance < .05f) delta = ((i & 1) == 0 ? Vector3.right : Vector3.forward) * radius;
                else if (distance < radius) delta *= (radius - distance) / distance;
                else continue;
                push += delta;
            }
            return push;
        }
    }
}
