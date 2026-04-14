using Player;
using UnityEngine;
using Unity.Cinemachine;

namespace Levels
{
    /// <summary>
    /// Represents a playable level/area. Owns the camera priority swap
    /// (via Cinemachine) and the respawn point for this area.
    /// Attached to the Level prefab root; its Collider2D defines the
    /// trigger region the player must enter to activate this level.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Level : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera vcam;

        [Tooltip("Priority when this level is active (higher = this camera wins)")]
        [SerializeField] private int activePriority = 1;

        [Tooltip("Priority when this level is inactive")]
        [SerializeField] private int inactivePriority = 0;

        [Tooltip("Where the player respawns when dying in this level (optional)")]
        [SerializeField] private Transform spawnPoint;

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
                Debug.LogWarning($"Level on {name}: no vcam assigned.");
                return;
            }

            vcam.Priority = activePriority;

            if (spawnPoint != null)
            {
                var respawner = other.GetComponent<PlayerRespawn>();
                if (respawner != null)
                    respawner.SetRespawnPosition(spawnPoint.position);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (vcam != null)
                vcam.Priority = inactivePriority;
        }
    }
}
