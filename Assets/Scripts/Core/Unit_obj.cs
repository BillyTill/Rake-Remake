using UnityEngine;

/// <summary>
/// Base class for all damageable units in the game.
/// Both the Rake (Enemy_AI) and Animals (Animal_AI) inherit from this.
/// </summary>
public class Unit_obj : MonoBehaviour
{
    /// <summary>
    /// Called when the unit takes damage.
    /// </summary>
    /// <param name="amount">Damage amount</param>
    /// <param name="damageType">Type of damage (e.g., "SniperShot", "TrapDamage", "RakeKick", "RakeUppercut")</param>
    public virtual void TakeDamage(int amount, string damageType)
    {
        // Base implementation does nothing - derived classes must override
    }
}