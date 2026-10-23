using System;
using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // Shared by every freeze-capable gesture (aimed jump, charged shot, ...). Defaults
    // give a ~0.6s ease so freshly-added fields are never silently flat/instant.
    [Serializable]
    public sealed class FreezeSettings
    {
        public AnimationCurve enterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float enterDuration = 0.6f;

        public AnimationCurve exitCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float exitDuration = 0.25f;
    }
}
