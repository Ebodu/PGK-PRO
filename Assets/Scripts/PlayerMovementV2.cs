using System.Collections;
using UnityEngine;

public class PlayerMovementAdvancedV2 : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 3f;

    [Header("Drag")]
    public float groundDrag = 5f;
    public float slideDrag = 0f;

    [Header("Jumping")]
    public float jumpForce = 10f;
    public float jumpCooldown = 0.25f;
    public float airMultiplier = 0.4f;
    [Tooltip("Maksymalny kąt odchylenia lotu od kierunku wybicia (0 = bez ograniczeń)")]
    public float airControlMaxAngle = 0f;

    bool readyToJump = true;

    [Header("Crouching & Sliding")]
    public float crouchYScale = 0.5f;
    public float slideMaxDuration = 1f;
    public float slideMinSpeed = 0.5f;

    private float startYScale;
    private bool isSliding;
    private float slideTimer;
    private bool crouchHeld;
    private bool slideOnLanding;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 45f;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        sliding,
        air,
        freeze,
        grappling
    }

    // ── Grappling support ──────────────────────────────────────────
    [HideInInspector] public bool freeze = false;
    [HideInInspector] public bool activeGrapple = false;

    private Vector3 launchDirection;

    // ── Velocity tracking (used by JumpToPosition) ─────────────────
    private bool enableMovementOnNextTouch = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
        startYScale = transform.localScale.y;
        launchDirection = transform.forward;
        moveSpeed = walkSpeed;
    }

    private void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        crouchHeld = Input.GetKey(crouchKey);

        MyInput();
        SpeedControl();
        StateHandler();

        // Drag
        if (freeze)
        {
            rb.linearDamping = 0f;
            return;
        }

        if (activeGrapple) return;

        if (grounded && !isSliding)
            rb.linearDamping = groundDrag;
        else if (isSliding)
            rb.linearDamping = slideDrag;
        else
            rb.linearDamping = 0f;

        UpdateCrouchScale();
    }

    private void FixedUpdate()
    {
        MovePlayer();
        HandleSlideTimer();
    }

    private void UpdateCrouchScale()
    {
        if (isSliding) return;

        if (crouchHeld)
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        else
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
    }

    private void MyInput()
    {
        if (freeze || activeGrapple) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && readyToJump && grounded && !isSliding)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (crouchHeld && grounded && state == MovementState.sprinting && !isSliding)
            StartSlide();

        if (!grounded && crouchHeld && state == MovementState.sprinting)
            slideOnLanding = true;

        if (grounded && slideOnLanding && !isSliding)
        {
            StartSlide();
            slideOnLanding = false;
        }

        if (!crouchHeld && isSliding)
            EndSlide();
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideMaxDuration;
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        slideOnLanding = false;
    }

    private void EndSlide()
    {
        isSliding = false;
        slideOnLanding = false;
    }

    private void HandleSlideTimer()
    {
        if (!isSliding) return;

        slideTimer -= Time.fixedDeltaTime;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude < slideMinSpeed || slideTimer <= 0f)
            EndSlide();
    }

    private void StateHandler()
    {
        if (freeze)
        {
            state = MovementState.freeze;
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (activeGrapple)
        {
            state = MovementState.grappling;
            return;
        }

        if (isSliding)
        {
            state = MovementState.sliding;
            moveSpeed = sprintSpeed;
            return;
        }

        if (crouchHeld && grounded)
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
        else
        {
            state = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        if (freeze || activeGrapple) return;

        if (!isSliding)
        {
            moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

            if (OnSlope() && !exitingSlope)
            {
                rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);
                if (rb.linearVelocity.y > 0)
                    rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
            else if (grounded)
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            }
            else if (!grounded)
            {
                Vector3 forceDir = moveDirection.normalized;
                if (airControlMaxAngle > 0f && launchDirection != Vector3.zero)
                    forceDir = Vector3.RotateTowards(launchDirection, forceDir, Mathf.Deg2Rad * airControlMaxAngle, 0f);

                rb.AddForce(forceDir * moveSpeed * 10f * airMultiplier, ForceMode.Force);
            }

            rb.useGravity = !OnSlope();
        }
        else
        {
            Vector3 inputDir = (orientation.forward * verticalInput + orientation.right * horizontalInput).normalized;
            rb.AddForce(inputDir * moveSpeed * 5f, ForceMode.Force);
            rb.useGravity = true;
        }
    }

    private void SpeedControl()
    {
        if (activeGrapple) return;
        if (isSliding) return;

        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;
        isSliding = false;

        launchDirection = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).normalized;
        if (launchDirection == Vector3.zero)
            launchDirection = orientation.forward;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    // ── Grappling: skok w kierunku punktu ─────────────────────────────────────
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        // Oblicz wektor prędkości potrzebny, by wylądować w targetPosition
        // na podstawie fizyki (kinematyka w osi Y)
        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);

        // Zastosujemy prędkość w następnej klatce, by uniknąć konfliktów z fizyką
        Invoke(nameof(SetVelocity), 0.1f);

        // Wyłącz grappling po dotarciu (safeguard)
        Invoke(nameof(ResetRestrictions), 3f);
    }

    private Vector3 velocityToSet;

    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.linearVelocity = velocityToSet;
    }

    private void ResetRestrictions()
    {
        activeGrapple = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();

            // Anuluj opóźnione wywołania resetowania
            GetComponent<Grappling>()?.StopGrapple();
        }
    }

    // Kinematyka balistyczna – oblicza v0 potrzebne by osiągnąć cel z danym szczytem toru
    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        // Czas do szczytu i czas do ziemi
        float timeToTop = Mathf.Sqrt(-2f * trajectoryHeight / gravity);
        float timeToGround = Mathf.Sqrt(2f * (displacementY - trajectoryHeight) / gravity);

        // Ujemny czas oznaczałby problem – ochrona
        if (float.IsNaN(timeToTop) || float.IsNaN(timeToGround))
            return (endPoint - startPoint).normalized * 10f;

        float totalTime = timeToTop + Mathf.Abs(timeToGround);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2f * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / totalTime;

        return velocityXZ + velocityY;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}