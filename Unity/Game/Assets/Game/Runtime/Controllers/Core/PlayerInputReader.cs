using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Controllers.Core
{
    // Xbox pad mapping: Move=leftStick, Jump=A, Fire=X ("Attack" action), Drill=Y
    // ("Interact" action), Boost=LT.
    public sealed class PlayerInputReader : IControllerInput
    {
        private readonly InputAction _move;
        private readonly InputAction _jump;
        private readonly InputAction _fire;
        private readonly InputAction _drill;
        private readonly InputAction _boost;

        public PlayerInputReader(InputActionAsset actions)
        {
            var player = actions.FindActionMap("Player", throwIfNotFound: true);
            _move = player.FindAction("Move", throwIfNotFound: true);
            _jump = player.FindAction("Jump", throwIfNotFound: true);
            _fire = player.FindAction("Attack", throwIfNotFound: true);
            _drill = player.FindAction("Interact", throwIfNotFound: true);
            _boost = player.FindAction("Boost", throwIfNotFound: true);
        }

        public InputSample Sample()
        {
            return new InputSample(
                move: _move.ReadValue<Vector2>(),
                jumpHeld: _jump.IsPressed(), jumpPressed: _jump.WasPressedThisFrame(), jumpReleased: _jump.WasReleasedThisFrame(),
                fireHeld: _fire.IsPressed(), firePressed: _fire.WasPressedThisFrame(), fireReleased: _fire.WasReleasedThisFrame(),
                drillHeld: _drill.IsPressed(), drillPressed: _drill.WasPressedThisFrame(), drillReleased: _drill.WasReleasedThisFrame(),
                boostHeld: _boost.IsPressed());
        }
    }
}
