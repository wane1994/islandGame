using UnityEngine;

public sealed class IslandBeacon : MonoBehaviour
{
    [SerializeField] private Light beaconLight;
    [SerializeField] private float pulseSpeed = 3f;

    private Vector3 startScale;

    public void Configure(Light lightSource)
    {
        beaconLight = lightSource;
    }

    private void Start()
    {
        startScale = transform.localScale;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.08f;
        transform.localScale = startScale * pulse;

        if (beaconLight != null)
        {
            beaconLight.intensity = IslandGameManager.Instance != null && IslandGameManager.Instance.HasCollectedAllCrystals
                ? 3.2f + Mathf.Sin(Time.time * pulseSpeed) * 0.7f
                : 1.1f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<IslandPlayerController>() == null || IslandGameManager.Instance == null)
        {
            return;
        }

        if (IslandGameManager.Instance.HasCollectedAllCrystals)
        {
            IslandGameManager.Instance.Win();
        }
    }
}
