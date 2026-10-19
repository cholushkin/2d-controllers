using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    public readonly struct InputSample
    {
        public readonly Vector2 Move;
        public readonly Vector2 Aim;

        public readonly bool JumpHeld;
        public readonly bool JumpPressed;
        public readonly bool JumpReleased;

        public readonly bool AttackHeld;
        public readonly bool AttackPressed;
        public readonly bool AttackReleased;

        public readonly bool DrillHeld;
        public readonly bool DrillPressed;
        public readonly bool DrillReleased;

        public InputSample(
            Vector2 move, Vector2 aim,
            bool jumpHeld, bool jumpPressed, bool jumpReleased,
            bool attackHeld, bool attackPressed, bool attackReleased,
            bool drillHeld, bool drillPressed, bool drillReleased)
        {
            Move = move;
            Aim = aim;
            JumpHeld = jumpHeld;
            JumpPressed = jumpPressed;
            JumpReleased = jumpReleased;
            AttackHeld = attackHeld;
            AttackPressed = attackPressed;
            AttackReleased = attackReleased;
            DrillHeld = drillHeld;
            DrillPressed = drillPressed;
            DrillReleased = drillReleased;
        }
    }
}
