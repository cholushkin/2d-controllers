using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Drone
{
    // Also used as the drone's docked/idle representation before its first launch.
    public sealed class DroneDepletedState : IControllerState<DroneStateId>
    {
        public DroneStateId Id => DroneStateId.Depleted;

        public void Enter(ControllerContext ctx)
        {
            var body = ctx.Body;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        public void Exit(ControllerContext ctx) { }

        public void Tick(ControllerContext ctx, float deltaTime) { }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime) { }

        public bool CanTransitionTo(DroneStateId next, ControllerContext ctx) => true;
    }
}
