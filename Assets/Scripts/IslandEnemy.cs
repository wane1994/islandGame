using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class IslandEnemy : MonoBehaviour
{
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] public float patrolSpeed = 2.2f;
    [SerializeField] public float chaseSpeed = 4.2f;
    [SerializeField] private float detectionRange = 32f;
    [SerializeField] private float attackRange = 3.2f;
    [SerializeField] private float attackInterval = 0.65f;
    [SerializeField] public int damage = 12;
    [SerializeField] public int maxHealth = 100;

    private CharacterController controller;
    private Transform player;
    private IslandPlayerHealth playerHealth;
    private Vector3 homePosition;
    private Vector3 targetPosition;
    private float repickTime;
    private float nextAttackTime;
    private float verticalVelocity;
    private Animator animator;
    private Renderer[] renderers;
    private Color[] originalColors;
    private int currentHealth;
    private float flashUntil;
    private bool dead;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    private void Start()
    {
        homePosition = transform.position;
        FindPlayer();
        PickPatrolTarget();
    }

    private void Update()
    {
        UpdateFlash();

        if (dead)
        {
            return;

        }

        if (player == null || playerHealth == null)
        {
            FindPlayer();
        }

        if (player == null || playerHealth == null)
        {
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        bool chasing = toPlayer.magnitude <= detectionRange && IslandGameManager.Instance != null && IslandGameManager.Instance.IsRunning;
        Vector3 destination = chasing ? player.position : targetPosition;
        float speed = chasing ? chaseSpeed : patrolSpeed;

        MoveToward(destination, speed);

        if (chasing && toPlayer.magnitude <= attackRange && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackInterval;
            playerHealth.TakeDamage(damage);
        }

        if (!chasing && (Time.time > repickTime || Vector3.Distance(transform.position, targetPosition) < 1.2f))
        {
            PickPatrolTarget();
        }
    }

    public void TakeDamage(int amount)
    {
        if (dead)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Abs(amount));
        flashUntil = Time.time + 0.12f;
        IslandGameManager.Instance?.PlayHitSound();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void UpdateFlash()
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

            renderers[i].material.color = flashing ? Color.yellow : originalColors[i];
        }
    }

    private void Die()
    {
        dead = true;
        bool isMegaEnemy = GetComponent<IslandMegaEnemy>() != null;
        IslandGameManager.Instance?.EnemyKilled(isMegaEnemy);
        FindObjectOfType<IslandTerrainGenerator>().SpawnHealItem(transform.position);
        Debug.Log("Enemy died, dropping heal items.");
        Destroy(gameObject);
    }

    private void MoveToward(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        Vector3 movement = Vector3.zero;
        if (direction.sqrMagnitude > 0.2f)
        {
            direction.Normalize();
            movement = direction * speed;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 10f * Time.deltaTime);
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += -24f * Time.deltaTime;
        movement.y = verticalVelocity;
        controller.Move(movement * Time.deltaTime);

        if (animator != null)
        {
            animator.speed = direction.sqrMagnitude > 0.2f ? speed / patrolSpeed : 0f;
        }
    }

    private void PickPatrolTarget()
    {
        Vector2 offset = Random.insideUnitCircle * patrolRadius;
        targetPosition = homePosition + new Vector3(offset.x, 0f, offset.y);
        repickTime = Time.time + Random.Range(3f, 7f);
    }

    private void FindPlayer()
    {
        playerHealth = FindObjectOfType<IslandPlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindWithTag("Player");
        player = taggedPlayer != null ? taggedPlayer.transform : null;
        playerHealth = player != null ? player.GetComponent<IslandPlayerHealth>() : null;
    }
}
