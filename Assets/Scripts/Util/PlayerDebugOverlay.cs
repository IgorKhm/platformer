using Player;
using UnityEngine;

namespace Util
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStateMachine))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerDebugOverlay : MonoBehaviour
    {
        private Rigidbody2D rb;
        private PlayerStateMachine stateMachine;
        private PlayerController playerController;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<PlayerStateMachine>();
            playerController = GetComponent<PlayerController>();
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            var velocity = rb.linearVelocity;
            float speed = velocity.magnitude;

            int wallDir = playerController.WallDirection;
            string wallLabel = wallDir > 0 ? "right" : wallDir < 0 ? "left" : "none";

            string text =
                $"State: {stateMachine.CurrentState}\n" +
                $"Velocity: ({velocity.x:F1}, {velocity.y:F1})\n" +
                $"Speed: {speed:F1}\n" +
                $"Grounded: {stateMachine.IsGrounded}\n" +
                $"Wall: {wallLabel}";

            GUI.skin.label.fontSize = 18;
            GUI.skin.label.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 10, 400, 140), text);
        }
#endif
    }
}
