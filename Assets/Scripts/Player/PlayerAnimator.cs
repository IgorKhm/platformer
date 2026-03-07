using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(PlayerStateMachine))]
    public class PlayerAnimator : MonoBehaviour
    {
        private PlayerStateMachine stateMachine;
        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        private static readonly int IsRunning = Animator.StringToHash("isRunning");
        private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
        private static readonly int YVelocity = Animator.StringToHash("yVelocity");

        private float lastNonZeroX;
        private bool hasAnimator;
        private bool hasSpriteRenderer;
        private bool hasRb;

        private void Awake()
        {
            stateMachine = GetComponent<PlayerStateMachine>();
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            hasRb = rb != null;
            hasAnimator = animator != null;
            hasSpriteRenderer = spriteRenderer != null;

            if (!hasAnimator)
                Debug.LogWarning("PlayerAnimator: No Animator found. Animation parameters won't be set.");

            if (!hasSpriteRenderer)
                Debug.LogWarning("PlayerAnimator: No SpriteRenderer found. Sprite flipping won't work.");
        }

        private void LateUpdate()
        {
            HandleSpriteFlip();
            UpdateAnimatorParameters();
        }

        private void HandleSpriteFlip()
        {
            if (!hasSpriteRenderer || !hasRb) return;

            float xVel = rb.linearVelocity.x;
            if (Mathf.Abs(xVel) > 0.1f)
            {
                lastNonZeroX = xVel;
            }

            if (Mathf.Abs(lastNonZeroX) > 0.1f)
            {
                spriteRenderer.flipX = lastNonZeroX < 0f;
            }
        }

        private void UpdateAnimatorParameters()
        {
            if (!hasAnimator) return;

            var state = stateMachine.CurrentState;

            animator.SetBool(IsRunning, state == PlayerState.Running);
            animator.SetBool(IsGrounded, stateMachine.IsGrounded);

            if (hasRb)
                animator.SetFloat(YVelocity, rb.linearVelocity.y);
        }
    }
}
