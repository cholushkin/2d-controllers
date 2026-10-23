using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // Xbox-pad-referenced sample: Move is the left stick (movement AND aim/drill
    // direction - there is no separate aim stick). Jump=A, Fire=X ("use weapon"),
    // Boost=LT; WeaponNext/Previous=RB/LB (mouse wheel / keyboard equivalents live in
    // the action bindings, not here).
    public readonly struct InputSample
    {
        public readonly Vector2 Move;

        public readonly bool JumpHeld;
        public readonly bool JumpPressed;
        public readonly bool JumpReleased;

        public readonly bool FireHeld;
        public readonly bool FirePressed;
        public readonly bool FireReleased;

        public readonly bool BoostHeld;

        public readonly bool WeaponNextPressed;
        public readonly bool WeaponPreviousPressed;

        public InputSample(
            Vector2 move,
            bool jumpHeld, bool jumpPressed, bool jumpReleased,
            bool fireHeld, bool firePressed, bool fireReleased,
            bool boostHeld,
            bool weaponNextPressed, bool weaponPreviousPressed)
        {
            Move = move;
            JumpHeld = jumpHeld;
            JumpPressed = jumpPressed;
            JumpReleased = jumpReleased;
            FireHeld = fireHeld;
            FirePressed = firePressed;
            FireReleased = fireReleased;
            BoostHeld = boostHeld;
            WeaponNextPressed = weaponNextPressed;
            WeaponPreviousPressed = weaponPreviousPressed;
        }
    }
}
