using Game.Gameplay.Controllers.Core;
using Game.Gameplay.Controllers.Terrain;
using UnityEngine;


namespace Game.Gameplay.Controllers.Capsule
{
    // Kinematic + gravity off while drilling: the capsule "sticks" to the rock and
    // moves purely by velocity along the held direction, carving as it goes.
    public sealed class CapsuleDrillState : IControllerState<CapsuleStateId>
    {
        public CapsuleStateId Id => CapsuleStateId.Drilling;

        private readonly CapsuleConfig.DrillSettings _settings;
        private readonly DestructibleTerrain _terrain;

        private Vector2 _drillDirection = Vector2.down;

        public CapsuleDrillState(CapsuleConfig.DrillSettings settings, DestructibleTerrain terrain)
        {
            _settings = settings;
            _terrain = terrain;
        }

        public void Enter(ControllerContext ctx)
        {
            var move = ctx.CurrentInput.Move;
            if (move.sqrMagnitude > 0.01f)
                _drillDirection = move.normalized;

            var body = ctx.Body;
            body.type = Unity.U2D.Physics.PhysicsBody.BodyType.Kinematic;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
        }

        public void Exit(ControllerContext ctx)
        {
            var body = ctx.Body;
            body.type = Unity.U2D.Physics.PhysicsBody.BodyType.Dynamic;
            body.gravityScale = 1f;
            body.linearVelocity = Vector2.zero;
        }

        public void Tick(ControllerContext ctx, float deltaTime)
        {
            var move = ctx.CurrentInput.Move;
            if (move.sqrMagnitude > 0.01f)
                _drillDirection = move.normalized;
        }

        public void FixedTick(ControllerContext ctx, float fixedDeltaTime)
        {
            var body = ctx.Body;
            var tip = body.position + _drillDirection * _settings.probeDistance;

            if (_terrain != null && _terrain.HasRockAt(tip))
            {
                _terrain.Carve(tip, _settings.carveRadius);
                body.linearVelocity = _drillDirection * _settings.drillSpeed;
            }
            else
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public bool CanTransitionTo(CapsuleStateId next, ControllerContext ctx) => true;
    }
}
