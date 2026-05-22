using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class IslandPlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5.5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float mouseSensitivity = 2.2f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 3.1f, -6.5f);
    [SerializeField] private Vector3 cameraLookOffset = new Vector3(0f, 1.25f, 0f);

    private CharacterController characterController;
    private Camera playerCamera;
    private Animator animator;
    private float verticalVelocity;
    private float pitch;
    private float yaw;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        playerCamera = Camera.main;
        yaw = transform.eulerAngles.y;

        if (playerCamera != null)
        {
            pitch = playerCamera.transform.eulerAngles.x;
        }
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (IslandGameManager.Instance != null && !IslandGameManager.Instance.IsRunning)
        {
            if (animator != null)
            {
                animator.speed = 0f;
            }

            return;
        }

        HandleLook();
        HandleMovement();
    }

    private void LateUpdate()
    {
        if (playerCamera == null)
        {
            return;
        }

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        playerCamera.transform.position = transform.position + cameraRotation * cameraOffset;
        playerCamera.transform.rotation = Quaternion.LookRotation((transform.position + cameraLookOffset) - playerCamera.transform.position, Vector3.up);
    }

    private void HandleLook()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -10f, 55f);
    }

    private void HandleMovement()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        Vector3 horizontalMovement = forward * input.y + right * input.x;

        if (horizontalMovement.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontalMovement, Vector3.up), 16f * Time.deltaTime);
        }

        Vector3 movement = horizontalMovement * CurrentSpeed();

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (characterController.isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;

        characterController.Move(movement * Time.deltaTime);
        UpdateAnimator(input.magnitude);
    }

    private float CurrentSpeed()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? runSpeed : walkSpeed;
    }

    private void UpdateAnimator(float inputAmount)
    {
        if (animator == null)
        {
            return;
        }

        animator.speed = Mathf.Clamp01(inputAmount) * (CurrentSpeed() / walkSpeed);
    }
}
