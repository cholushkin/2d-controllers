using UnityEngine;


namespace Game.Gameplay.Controllers.Core
{
    // Converts a classic BoxCollider2D-shaped placeholder (e.g. the sandbox's Block3x1
    // ground pieces) into a static low-level body so it collides with the unified world.
    [DisallowMultipleComponent]
    public sealed class LowLevelStaticBaker : MonoBehaviour
    {
        [SerializeField] private Vector2 boxSize = new(3f, 1f);

        private Unity.U2D.Physics.PhysicsBody _body;

        private void Start()
        {
            var world = PhysicsWorldBootstrap.World;
            var bodyDef = new Unity.U2D.Physics.PhysicsBodyDefinition
            {
                type = Unity.U2D.Physics.PhysicsBody.BodyType.Static,
                position = transform.position
            };

            _body = world.CreateBody(bodyDef);
            _body.transformObject = transform;

            var shapeDef = new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter
                {
                    categories = PhysicsCategories.Ground,
                    contacts = PhysicsCategories.Player | PhysicsCategories.Debris | PhysicsCategories.Projectile
                }
            };

            _body.CreateShape(Unity.U2D.Physics.PolygonGeometry.CreateBox(boxSize, 0f, Unity.U2D.Physics.PhysicsTransform.identity), shapeDef);
        }

        private void OnDestroy()
        {
            if (_body.isValid)
                _body.Destroy();
        }
    }
}
