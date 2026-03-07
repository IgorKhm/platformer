using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    public class MinimapController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera minimapCamera;
        [SerializeField] private RawImage minimapDisplay;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform playerIcon;

        [Header("Settings")]
        [SerializeField] private float orthoSize = 40f;
        [SerializeField] private int textureSize = 256;

        private RenderTexture renderTexture;
        private bool hasMinimapCamera;
        private bool hasMinimapDisplay;
        private bool hasPlayer;
        private bool hasPlayerIcon;

        private void Start()
        {
            hasMinimapCamera = minimapCamera != null;
            hasMinimapDisplay = minimapDisplay != null;
            hasPlayer = playerTransform != null;
            hasPlayerIcon = playerIcon != null;

            if (!hasMinimapCamera)
            {
                Debug.LogWarning("MinimapController: No minimap camera assigned.");
                return;
            }

            renderTexture = new RenderTexture(textureSize, textureSize, 16);
            minimapCamera.targetTexture = renderTexture;
            minimapCamera.orthographicSize = orthoSize;

            if (hasMinimapDisplay)
                minimapDisplay.texture = renderTexture;
        }

        private void LateUpdate()
        {
            if (!hasMinimapCamera || !hasPlayer) return;

            // Follow player on X/Y, keep camera Z
            var pos = playerTransform.position;
            minimapCamera.transform.position = new Vector3(pos.x, pos.y, minimapCamera.transform.position.z);

            // Move player icon to match player world position
            if (hasPlayerIcon)
                playerIcon.position = new Vector3(pos.x, pos.y, playerIcon.position.z);
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }
}
