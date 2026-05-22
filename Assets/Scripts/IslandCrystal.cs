using UnityEngine;

public sealed class IslandCrystal : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 95f;
    [SerializeField] private float bobSpeed = 2.3f;
    [SerializeField] private float bobHeight = 0.22f;

    private Vector3 startPosition;
    private bool collected;

    private void Start()
    {
        startPosition = transform.position;
        IslandGameManager.Instance?.RegisterCrystal();
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || other.GetComponentInParent<IslandPlayerController>() == null)
        {
            return;
        }

        collected = true;
        IslandGameManager.Instance?.CollectCrystal();
        Destroy(gameObject);
    }
}
