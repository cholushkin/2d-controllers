using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // Visualizes every live controller value: probes, aim/fire/drill/shield, and the
    // freeze ramp. Add to the same GameObject as CapsuleController.
    [RequireComponent(typeof(CapsuleController))]
    public sealed class CapsuleDebugGizmos : MonoBehaviour
    {
        [SerializeField] private bool drawVelocity = true;
        [SerializeField] private bool drawGroundProbe = true;
        [SerializeField] private bool drawAim = true;
        [SerializeField] private bool drawFire = true;
        [SerializeField] private bool drawDrill = true;
        [SerializeField] private bool drawShield = true;
        [SerializeField] private bool drawFreeze = true;

        private CapsuleController _controller;

        private void Awake() => _controller = GetComponent<CapsuleController>();

        private void OnDrawGizmos()
        {
            if (_controller == null)
                _controller = GetComponent<CapsuleController>();

            if (_controller == null || !Application.isPlaying)
                return;

            var info = _controller.DebugView;
            if (!info.IsValid)
                return;

            var position = info.Position;

            if (drawVelocity)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(position, position + info.Velocity * 0.2f);
            }

            if (drawGroundProbe)
            {
                var (a, b, c) = info.GroundProbePoints;
                Gizmos.color = info.IsGrounded ? Color.green : new Color(1f, 0.5f, 0f);
                Gizmos.DrawSphere(a, 0.05f);
                Gizmos.DrawSphere(b, 0.05f);
                Gizmos.DrawSphere(c, 0.05f);

                Gizmos.color = info.IsSubmerged ? new Color(0.2f, 0.6f, 1f) : new Color(0.2f, 0.6f, 1f, 0.25f);
                Gizmos.DrawWireSphere(position, info.BodyRadius * 1.1f);
            }

            if (drawAim && info.IsAiming)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(position, position + info.AimDirection * (info.AimPower * info.MaxAimImpulse * 0.1f));
            }

            if (drawFire && info.IsCharging)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(info.ChargePosition, info.ChargeRadius);
            }

            if (drawDrill)
            {
                if (info.IsDrilling)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(position, info.DrillProbeTip);
                    Gizmos.DrawWireSphere(info.DrillProbeTip, info.DrillCarveRadius);
                }
                else if (info.TryGetRockAhead(out var hit))
                {
                    Gizmos.color = new Color(1f, 0f, 1f, 0.4f);
                    Gizmos.DrawLine(position, hit);
                    Gizmos.DrawWireSphere(hit, 0.1f);
                }
            }

            if (drawShield && info.ShieldActive)
            {
                Gizmos.color = new Color(0.5f, 0.85f, 1f, 0.8f);
                Gizmos.DrawWireSphere(info.ShieldPosition, info.ShieldRadius);
            }

            if (drawFreeze && info.FreezeProgress > 0.0001f)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, info.FreezeProgress);
                Gizmos.DrawWireSphere(position, info.BodyRadius + 0.15f + info.FreezeProgress * 0.3f);
            }
        }
    }
}
