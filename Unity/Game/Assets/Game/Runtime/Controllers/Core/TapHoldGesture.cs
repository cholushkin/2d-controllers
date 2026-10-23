namespace Game.Gameplay.Controllers.Core
{
    // Distinguishes a tap from a hold on one button, on unscaled time so it stays
    // responsive while the controller is frozen. One instance per gesture button
    // (A -> jump, X -> fire/shield); the controller owns the decision of which
    // top-level state a tap vs. a hold-begin leads to.
    public sealed class TapHoldGesture
    {
        private readonly float _holdThreshold;
        private float _heldTime;
        private bool _consumedAsHold;

        public TapHoldGesture(float holdThreshold)
        {
            _holdThreshold = holdThreshold;
        }

        public bool TapRequested { get; private set; }
        public bool HoldBeginRequested { get; private set; }

        public void Tick(bool held, bool released, float unscaledDeltaTime)
        {
            TapRequested = false;
            HoldBeginRequested = false;

            if (held)
            {
                _heldTime += unscaledDeltaTime;
                if (!_consumedAsHold && _heldTime >= _holdThreshold)
                {
                    _consumedAsHold = true;
                    HoldBeginRequested = true;
                }
            }

            if (released)
            {
                if (!_consumedAsHold)
                    TapRequested = true;

                _heldTime = 0f;
                _consumedAsHold = false;
            }
        }
    }
}
