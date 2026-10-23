using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    public sealed class PlasmaBulletSpawner
    {
        public PlasmaBullet Spawn(Vector2 position, float radius, float launchSpeed, float reflectSpeedBonus, float lifetime)
        {
            var world = PhysicsWorldBootstrap.World;

            var bodyDef = new Unity.U2D.Physics.PhysicsBodyDefinition
            {
                type = Unity.U2D.Physics.PhysicsBody.BodyType.Dynamic,
                position = position,
                gravityScale = 0f,
                fastCollisionsAllowed = true
            };
            var body = world.CreateBody(bodyDef);

            var shapeDef = new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter
                {
                    categories = PhysicsCategories.Projectile,
                    contacts = PhysicsCategories.Ground | PhysicsCategories.Terrain | PhysicsCategories.Shield
                },
                contactEvents = true
            };
            var shape = body.CreateShape(new Unity.U2D.Physics.CircleGeometry { radius = radius }, shapeDef);

            var go = new GameObject("PlasmaBullet");
            go.transform.position = position;
            body.transformObject = go.transform;
            body.transformWriteMode = Unity.U2D.Physics.PhysicsBody.TransformWriteMode.Interpolate;

            var visual = go.AddComponent<SpriteRenderer>();
            visual.sprite = ProceduralSprites.Circle;
            visual.color = new Color(0.4f, 1f, 0.9f);

            var bullet = go.AddComponent<PlasmaBullet>();
            bullet.Initialize(body, shape, lifetime, reflectSpeedBonus, radius, launchSpeed);
            return bullet;
        }
    }
}
