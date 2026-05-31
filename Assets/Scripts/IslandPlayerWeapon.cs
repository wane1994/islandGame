using System.Collections;
using UnityEngine;

public sealed class IslandPlayerWeapon : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float range = 58f;
    [SerializeField] private float fireInterval = 0.28f;
    [SerializeField] private float bulletSpeed = 52f;
    [SerializeField] private float hitRadius = 0.85f;
    [SerializeField] private Vector3 weaponOffset = new Vector3(0.42f, 1.35f, 0.52f);

    private Camera playerCamera;
    private Transform muzzle;
    private Material weaponMaterial;
    private Material muzzleMaterial;
    private Material tracerMaterial;
    private Material bulletMaterial;
    private float nextFireTime;

    public float FireCooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time);

    private void Awake()
    {
        CreateWeaponVisual();
    }

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (IslandGameManager.Instance == null || !IslandGameManager.Instance.IsRunning)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked || !Input.GetMouseButton(0) || Time.time < nextFireTime)
        {
            return;
        }

        Fire();
    }

    private void Fire()
    {
        nextFireTime = Time.time + fireInterval;
        IslandGameManager.Instance?.PlayShootSound();

        Ray ray = playerCamera != null
            ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : new Ray(transform.position + Vector3.up * 1.4f, transform.forward);

        Vector3 aimDirection = ray.direction;
        aimDirection.y = 0f;
        if (aimDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
        }

        Vector3 endPoint = ray.origin + ray.direction * range;
        RaycastHit[] hits = Physics.SphereCastAll(ray, hitRadius, range, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        RaycastHit? firstWorldHit = null;
        IslandEnemy hitEnemy = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            IslandEnemy enemy = hit.collider.GetComponentInParent<IslandEnemy>();
            if (enemy != null)
            {
                hitEnemy = enemy;
                endPoint = enemy.transform.position + Vector3.up * 1.1f;
                break;
            }

            firstWorldHit ??= hit;
        }

        if (hitEnemy != null)
        {
            hitEnemy.TakeDamage(damage);
        }
        else if (firstWorldHit.HasValue)
        {
            endPoint = firstWorldHit.Value.point;
        }

        StartCoroutine(FlyBullet(endPoint));
    }

    private void CreateWeaponVisual()
    {
        weaponMaterial = CreateMaterial("Hero Weapon Material", new Color(0.08f, 0.09f, 0.10f));
        muzzleMaterial = CreateMaterial("Hero Muzzle Material", new Color(1f, 0.74f, 0.18f));
        tracerMaterial = CreateMaterial("Hero Shot Tracer Material", new Color(1f, 0.82f, 0.25f, 0.9f), true);
        bulletMaterial = CreateMaterial("Hero Bullet Material", new Color(1f, 0.92f, 0.18f));

        var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.name = "Weapon Grip";
        grip.transform.SetParent(transform, false);
        grip.transform.localPosition = weaponOffset + new Vector3(0f, -0.18f, -0.05f);
        grip.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        grip.transform.localScale = new Vector3(0.16f, 0.36f, 0.16f);
        grip.GetComponent<Renderer>().sharedMaterial = weaponMaterial;
        RemoveCollider(grip);

        var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Weapon Barrel";
        barrel.transform.SetParent(transform, false);
        barrel.transform.localPosition = weaponOffset;
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.transform.localScale = new Vector3(0.08f, 0.42f, 0.08f);
        barrel.GetComponent<Renderer>().sharedMaterial = weaponMaterial;
        RemoveCollider(barrel);

        var muzzleObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        muzzleObject.name = "Weapon Muzzle";
        muzzleObject.transform.SetParent(transform, false);
        muzzleObject.transform.localPosition = weaponOffset + Vector3.forward * 0.44f;
        muzzleObject.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        muzzleObject.GetComponent<Renderer>().sharedMaterial = muzzleMaterial;
        RemoveCollider(muzzleObject);
        muzzle = muzzleObject.transform;
    }

    private IEnumerator FlyBullet(Vector3 endPoint)
    {
        Vector3 startPoint = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.35f + transform.forward * 0.75f;
        float distance = Vector3.Distance(startPoint, endPoint);
        float duration = Mathf.Clamp(distance / bulletSpeed, 0.12f, 0.55f);
        float elapsed = 0f;

        var bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Visible Bullet";
        bullet.transform.position = startPoint;
        bullet.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        bullet.GetComponent<Renderer>().sharedMaterial = bulletMaterial;
        RemoveCollider(bullet);

        var light = bullet.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.76f, 0.18f);
        light.range = 3.5f;
        light.intensity = 1.2f;

        var tracer = new GameObject("Shot Tracer");
        var line = tracer.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.11f;
        line.endWidth = 0.03f;
        line.material = tracerMaterial;
        line.useWorldSpace = true;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            Vector3 bulletPosition = Vector3.Lerp(startPoint, endPoint, progress);
            bullet.transform.position = bulletPosition;
            line.SetPosition(0, Vector3.Lerp(startPoint, bulletPosition, 0.45f));
            line.SetPosition(1, bulletPosition);
            yield return null;
        }

        CreateImpactFlash(endPoint);

        if (bullet != null)
        {
            Destroy(bullet);
        }

        if (tracer != null)
        {
            Destroy(tracer);
        }
    }

    private void CreateImpactFlash(Vector3 position)
    {
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "Bullet Impact";
        flash.transform.position = position;
        flash.transform.localScale = new Vector3(0.36f, 0.36f, 0.36f);
        flash.GetComponent<Renderer>().sharedMaterial = muzzleMaterial;
        RemoveCollider(flash);
        Destroy(flash, 0.12f);
    }

    private static void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private static Material CreateMaterial(string materialName, Color color, bool transparent = false)
    {
        Material sourceMaterial = Resources.Load<Material>("RuntimeMaterials/" + (transparent ? "IslandTransparent" : "IslandOpaque"));
        if (sourceMaterial != null)
        {
            Material clonedMaterial = new Material(sourceMaterial)
            {
                name = materialName
            };
            ApplyMaterialColor(clonedMaterial, color);
            return clonedMaterial;
        }

        Shader shader = transparent ? Shader.Find("Legacy Shaders/Transparent/Diffuse") : Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/VertexLit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            Debug.LogWarning($"Could not create material '{materialName}' because no runtime shader was available.");
            return null;
        }

        Material material = new Material(shader)
        {
            name = materialName
        };
        ApplyMaterialColor(material, color);

        if (transparent)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        return material;
    }

    private static void ApplyMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }
}
