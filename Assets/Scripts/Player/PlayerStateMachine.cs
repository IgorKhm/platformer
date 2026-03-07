using UnityEngine;

namespace Player
{
    public enum PlayerState
    {
        Idle,
        Running,
        Jumping,
        Falling,
        Landing
    }

    public class PlayerStateMachine : MonoBehaviour
    {
        public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
        public PlayerState PreviousState { get; private set; } = PlayerState.Idle;

        public void ChangeState(PlayerState newState)
        {
            if (newState == CurrentState) return;

            PreviousState = CurrentState;
            CurrentState = newState;

            #if UNITY_EDITOR
            Debug.Log($"State: {PreviousState} → {newState}");
            #endif
        }
    }
}
