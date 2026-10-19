using UnityEngine;

namespace Game.Gameplay.Controllers.Director
{
    // World-space line from the body toward the stick direction, scaled by hold power -
    // shared by jump-aiming and shooting rather than the segmented sprite arrow in the
    // old-controller reference.
    [RequireComponent(typeof(LineRenderer))]
    public sealed class AimLineView : MonoBehaviour
    {
        [SerializeField] private float maxLength = 4f;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            Hide();
        }

        public void Show(Vector2 origin, Vector2 direction, float power01)
        {
            _line.enabled = true;
            var end = origin + direction * (maxLength * Mathf.Clamp01(power01));
            _line.SetPosition(0, origin);
            _line.SetPosition(1, end);
        }

        public void Hide()
        {
            if (_line != null)
                _line.enabled = false;
        }
    }
}
