using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Adjusts RectTransform anchors to match simulated or hardware safeArea in mobile portrait orientations.
    /// Operates in both Edit Mode (via Device Simulator) and Play Mode.
    /// Only recalculates when screen orientation or dimensions change.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class LobbySafeAreaView : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            EnsureRectTransform();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            EnsureRectTransform();
            ApplySafeArea();
        }

        private void Update()
        {
            Rect safeArea = GetCurrentSafeArea();
            Vector2Int screenSize = GetCurrentScreenSize();

            if (HasSafeAreaChanged(safeArea, screenSize))
            {
                ApplySafeAreaInternal(safeArea, screenSize);
            }
        }

        public void ApplySafeArea()
        {
            EnsureRectTransform();
            Rect safeArea = GetCurrentSafeArea();
            Vector2Int screenSize = GetCurrentScreenSize();
            ApplySafeAreaInternal(safeArea, screenSize);
        }

        private void ApplySafeAreaInternal(Rect safeArea, Vector2Int screenSize)
        {
            if (_rectTransform == null) return;
            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            if (_rectTransform.anchorMin != anchorMin)
            {
                _rectTransform.anchorMin = anchorMin;
            }

            if (_rectTransform.anchorMax != anchorMax)
            {
                _rectTransform.anchorMax = anchorMax;
            }

            if (_rectTransform.offsetMin != Vector2.zero)
            {
                _rectTransform.offsetMin = Vector2.zero;
            }

            if (_rectTransform.offsetMax != Vector2.zero)
            {
                _rectTransform.offsetMax = Vector2.zero;
            }
        }

        private Rect GetCurrentSafeArea()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return UnityEngine.Device.Screen.safeArea;
            }
#endif
            return Screen.safeArea;
        }

        private Vector2Int GetCurrentScreenSize()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return new Vector2Int(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
            }
#endif
            return new Vector2Int(Screen.width, Screen.height);
        }

        private bool HasSafeAreaChanged(Rect safeArea, Vector2Int screenSize)
        {
            return safeArea != _lastSafeArea || screenSize != _lastScreenSize;
        }

        private void EnsureRectTransform()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }
    }
}
