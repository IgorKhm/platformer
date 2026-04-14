using Player;
using UnityEngine;

namespace Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            var respawn = other.GetComponent<PlayerRespawn>();
            if (respawn != null)
                respawn.Respawn();
        }
    }
}
