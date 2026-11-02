namespace Mavis
{
    /// <summary>An owned item whose running cooldown can be shortened by a speed scroll.</summary>
    public interface ICooldownTarget
    {
        string CooldownDisplayName { get; }
        bool CooldownTargetOwned { get; }
        float CooldownRemaining { get; }
        bool CanReduceCooldown { get; }
        bool ReduceCooldown(float seconds);
    }
}
