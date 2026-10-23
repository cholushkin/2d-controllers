using Game.Gameplay.Controllers.Core;
using UnityEngine;


namespace Game.Gameplay.Controllers.Capsule
{
    public sealed class PlasmaBulletSpawner
    {
        public void Spawn(Vector2 position, Vector2 direction, CapsuleConfig.ShootSettings settings)
        {
            var world = PhysicsWorldBootstrap.World;
            var spawnPosition = position + direction * (settings.bulletRadius + 0.1f);

            var bodyDef = new Unity.U2D.Physics.PhysicsBodyDefinition
            {
                type = Unity.U2D.Physics.PhysicsBody.BodyType.Dynamic,
                position = spawnPosition,
                gravityScale = 0f,
                fastCollisionsAllowed = true,
                linearVelocity = direction * settings.bulletSpeed
            };
            var body = world.CreateBody(bodyDef);

            var shapeDef = new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter
                {
                    categories = PhysicsCategories.Projectile,
                    contacts = PhysicsCategories.Ground | PhysicsCategories.Terrain
                },
                contactEvents = true
            };
            body.CreateShape(new Unity.U2D.Physics.CircleGeometry { radius = settings.bulletRadius }, shapeDef);

            var go = new GameObject("PlasmaBullet");
            go.transform.position = spawnPosition;
            body.transformObject = go.transform;
            body.transformWriteMode = Unity.U2D.Physics.PhysicsBody.TransformWriteMode.Interpolate;

            var visual = go.AddComponent<SpriteRenderer>();
            visual.color = new Color(0.4f, 1f, 0.9f);

            var bullet = go.AddComponent<PlasmaBullet>();
            bullet.Initialize(body, settings.bulletLifetime);
        }
    }
}
