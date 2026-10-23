using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // Short X tap -> a timed bubble that chases the capsule and reflects (rather than
    // destroys) any plasma bullet that touches it. Not a full FSM state: it runs
    // alongside Ground/Underwater/Drilling without blocking them.
    public sealed class ShieldModule
    {
        private readonly CapsuleConfig.ShieldSettings _settings;
        private ShieldBubble _active;

        public bool IsActive => _active != null;
        public Vector2 Position => _active != null ? _active.Position : default;
        public float Radius => _settings.radius;

        public ShieldModule(CapsuleConfig.ShieldSettings settings) => _settings = settings;

        public void Activate(Transform owner)
        {
            if (_active != null)
                return;

            var world = PhysicsWorldBootstrap.World;
            var bodyDef = new Unity.U2D.Physics.PhysicsBodyDefinition
            {
                type = Unity.U2D.Physics.PhysicsBody.BodyType.Kinematic,
                position = owner.position,
                gravityScale = 0f
            };
            var body = world.CreateBody(bodyDef);

            var shapeDef = new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter
                {
                    categories = PhysicsCategories.Shield,
                    contacts = PhysicsCategories.Projectile
                }
            };
            body.CreateShape(new Unity.U2D.Physics.CircleGeometry { radius = _settings.radius }, shapeDef);

            var go = new GameObject("ShieldBubble");
            go.transform.position = owner.position;
            go.transform.localScale = Vector3.one * (_settings.radius * 2f);
            body.transformObject = go.transform;
            body.transformWriteMode = Unity.U2D.Physics.PhysicsBody.TransformWriteMode.Interpolate;

            var visual = go.AddComponent<SpriteRenderer>();
            visual.sprite = ProceduralSprites.Circle;
            visual.color = new Color(0.5f, 0.85f, 1f, 0.35f);

            var bubble = go.AddComponent<ShieldBubble>();
            bubble.Initialize(body, owner, _settings.duration, this);
            _active = bubble;
        }

        internal void NotifyExpired(ShieldBubble bubble)
        {
            if (_active == bubble)
                _active = null;
        }
    }
}
