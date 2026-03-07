using Player;
using UnityEngine;

namespace Util
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public class PlayerDebugOverlay : MonoBehaviour
    {
        private Rigidbody2D rb;
        private PlayerStateMachine stateMachine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = GetComponent<PlayerStateMachine>();
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            var velocity = rb.linearVelocity;
            float speed = velocity.magnitude;

            string text =
                $"State: {stateMachine.CurrentState}\n" +
                $"Velocity: ({velocity.x:F1}, {velocity.y:F1})\n" +
                $"Speed: {speed:F1}\n" +
                $"Grounded: {stateMachine.IsGrounded}";

            GUI.skin.label.fontSize = 18;
            GUI.skin.label.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 10, 400, 120), text);
        }
#endif
    }
}
