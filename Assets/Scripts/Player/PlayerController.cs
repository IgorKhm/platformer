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

        private Rigidbody2D rb;
        private BoxCollider2D col;
        private PlayerStateMachine stateMachine;

        private Vector2 moveInput;
        private Vector2 velocity;
        private bool isGrounded;
        private bool wasGrounded;
        private bool jumpHeld;

        private Timer coyoteTimer = new Timer();
        private Timer jumpBufferTimer = new Timer();
        private Timer landingTimer = new Timer();

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
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                jumpBufferTimer.Start(jumpBufferTime);
            }

            jumpHeld = !context.canceled;
        }

        // --- Physics loop ---

        private void FixedUpdate()
        {
            wasGrounded = isGrounded;
            
            CheckGround();
            CheckCeiling();
            HandleCoyoteTime();
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

        private bool CanJump()
        {
            return isGrounded || coyoteTimer.IsRunning;
        }

        private void HandleJump()
        {
            // Consume buffered jump
            if (jumpBufferTimer.IsRunning && CanJump())
            {
                velocity.y = jumpVelocity;
                jumpBufferTimer.Stop();
                coyoteTimer.Stop();
                isGrounded = false;
            }

        }

        private void ApplyHorizontalMovement()
        {
            float maxSpeed = isGrounded ? maxRunSpeed : maxAirSpeed;
            float targetSpeed = moveInput.x * maxSpeed;
            float accel;

            if (isGrounded)
            {
                accel = Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration;
            }
            else
            {
                accel = Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration;

                // Preserve ground speed — don't clamp if already going faster
                if (Mathf.Abs(velocity.x) > maxAirSpeed && Mathf.Sign(velocity.x) == Mathf.Sign(moveInput.x))
                    return;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * Time.fixedDeltaTime);
        }

        private void ApplyGravity()
        {
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
        }

        // --- Gizmos for ground check visualization ---

        private void OnDrawGizmosSelected()
        {
            if (col == null) col = GetComponent<BoxCollider2D>();
            if (col == null) return;

            Vector2 origin = (Vector2)transform.position + col.offset + Vector2.down * (col.size.y * 0.5f);
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(origin + Vector2.down * groundCheckDistance, groundCheckSize);

            Vector2 ceilingOrigin = (Vector2)transform.position + col.offset + Vector2.up * (col.size.y * 0.5f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(ceilingOrigin + Vector2.up * ceilingCheckDistance, ceilingCheckSize);
        }
    }
}
