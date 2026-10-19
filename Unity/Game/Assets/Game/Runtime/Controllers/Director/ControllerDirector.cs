using Game.Gameplay.Controllers.Capsule;
using Game.Gameplay.Controllers.Drone;
using Game.Gameplay.Controllers.Pilot;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Controllers.Director
{
    public enum PossessedAgent
    {
        Capsule,
        Pilot
    }

    // Minimal possession/launch orchestration for the sandbox: toggles pilot<->capsule
    // on the "Previous" action and launches/relaunches the drone on "Next". Intended as
    // a starting point to refine once real exit/pickup prompts and camera framing exist.
    [DisallowMultipleComponent]
    public sealed class ControllerDirector : MonoBehaviour
    {
        [SerializeField] private CapsuleController capsule;
        [SerializeField] private PilotController pilot;
        [SerializeField] private DroneController drone;
        [SerializeField] private InputActionAsset inputActions;

        private InputAction _exitCapsuleAction;
        private InputAction _toggleDroneAction;
        private PossessedAgent _possessed = PossessedAgent.Capsule;

        private void Awake()
        {
            var map = inputActions.FindActionMap("Player", throwIfNotFound: true);
            _exitCapsuleAction = map.FindAction("Previous", throwIfNotFound: true);
            _toggleDroneAction = map.FindAction("Next", throwIfNotFound: true);

            capsule.SetInputEnabled(true);
            pilot.SetActive(false);
        }

        private void Update()
        {
            if (_exitCapsuleAction.WasPressedThisFrame())
                TogglePossession();

            if (_toggleDroneAction.WasPressedThisFrame())
                LaunchOrRelaunchDrone();
        }

        private void TogglePossession()
        {
            if (_possessed == PossessedAgent.Capsule)
            {
                pilot.transform.position = capsule.transform.position + Vector3.up * 0.8f;
                capsule.SetInputEnabled(false);
                pilot.SetActive(true);
                _possessed = PossessedAgent.Pilot;
            }
            else
            {
                capsule.SetInputEnabled(true);
                pilot.SetActive(false);
                _possessed = PossessedAgent.Capsule;
            }
        }

        private void LaunchOrRelaunchDrone()
        {
            if (drone.CurrentState != DroneStateId.Depleted)
                return;

            var origin = _possessed == PossessedAgent.Capsule ? capsule.transform.position : pilot.transform.position;
            drone.transform.position = origin + Vector3.up;
            drone.Launch();
        }
    }
}
