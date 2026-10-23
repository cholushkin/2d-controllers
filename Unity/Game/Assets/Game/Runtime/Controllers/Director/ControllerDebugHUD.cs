using Game.Gameplay.Controllers.Capsule;
using Game.Gameplay.Controllers.Drone;
using Game.Gameplay.Controllers.Pilot;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Controllers.Director
{
    // IMGUI readout of live controller state, independent of the Scene/Game view
    // Gizmos toggle (which hides OnDrawGizmos output unless explicitly enabled).
    public sealed class ControllerDebugHUD : MonoBehaviour
    {
        [SerializeField] private CapsuleController capsule;
        [SerializeField] private PilotController pilot;
        [SerializeField] private DroneController drone;
        [SerializeField] private bool visible = true;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 440, 260), GUI.skin.box);
            GUILayout.Label("Controller Debug (F1 to hide)");

            if (capsule != null)
            {
                var d = capsule.DebugView;
                GUILayout.Label($"Capsule: {capsule.CurrentState}   weapon: {capsule.CurrentWeapon}");
                GUILayout.Label($"grounded:{d.IsGrounded} submerged:{d.IsSubmerged} freeze:{d.FreezeProgress:0.00}");
                GUILayout.Label($"aiming:{d.IsAiming} aimDir:{d.AimDirection} power:{d.AimPower:0.00}");
                GUILayout.Label($"shooting:{d.IsShooting} pendingBullets:{capsule.PendingBulletCount}");
                GUILayout.Label($"drilling:{d.IsDrilling} drillDir:{d.DrillDirection}");
                GUILayout.Label($"shield active:{d.ShieldActive}");
                GUILayout.Label($"batteries:{capsule.Energy.Batteries}/{capsule.Energy.MaxBatteries} energy:{capsule.Energy.Energy:0.00}");
            }

            if (pilot != null)
                GUILayout.Label($"Pilot: {pilot.CurrentState}");

            if (drone != null)
                GUILayout.Label($"Drone: {drone.CurrentState}");

            GUILayout.EndArea();
        }
    }
}
