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
        [SerializeField] private float wallJumpVelocityY = 14f;
        [SerializeField] private float wallJumpLockoutTime = 0.15f;

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

        public int WallDirection => wallDirection;

        private Timer coyoteTimer = new Timer();
        private Timer jumpBufferTimer = new Timer();
        private Timer landingTimer = new Timer();
        private Timer wallJumpLockoutTimer = new Timer();

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

        // --- Physics loop ---

        private void FixedUpdate()
        {
            wasGrounded = isGrounded;

            CheckGround();
            CheckWalls();
            CheckCeiling();
            HandleCoyoteTime();
            HandleWallState();
            ApplyHorizontalMovement();
            ApplyGravity();
            HandleJump();

            rb.linearVelocity = velocity;

            UpdateState();
            TickTimers();
        }

        private void CheckGround()
        {
            Vector2 origin = (Vector2)transform.position + col.offset + Vector2.down * (col.size.y * 0.5f);
            isGrounded = Physics2D.OverlapBox(
                origin + Vector2.down * groundCheckDistance,
                groundCheckSize,
                0f,
                groundLayer
            );

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
                    // Grab + no away input → jump straight up
                    velocity.x = 0f;
                }
                else
                {
                    // Grab + pressing away, slide, or freefall touch → kick away from wall
                    velocity.x = -wallDirection * wallJumpVelocityX;
                }

                velocity.y = wallJumpVelocityY;
                jumpBufferTimer.Stop();
                wallJumpLockoutTimer.Start(wallJumpLockoutTime);
                isOnWall = false;
                isWallGrabbing = false;
            }
        }

        private void ApplyHorizontalMovement()
        {
            if (isOnWall || wallJumpLockoutTimer.IsRunning) return;

            float maxSpeed = isGrounded ? maxRunSpeed : maxAirSpeed;
            float targetSpeed = moveInputX * maxSpeed;
            float accel;

            if (isGrounded)
            {
                accel = Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration;
            }
            else
            {
                accel = Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration;

                // Preserve ground speed — don't clamp if already going faster
                if (Mathf.Abs(velocity.x) > maxAirSpeed && Mathf.Sign(velocity.x) == Mathf.Sign(moveInputX))
                    return;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * Time.fixedDeltaTime);
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
