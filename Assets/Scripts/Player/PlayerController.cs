using UnityEngine;
using UnityEngine.InputSystem;
using Util;

namespace Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float maxRunSpeed = 8.5f;
        [SerializeField] private float maxAirSpeed = 6f;
        [SerializeField] private float groundAcceleration = 90f;
        [SerializeField] private float groundDeceleration = 85f;
        [SerializeField] private float airAcceleration = 65f;
        [SerializeField] private float airDeceleration = 55f;

        [Header("Jumping")]
        [SerializeField] private float jumpVelocity = 16f;
        [SerializeField] private float risingGravity = -35f;
        [SerializeField] private float fallingGravity = -70f;
        [SerializeField] private float terminalVelocity = -20f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Ground Detection")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.9f, 0.1f);
        [SerializeField] private float groundCheckDistance = 0.05f;
        [SerializeField] private Vector2 ceilingCheckSize = new Vector2(0.9f, 0.1f);
        [SerializeField] private float ceilingCheckDistance = 0.05f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Wall Detection")]
        [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f, 0.8f);
        [SerializeField] private float wallCheckDistance = 0.05f;

        [Header("Wall Grab")]
        [SerializeField] private float wallGrabStamina = 2f;

        [Header("Wall Slide")]
        [SerializeField] private float wallSlideSpeed = -2f;
        [SerializeField] private float wallSlideAcceleration = 20f;

        [Header("Wall Jump")]
        [SerializeField] private float wallJumpVelocityX = 8f;
        [SerializeField] private float wallJumpLockoutTime = 0.15f;
        [SerializeField] private float wallJumpMinKickX = 3f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 24f;
        [SerializeField] private float dashDuration = 0.19f;
        [SerializeField] private float dashAcceleration = 300f;
        [SerializeField] private float dashLockoutTime = 0.15f;
        [SerializeField] private float dashMomentumRetention = 0.3f;

        private Rigidbody2D rb;
        private BoxCollider2D col;
        private PlayerStateMachine stateMachine;

        private Vector2 moveInput;
        private float moveInputX; // horizontal input clamped to -1/0/1 magnitude
        private Vector2 velocity;
        private bool isGrounded;
        private bool wasGrounded;
        private bool jumpHeld;
        private bool grabHeld;

        // Wall state
        private bool isTouchingWallLeft;
        private bool isTouchingWallRight;
        private int wallDirection; // +1 right, -1 left, 0 none
        private bool isOnWall;
        private bool isWallGrabbing;

        private float currentGrabStamina;

        // Ability state (shared by dash + wall jump)
        private Vector2 moveDirection;
        private bool isDashing;
        private bool hasDashCharge = true;
        private float facingDirection = 1f;

        public int WallDirection => wallDirection;
        public float FacingDirection => facingDirection;
        public bool HasDashCharge => hasDashCharge;

        private Timer coyoteTimer = new Timer();
        private Timer jumpBufferTimer = new Timer();
        private Timer landingTimer = new Timer();
        private Timer wallJumpLockoutTimer = new Timer();
        private Timer dashTimer = new Timer();
        private Timer dashLockoutTimer = new Timer();

        private const float LANDING_DURATION = 0.1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            stateMachine = GetComponent<PlayerStateMachine>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // --- Input System callbacks ---

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
            moveInputX = Mathf.Abs(moveInput.x) > 0.1f ? Mathf.Sign(moveInput.x) : 0f;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                jumpBufferTimer.Start(jumpBufferTime);
            }

            jumpHeld = !context.canceled;
        }

        public void OnGrab(InputAction.CallbackContext context)
        {
            grabHeld = !context.canceled;
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.started && !isDashing && hasDashCharge)
            {
                hasDashCharge = false;
                StartDash();
            }
        }

        // --- Physics loop ---

        private void FixedUpdate()
        {
            wasGrounded = isGrounded;

            CheckWalls();
            CheckGround();
            CheckCeiling();
            HandleCoyoteTime();

            HandleWallState();
            HandleDash();
            if (!isDashing)
            {
                ApplyHorizontalMovement();
                ApplyGravity();
            }
            HandleJump();

            rb.linearVelocity = velocity;

            UpdateState();
            TickTimers();
        }

        private void CheckGround()
        {
            Vector2 origin = (Vector2)transform.position + col.offset + Vector2.down * (col.size.y * 0.5f);

            // Shrink and shift ground check away from wall to prevent false grounding
            Vector2 checkSize = groundCheckSize;
            Vector2 checkOrigin = origin + Vector2.down * groundCheckDistance;
            if (wallDirection != 0)
            {
                checkSize.x *= 0.3f;
                checkOrigin.x -= wallDirection * checkSize.x * 0.5f;
            }

            isGrounded = Physics2D.OverlapBox(checkOrigin, checkSize, 0f, groundLayer);

            if (isGrounded)
                currentGrabStamina = wallGrabStamina;
        }

        private void CheckWalls()
        {
            Vector2 center = (Vector2)transform.position + col.offset;
            float halfWidth = col.size.x * 0.5f;

            Vector2 rightOrigin = center + Vector2.right * (halfWidth + wallCheckDistance);
            isTouchingWallRight = Physics2D.OverlapBox(rightOrigin, wallCheckSize, 0f, groundLayer);

            Vector2 leftOrigin = center + Vector2.left * (halfWidth + wallCheckDistance);
            isTouchingWallLeft = Physics2D.OverlapBox(leftOrigin, wallCheckSize, 0f, groundLayer);

            if (isTouchingWallRight && !isTouchingWallLeft)
                wallDirection = 1;
            else if (isTouchingWallLeft && !isTouchingWallRight)
                wallDirection = -1;
            else
                wallDirection = 0;
        }

        private void CheckCeiling()
        {
            if (velocity.y <= 0f) return;

            Vector2 origin = (Vector2)transform.position + col.offset + Vector2.up * (col.size.y * 0.5f);
            bool hitCeiling = Physics2D.OverlapBox(
                origin + Vector2.up * ceilingCheckDistance,
                ceilingCheckSize,
                0f,
                groundLayer
            );

            if (hitCeiling)
            {
                velocity.y = 0f;
            }
        }

        private void HandleCoyoteTime()
        {
            // Just left the ground (didn't jump)
            if (wasGrounded && !isGrounded && velocity.y <= 0f)
            {
                coyoteTimer.Start(coyoteTime);
            }

            // Landed
            if (!wasGrounded && isGrounded)
            {
                coyoteTimer.Stop();
            }
        }

        private void HandleWallState()
        {
            // Early exit: grounded, no wall, or lockout active → detach
            if (isGrounded || wallDirection == 0 || wallJumpLockoutTimer.IsRunning)
            {
                isOnWall = false;
                isWallGrabbing = false;
                return;
            }

            if (grabHeld && currentGrabStamina > 0f)
            {
                isOnWall = true;
                isWallGrabbing = true;
                velocity.x = 0f;
                velocity.y = 0f;
                currentGrabStamina -= Time.fixedDeltaTime;
                return;
            }

            if (velocity.y <= 0f && moveInputX != 0f && (int)moveInputX == wallDirection)
            {
                isOnWall = true;
                isWallGrabbing = false;
                velocity.x = 0f;
                velocity.y = Mathf.MoveTowards(velocity.y, wallSlideSpeed, wallSlideAcceleration * Time.fixedDeltaTime);
                return;
            }

            isOnWall = false;
            isWallGrabbing = false;
        }

        private bool CanJump()
        {
            return isGrounded || coyoteTimer.IsRunning;
        }

        private void HandleJump()
        {
            if (!jumpBufferTimer.IsRunning) return;

            // Ground jump (priority)
            if (CanJump())
            {
                velocity.y = jumpVelocity;
                jumpBufferTimer.Stop();
                coyoteTimer.Stop();
                isGrounded = false;
                return;
            }

            // Wall jump (grab, slide, or just touching wall while airborne)
            if (isOnWall || (!isGrounded && wallDirection != 0))
            {
                if (isWallGrabbing && moveInputX != -wallDirection)
                {
                    // Grab + no away input → small kick to prevent hover
                    velocity.x = -wallDirection * wallJumpMinKickX;
                }
                else
                {
                    // Grab + pressing away, slide, or freefall touch → kick away from wall
                    velocity.x = -wallDirection * wallJumpVelocityX;
                }

                velocity.y = jumpVelocity;
                moveDirection = new Vector2(-wallDirection, 0f);
                jumpBufferTimer.Stop();
                wallJumpLockoutTimer.Start(wallJumpLockoutTime);
                isOnWall = false;
                isWallGrabbing = false;
            }
        }

        private Vector2 GetDashDirection()
        {
            if (moveInput.sqrMagnitude > 0.01f)
                return moveInput.normalized;
            return new Vector2(facingDirection, 0f);
        }

        private void StartDash()
        {
            isDashing = true;
            moveDirection = GetDashDirection();
            dashTimer.Start(dashDuration);
            velocity = Vector2.zero;
            if (Mathf.Abs(moveDirection.x) > 0.01f)
                facingDirection = Mathf.Sign(moveDirection.x);
            stateMachine.ChangeState(PlayerState.Dashing);
        }

        private void EndDash()
        {
            isDashing = false;
            dashTimer.Stop();
            velocity = moveDirection * (dashSpeed * dashMomentumRetention);
            dashLockoutTimer.Start(dashLockoutTime);
        }

        private void HandleDash()
        {
            if (isGrounded && !isDashing)
                hasDashCharge = true;

            if (!isDashing) return;
            if (!dashTimer.IsRunning) { EndDash(); return; }

            // Wall cancellation
            if ((moveDirection.x > 0f && isTouchingWallRight) ||
                (moveDirection.x < 0f && isTouchingWallLeft))
            {
                EndDash();
                velocity.x = 0f;
                return;
            }

            velocity = Vector2.MoveTowards(velocity, moveDirection * dashSpeed, dashAcceleration * Time.fixedDeltaTime);
        }

        private float GetLockoutFactor()
        {
            if (dashLockoutTimer.IsRunning)
                return 1f - (dashLockoutTimer.TimeRemaining / dashLockoutTimer.Duration);
            if (wallJumpLockoutTimer.IsRunning)
                return 1f - (wallJumpLockoutTimer.TimeRemaining / wallJumpLockoutTimer.Duration);
            return 1f;
        }

        private void ApplyHorizontalMovement()
        {
            if (isOnWall) return;

            float maxSpeed = isGrounded ? maxRunSpeed : maxAirSpeed;
            float targetSpeed = moveInputX * maxSpeed;
            float lockout = GetLockoutFactor();

            float baseAccel = isGrounded
                ? (Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration)
                : (Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration);
            float acceleration = baseAccel * lockout;

            // Preserve above-max momentum while pressing the same direction
            if (Mathf.Abs(velocity.x) > maxSpeed && Mathf.Sign(velocity.x) == Mathf.Sign(moveInputX))
                return;

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);

            if (Mathf.Abs(velocity.x) > 0.1f)
                facingDirection = Mathf.Sign(velocity.x);
        }

        private void ApplyGravity()
        {
            if (isOnWall) return;

            if (isGrounded && velocity.y <= 0f)
            {
                // Stick to ground
                velocity.y = 0f;
                return;
            }

            // Variable height: low gravity while rising + holding jump, high gravity otherwise
            float gravity = (velocity.y > 0f && jumpHeld) ? risingGravity : fallingGravity;
            velocity.y += gravity * Time.fixedDeltaTime;
            velocity.y = Mathf.Max(velocity.y, terminalVelocity);
        }

        private void UpdateState()
        {
            if (isDashing) return;

            // Wall state takes priority over airborne states
            if (isOnWall && !isGrounded)
            {
                stateMachine.ChangeState(isWallGrabbing ? PlayerState.WallGrab : PlayerState.WallSliding);
                return;
            }

            if (isGrounded)
            {
                if (!wasGrounded)
                {
                    landingTimer.Start(LANDING_DURATION);
                    stateMachine.ChangeState(PlayerState.Landing);
                    return;
                }

                if (landingTimer.IsRunning) return;

                if (Mathf.Abs(velocity.x) > 0.1f)
                    stateMachine.ChangeState(PlayerState.Running);
                else
                    stateMachine.ChangeState(PlayerState.Idle);
            }
            else
            {
                if (velocity.y > 0f)
                    stateMachine.ChangeState(PlayerState.Jumping);
                else
                    stateMachine.ChangeState(PlayerState.Falling);
            }
        }

        private void TickTimers()
        {
            float dt = Time.fixedDeltaTime;
            coyoteTimer.Tick(dt);
            jumpBufferTimer.Tick(dt);
            landingTimer.Tick(dt);
            wallJumpLockoutTimer.Tick(dt);
            dashTimer.Tick(dt);
            dashLockoutTimer.Tick(dt);
        }

        // --- Gizmos ---

        private void OnDrawGizmosSelected()
        {
            if (col == null) col = GetComponent<BoxCollider2D>();
            if (col == null) return;

            // Ground check
            Vector2 origin = (Vector2)transform.position + col.offset + Vector2.down * (col.size.y * 0.5f);
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(origin + Vector2.down * groundCheckDistance, groundCheckSize);

            // Ceiling check
            Vector2 ceilingOrigin = (Vector2)transform.position + col.offset + Vector2.up * (col.size.y * 0.5f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(ceilingOrigin + Vector2.up * ceilingCheckDistance, ceilingCheckSize);

            // Wall checks
            Vector2 center = (Vector2)transform.position + col.offset;
            float halfWidth = col.size.x * 0.5f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center + Vector2.right * (halfWidth + wallCheckDistance), wallCheckSize);
            Gizmos.DrawWireCube(center + Vector2.left * (halfWidth + wallCheckDistance), wallCheckSize);
        }
    }
}
