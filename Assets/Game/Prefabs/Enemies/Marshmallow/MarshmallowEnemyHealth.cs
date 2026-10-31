using Mavis;
using UnityEngine;

// The common combat pipeline resolves hits on the Health component itself.
public sealed class MarshmallowEnemyHealth : Health, ICombatHitResolver
{
    [Header("Health bar")]
    [Min(1f)] public float healthBarVisibleDistance = 35f;
    [Min(0f)] public float healthBarHeight = 2.25f;

    void Start() => MarshmallowHealthBars.Register(this);
    void OnDestroy() => MarshmallowHealthBars.Unregister(this);

    public CombatHitResult ResolveCombatHit(GameObject attacker, float damage, Vector3 point, CombatHitKind kind)
    {
        var brain = GetComponent<ChickenBroAI>();
        if (brain) return brain.ResolveCombatHit(attacker, damage, point, kind);
        ApplyDamage(damage, point);
        return CombatHitResult.Hit;
    }
}
