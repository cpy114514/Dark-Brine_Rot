using Mavis;
using UnityEngine;

/// <summary>The story shark takes normal weapon hits, but cannot end this encounter in victory.</summary>
public sealed class Story1SharkHealth : Health
{
    public int ReceivedHits { get; private set; }
    public Vector3 LockPoint
    {
        get
        {
            var hitbox=GetComponent<BoxCollider>();
            return hitbox!=null ? hitbox.bounds.center+Vector3.up*(hitbox.bounds.extents.y*.95f) : transform.position;
        }
    }
    public override void ApplyDamage(float amount, Vector3 hitPoint)
    {
        if (amount <= 0 || IsDead) return;
        ReceivedHits++;
        float damage = Mathf.Min(amount, Mathf.Max(0, currentHealth - 1));
        if (damage > 0) base.ApplyDamage(damage, hitPoint);
        else OnDamaged?.Invoke(amount);
    }
}
