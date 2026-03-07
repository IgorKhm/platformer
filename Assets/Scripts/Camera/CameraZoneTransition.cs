using UnityEngine;
using Unity.Cinemachine;

namespace CameraSystem
{
    /// <summary>
    /// Each zone has its own CinemachineCamera with a CinemachineConfiner2D.
    /// When the player enters a zone, that zone's vcam gets high priority,
    /// and CinemachineBrain blends smoothly between them.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CameraZoneTransition : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera vcam;

        [Tooltip("Priority when this zone is active (higher = this camera wins)")]
        [SerializeField] private int activePriority = 1;

        [Tooltip("Priority when this zone is inactive")]
        [SerializeField] private int inactivePriority = 0;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            if (vcam != null)
                vcam.Priority = inactivePriority;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (vcam == null)
            {
                Debug.LogWarning($"CameraZoneTransition on {name}: no vcam assigned.");
                return;
            }

            vcam.Priority = activePriority;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (vcam != null)
                vcam.Priority = inactivePriority;
        }
    }
}
