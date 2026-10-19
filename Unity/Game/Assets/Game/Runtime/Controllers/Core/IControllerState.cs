using System;

namespace Game.Gameplay.Controllers.Core
{
    public interface IControllerState<TId> where TId : struct, Enum
    {
        TId Id { get; }

        void Enter(ControllerContext context);
        void Exit(ControllerContext context);
        void Tick(ControllerContext context, float deltaTime);
        void FixedTick(ControllerContext context, float fixedDeltaTime);
        bool CanTransitionTo(TId next, ControllerContext context);
    }
}
