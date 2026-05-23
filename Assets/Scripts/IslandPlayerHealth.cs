using UnityEngine;

public sealed class IslandPlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float damageCooldown = 0.75f;

    private int currentHealth;
    private float nextDamageTime;
    private Renderer[] renderers;
    private Color[] originalColors;
    private float flashUntil;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        currentHealth = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    private void Update()
    {
        if (renderers == null)
        {
            return;
        }

        bool flashing = Time.time < flashUntil;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            renderers[i].material.color = flashing ? Color.red : originalColors[i];
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || Time.time < nextDamageTime)
        {
            return;
        }

        nextDamageTime = Time.time + damageCooldown;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        flashUntil = Time.time + 0.18f;
        IslandGameManager.Instance?.PlayHitSound();

        if (currentHealth == 0)
        {
            IslandGameManager.Instance?.Lose();
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0 || currentHealth >= maxHealth)
            return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        flashUntil = Time.time + 0.18f;
    }
}
