using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // Xbox-pad-referenced sample: Move is the left stick (movement AND aim/jump/drill
    // direction - there is no separate aim stick). Jump=A, Fire=X, Drill=Y, Boost=LT.
    public readonly struct InputSample
    {
        public readonly Vector2 Move;

        public readonly bool JumpHeld;
        public readonly bool JumpPressed;
        public readonly bool JumpReleased;

        public readonly bool FireHeld;
        public readonly bool FirePressed;
        public readonly bool FireReleased;

        public readonly bool DrillHeld;
        public readonly bool DrillPressed;
        public readonly bool DrillReleased;

        public readonly bool BoostHeld;

        public InputSample(
            Vector2 move,
            bool jumpHeld, bool jumpPressed, bool jumpReleased,
            bool fireHeld, bool firePressed, bool fireReleased,
            bool drillHeld, bool drillPressed, bool drillReleased,
            bool boostHeld)
        {
            Move = move;
            JumpHeld = jumpHeld;
            JumpPressed = jumpPressed;
            JumpReleased = jumpReleased;
            FireHeld = fireHeld;
            FirePressed = firePressed;
            FireReleased = fireReleased;
            DrillHeld = drillHeld;
            DrillPressed = drillPressed;
            DrillReleased = drillReleased;
            BoostHeld = boostHeld;
        }
    }
}
