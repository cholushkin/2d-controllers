using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Controllers.Core
{
    // Xbox pad mapping: Move=leftStick, Jump=A/rightClick, Fire=X/leftClick ("Attack"
    // action), Boost=LT, WeaponNext=RB/mouse-wheel-up, WeaponPrevious=LB/wheel-down.
    public sealed class PlayerInputReader : IControllerInput
    {
        private readonly InputAction _move;
        private readonly InputAction _jump;
        private readonly InputAction _fire;
        private readonly InputAction _boost;
        private readonly InputAction _weaponNext;
        private readonly InputAction _weaponPrevious;

        public PlayerInputReader(InputActionAsset actions)
        {
            var player = actions.FindActionMap("Player", throwIfNotFound: true);
            _move = player.FindAction("Move", throwIfNotFound: true);
            _jump = player.FindAction("Jump", throwIfNotFound: true);
            _fire = player.FindAction("Attack", throwIfNotFound: true);
            _boost = player.FindAction("Boost", throwIfNotFound: true);
            _weaponNext = player.FindAction("WeaponNext", throwIfNotFound: true);
            _weaponPrevious = player.FindAction("WeaponPrevious", throwIfNotFound: true);
        }

        public InputSample Sample()
        {
            return new InputSample(
                move: _move.ReadValue<Vector2>(),
                jumpHeld: _jump.IsPressed(), jumpPressed: _jump.WasPressedThisFrame(), jumpReleased: _jump.WasReleasedThisFrame(),
                fireHeld: _fire.IsPressed(), firePressed: _fire.WasPressedThisFrame(), fireReleased: _fire.WasReleasedThisFrame(),
                boostHeld: _boost.IsPressed(),
                weaponNextPressed: _weaponNext.WasPressedThisFrame(),
                weaponPreviousPressed: _weaponPrevious.WasPressedThisFrame());
        }
    }
}
