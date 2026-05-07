using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Pogo pogoComponent;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float groundAcceleration = 70f;
    [SerializeField] private float airAcceleration = 35f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Gravity & Falling")]
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float fallGravityMultiplier = 1.6f;
    [SerializeField] private float maxFallSpeed = 25f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField, Range(0.05f, 1f)] private float jumpCutMultiplier = 0.45f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBuffer = 0.10f;

    [Header("Double Jump")]
    [SerializeField] private int maxAirJumps = 1;
    [SerializeField] private float doubleJumpHeight = 2.2f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 24f;
    [SerializeField] private float dashDuration = 0.16f;
    [SerializeField] private float dashCooldown = 0.35f;
    [SerializeField] private int airDashes = 1;
    [SerializeField, Range(0f, 1f)] private float dashEndSpeedKeep = 0.45f;

    [Header("Glide")]
    [SerializeField] private float glideMaxFallSpeed = 3f;
    [SerializeField] private float glideMaxDuration = 1.5f;

    [Header("Wall")]
    [SerializeField] private float wallCheckDistance = 0.55f;
    [SerializeField] private float wallSlideSpeed = 3f;
    [SerializeField] private float wallJumpUp = 12f;
    [SerializeField] private float wallJumpAway = 11f;
    [SerializeField] private float wallJumpControlLock = 0.18f;
    [SerializeField, Range(0.05f, 1f)] private float minPressIntoWall = 0.3f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Ground")]
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, -0.95f, 0f);
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;

    public bool IsGrounded { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsGliding { get; private set; }

    public bool DashAvailable => dashCooldownTimer <= 0f && (IsGrounded || dashesLeft > 0);
    public bool DoubleJumpAvailable => airJumpsLeft > 0;
    public float GlideRemainingNormalized => Mathf.Clamp01(glideRemaining / Mathf.Max(0.0001f, glideMaxDuration));

    public Rigidbody Body => rb;

    private Rigidbody rb;
    private CapsuleCollider col;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction pogoAction;
    private InputAction glideAction;

    private Vector2 moveInput;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private int airJumpsLeft;
    private int dashesLeft;
    private float dashCooldownTimer;
    private float dashTimer;
    private Vector3 dashDirection;
    private float glideRemaining;
    private float wallJumpLockTimer;

    private bool wallDetected;
    private Vector3 wallNormal;

    private Vector3 spawnPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        if (pogoComponent == null) pogoComponent = GetComponent<Pogo>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (inputActions == null)
        {
            Debug.LogError("[PlayerController] InputActionAsset не назначен в инспекторе.");
            enabled = false;
            return;
        }

        var map = inputActions.FindActionMap("Player", true);
        moveAction = map.FindAction("Move", true);
        lookAction = map.FindAction("Look", true);
        jumpAction = map.FindAction("Jump", true);
        dashAction = map.FindAction("Dash", true);
        pogoAction = map.FindAction("Pogo", true);
        glideAction = map.FindAction("Glide", true);

        spawnPoint = transform.position;
        airJumpsLeft = maxAirJumps;
        dashesLeft = airDashes;
        glideRemaining = glideMaxDuration;
    }

    private void OnEnable()
    {
        if (moveAction == null) return;

        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        dashAction.Enable();
        pogoAction.Enable();
        glideAction.Enable();

        jumpAction.performed += OnJumpPressed;
        jumpAction.canceled += OnJumpReleased;
        dashAction.performed += OnDashPressed;
        pogoAction.performed += OnPogoPressed;
    }

    private void OnDisable()
    {
        if (moveAction == null) return;

        jumpAction.performed -= OnJumpPressed;
        jumpAction.canceled -= OnJumpReleased;
        dashAction.performed -= OnDashPressed;
        pogoAction.performed -= OnPogoPressed;

        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        dashAction.Disable();
        pogoAction.Disable();
        glideAction.Disable();
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        if (jumpBufferCounter > 0f) jumpBufferCounter -= Time.deltaTime;
        if (coyoteCounter > 0f) coyoteCounter -= Time.deltaTime;
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;
        if (wallJumpLockTimer > 0f) wallJumpLockTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        UpdateGroundCheck();
        UpdateWallCheck();

        if (IsDashing)
        {
            UpdateDash();
            return;
        }

        UpdateWallSlide();
        UpdateGlide();
        UpdateMovement();
        UpdateJumps();
        UpdateGravity();
        ClampFallSpeed();
        UpdateRotation();
    }

    private void OnJumpPressed(InputAction.CallbackContext ctx) => jumpBufferCounter = jumpBuffer;

    private void OnJumpReleased(InputAction.CallbackContext ctx)
    {
        if (rb.linearVelocity.y > 0f && !IsDashing)
        {
            Vector3 v = rb.linearVelocity;
            v.y *= jumpCutMultiplier;
            rb.linearVelocity = v;
        }
    }

    private void OnDashPressed(InputAction.CallbackContext ctx) => TryStartDash();

    private void OnPogoPressed(InputAction.CallbackContext ctx)
    {
        if (IsGrounded) return;
        if (pogoComponent != null) pogoComponent.PerformPogoAttack();
    }

    private void UpdateGroundCheck()
    {
        Vector3 origin = transform.position + groundCheckOffset;
        IsGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);

        if (IsGrounded)
        {
            coyoteCounter = coyoteTime;
            airJumpsLeft = maxAirJumps;
            dashesLeft = airDashes;
            glideRemaining = glideMaxDuration;
        }
    }

    private void UpdateWallCheck()
    {
        wallDetected = false;
        wallNormal = Vector3.zero;

        Vector3 desired = GetCameraRelativeMoveDir();
        if (desired.sqrMagnitude < 0.01f) return;

        if (Physics.Raycast(transform.position, desired.normalized, out RaycastHit hit, wallCheckDistance, wallLayer, QueryTriggerInteraction.Ignore))
        {
            if (Mathf.Abs(hit.normal.y) < 0.35f)
            {
                wallDetected = true;
                wallNormal = hit.normal;
            }
        }
    }

    private Vector3 GetCameraRelativeMoveDir()
    {
        Vector3 fwd = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
        fwd.y = 0f; right.y = 0f;
        fwd.Normalize(); right.Normalize();

        Vector3 dir = fwd * moveInput.y + right * moveInput.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        return dir;
    }

    private void UpdateMovement()
    {
        if (wallJumpLockTimer > 0f) return;

        Vector3 desired = GetCameraRelativeMoveDir() * moveSpeed;
        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        float accel = IsGrounded ? groundAcceleration : airAcceleration;
        Vector3 newH = Vector3.MoveTowards(horizontal, desired, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newH.x, v.y, newH.z);
    }

    private void UpdateRotation()
    {
        Vector3 dir = GetCameraRelativeMoveDir();
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.fixedDeltaTime);
    }

    private void UpdateJumps()
    {
        if (jumpBufferCounter <= 0f) return;

        if (IsWallSliding || (wallDetected && !IsGrounded))
        {
            DoWallJump();
            jumpBufferCounter = 0f;
            return;
        }

        if (coyoteCounter > 0f)
        {
            DoJump(jumpHeight);
            coyoteCounter = 0f;
            jumpBufferCounter = 0f;
            return;
        }

        if (airJumpsLeft > 0)
        {
            DoJump(doubleJumpHeight);
            airJumpsLeft--;
            jumpBufferCounter = 0f;
        }
    }

    private void DoJump(float height)
    {
        float v0 = Mathf.Sqrt(2f * gravity * height);
        Vector3 v = rb.linearVelocity;
        v.y = v0;
        rb.linearVelocity = v;
        glideRemaining = glideMaxDuration;
    }

    private void DoWallJump()
    {
        rb.linearVelocity = wallNormal * wallJumpAway + Vector3.up * wallJumpUp;
        wallJumpLockTimer = wallJumpControlLock;
        airJumpsLeft = maxAirJumps;
        dashesLeft = airDashes;
        glideRemaining = glideMaxDuration;

        Vector3 face = new Vector3(wallNormal.x, 0f, wallNormal.z);
        if (face.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);
    }

    private void UpdateWallSlide()
    {
        IsWallSliding = false;
        if (IsGrounded || !wallDetected) return;
        if (rb.linearVelocity.y > 0.1f) return;

        if (Vector3.Dot(GetCameraRelativeMoveDir(), -wallNormal) < minPressIntoWall) return;

        IsWallSliding = true;
        Vector3 v = rb.linearVelocity;
        v.y = Mathf.Max(v.y, -wallSlideSpeed);
        rb.linearVelocity = v;

        airJumpsLeft = Mathf.Max(airJumpsLeft, maxAirJumps);
    }

    private bool TryStartDash()
    {
        if (IsDashing) return false;
        if (dashCooldownTimer > 0f) return false;
        if (!IsGrounded && dashesLeft <= 0) return false;

        Vector3 dir = GetCameraRelativeMoveDir();
        if (dir.sqrMagnitude < 0.01f)
        {
            dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
        }

        dashDirection = dir;
        if (!IsGrounded) dashesLeft--;
        IsDashing = true;
        IsGliding = false;
        dashTimer = dashDuration;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        return true;
    }

    private void UpdateDash()
    {
        rb.linearVelocity = dashDirection * dashSpeed;
        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer <= 0f)
        {
            IsDashing = false;
            dashCooldownTimer = dashCooldown;
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x * dashEndSpeedKeep, 0f, v.z * dashEndSpeedKeep);
        }
    }

    private void UpdateGlide()
    {
        IsGliding = false;
        if (IsGrounded || IsWallSliding) return;
        if (!glideAction.IsPressed()) return;
        if (rb.linearVelocity.y > 0.1f) return;
        if (glideRemaining <= 0f) return;

        IsGliding = true;
        glideRemaining -= Time.fixedDeltaTime;

        Vector3 v = rb.linearVelocity;
        v.y = Mathf.Max(v.y, -glideMaxFallSpeed);
        rb.linearVelocity = v;
    }

    private void UpdateGravity()
    {
        if (IsGrounded && rb.linearVelocity.y <= 0f)
        {
            Vector3 v = rb.linearVelocity;
            v.y = -2f;
            rb.linearVelocity = v;
            return;
        }

        float g = gravity;
        if (rb.linearVelocity.y < 0f) g *= fallGravityMultiplier;
        rb.linearVelocity += Vector3.down * (g * Time.fixedDeltaTime);
    }

    private void ClampFallSpeed()
    {
        float cap = IsGliding ? glideMaxFallSpeed : maxFallSpeed;
        Vector3 v = rb.linearVelocity;
        if (v.y < -cap)
        {
            v.y = -cap;
            rb.linearVelocity = v;
        }
    }

    public void OnPogoBounce(float bounceVelocity)
    {
        Vector3 v = rb.linearVelocity;
        v.y = bounceVelocity;
        rb.linearVelocity = v;
        airJumpsLeft = maxAirJumps;
        dashesLeft = airDashes;
        glideRemaining = glideMaxDuration;
        IsDashing = false;
    }

    public void Teleport(Vector3 position)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        transform.position = position;

        IsDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        airJumpsLeft = maxAirJumps;
        dashesLeft = airDashes;
        glideRemaining = glideMaxDuration;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        wallJumpLockTimer = 0f;
    }

    public Vector3 SpawnPoint => spawnPoint;
    public void SetSpawnPoint(Vector3 p) => spawnPoint = p;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckRadius);

        Gizmos.color = wallDetected ? Color.red : Color.cyan;
        Vector3 fwd = Application.isPlaying ? GetCameraRelativeMoveDir() : transform.forward;
        if (fwd.sqrMagnitude > 0.01f)
            Gizmos.DrawLine(transform.position, transform.position + fwd.normalized * wallCheckDistance);
    }
}
