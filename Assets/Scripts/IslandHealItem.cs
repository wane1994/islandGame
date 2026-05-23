using UnityEngine;

public sealed class IslandHealItem : MonoBehaviour
{
    [SerializeField] private int healAmount = 40;
    [SerializeField] private AudioClip healClip;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var health = other.GetComponent<IslandPlayerHealth>();
            if (health != null && health.CurrentHealth < health.MaxHealth)
            {
                health.Heal(healAmount);
                if (healClip != null)
                {
                    AudioSource.PlayClipAtPoint(healClip, transform.position);
                }
                Destroy(gameObject);
            }
        }
    }
}
