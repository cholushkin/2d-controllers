using Game.Gameplay.Controllers.Core;

namespace Game.Gameplay.Controllers.Capsule
{
    // Distinguishes a tap (light hop, no battery cost) from a hold (aimed power-jump)
    // on the same Jump button. Lives outside the states themselves because the
    // decision of *which* top-level state to enter belongs to the controller.
    public sealed class JumpHoldGesture
    {
        private readonly float _holdThreshold;
        private float _heldTime;
        private bool _consumedAsHold;

        public JumpHoldGesture(float holdThreshold)
        {
            _holdThreshold = holdThreshold;
        }

        public bool LightHopRequested { get; private set; }
        public bool AimedJumpRequested { get; private set; }

        public void Tick(InputSample input, float deltaTime)
        {
            LightHopRequested = false;
            AimedJumpRequested = false;

            if (input.JumpHeld)
            {
                _heldTime += deltaTime;
                if (!_consumedAsHold && _heldTime >= _holdThreshold)
                {
                    _consumedAsHold = true;
                    AimedJumpRequested = true;
                }
            }

            if (input.JumpReleased)
            {
                if (!_consumedAsHold)
                    LightHopRequested = true;

                _heldTime = 0f;
                _consumedAsHold = false;
            }
        }
    }
}
