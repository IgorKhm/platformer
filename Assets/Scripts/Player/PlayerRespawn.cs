using System.Collections;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStateMachine))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private float respawnDelay = 0.1f;

        private Rigidbody2D rb;
        private PlayerStateMachine stateMachine;
        private PlayerController controller;

        private Vector3 respawnPosition;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<PlayerStateMachine>();
            controller = GetComponent<PlayerController>();
            respawnPosition = transform.position;
        }

        public void Respawn()
        {
            if (stateMachine.CurrentState == PlayerState.Dead) return;
            stateMachine.ChangeState(PlayerState.Dead);
            controller.ResetMotionState();
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            controller.ResetMotionState();
            rb.linearVelocity = Vector2.zero;
            transform.position = respawnPosition;
            stateMachine.ChangeState(PlayerState.Idle);
        }

        public void SetRespawnPosition(Vector3 position)
        {
            respawnPosition = position;
        }
    }
}
