using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

namespace Game.Gameplay.Controllers.Core
{
    public static class LowLevelBody
    {
        public static PhysicsBody CreateCircleBody(PhysicsWorld world, Transform transform, float radius,
            PhysicsBody.BodyType bodyType, PhysicsMask category, PhysicsMask contacts,
            float gravityScale = 1f, bool contactEvents = false)
        {
            var bodyDef = new PhysicsBodyDefinition
            {
                type = bodyType,
                position = transform.position,
                gravityScale = gravityScale,
                fastCollisionsAllowed = true
            };

            var body = world.CreateBody(bodyDef);
            body.transformObject = transform;
            body.transformWriteMode = PhysicsBody.TransformWriteMode.Interpolate;

            var shapeDef = new PhysicsShapeDefinition
            {
                contactFilter = new PhysicsShape.ContactFilter { categories = category, contacts = contacts },
                contactEvents = contactEvents
            };

            body.CreateShape(new CircleGeometry { radius = radius }, shapeDef);
            return body;
        }
    }
}
