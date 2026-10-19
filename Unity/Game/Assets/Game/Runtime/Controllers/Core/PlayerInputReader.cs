using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Controllers.Core
{
    // Drill reuses the "Interact" action: it already carries a Hold interaction in
    // InputSystem_Actions.inputactions, matching the hold-to-drill gesture.
    public sealed class PlayerInputReader : IControllerInput
    {
        private readonly InputAction _move;
        private readonly InputAction _aim;
        private readonly InputAction _jump;
        private readonly InputAction _attack;
        private readonly InputAction _drill;

        public PlayerInputReader(InputActionAsset actions)
        {
            var player = actions.FindActionMap("Player", throwIfNotFound: true);
            _move = player.FindAction("Move", throwIfNotFound: true);
            _aim = player.FindAction("Look", throwIfNotFound: true);
            _jump = player.FindAction("Jump", throwIfNotFound: true);
            _attack = player.FindAction("Attack", throwIfNotFound: true);
            _drill = player.FindAction("Interact", throwIfNotFound: true);
        }

        public InputSample Sample()
        {
            return new InputSample(
                move: _move.ReadValue<Vector2>(),
                aim: _aim.ReadValue<Vector2>(),
                jumpHeld: _jump.IsPressed(), jumpPressed: _jump.WasPressedThisFrame(), jumpReleased: _jump.WasReleasedThisFrame(),
                attackHeld: _attack.IsPressed(), attackPressed: _attack.WasPressedThisFrame(), attackReleased: _attack.WasReleasedThisFrame(),
                drillHeld: _drill.IsPressed(), drillPressed: _drill.WasPressedThisFrame(), drillReleased: _drill.WasReleasedThisFrame());
        }
    }
}
