using UnityEngine;

public sealed class IslandPlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float damageCooldown = 0.75f;

    private int currentHealth;
    private float nextDamageTime;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || Time.time < nextDamageTime)
        {
            return;
        }

        nextDamageTime = Time.time + damageCooldown;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        IslandGameManager.Instance?.PlayHitSound();

        if (currentHealth == 0)
        {
            IslandGameManager.Instance?.Lose();
        }
    }
}
