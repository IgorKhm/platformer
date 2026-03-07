using UnityEngine;
using Unity.Cinemachine;

namespace Camera
{
    [RequireComponent(typeof(Collider2D))]
    public class CameraZoneTransition : MonoBehaviour
    {
        private Collider2D zoneCollider;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider2D>();
            zoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            // Find the active CinemachineCamera with a CinemachineConfiner2D
            var vcam = FindAnyObjectByType<CinemachineCamera>();
            if (vcam == null)
            {
                Debug.LogWarning("CameraZoneTransition: No CinemachineCamera found.");
                return;
            }

            var confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner == null)
            {
                Debug.LogWarning("CameraZoneTransition: No CinemachineConfiner2D on virtual camera.");
                return;
            }

            confiner.BoundingShape2D = zoneCollider;
            confiner.InvalidateBoundingShapeCache();
        }
    }
}
