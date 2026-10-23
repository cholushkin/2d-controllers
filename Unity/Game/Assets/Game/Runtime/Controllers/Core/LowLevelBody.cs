using UnityEngine;


namespace Game.Gameplay.Controllers.Core
{
    public static class LowLevelBody
    {
        public static Unity.U2D.Physics.PhysicsBody CreateCircleBody(Unity.U2D.Physics.PhysicsWorld world, Transform transform, float radius,
            Unity.U2D.Physics.PhysicsBody.BodyType bodyType, Unity.U2D.Physics.PhysicsMask category, Unity.U2D.Physics.PhysicsMask contacts,
            float gravityScale = 1f, bool contactEvents = false)
        {
            var bodyDef = new Unity.U2D.Physics.PhysicsBodyDefinition
            {
                type = bodyType,
                position = transform.position,
                gravityScale = gravityScale,
                fastCollisionsAllowed = true
            };

            var body = world.CreateBody(bodyDef);
            body.transformObject = transform;
            body.transformWriteMode = Unity.U2D.Physics.PhysicsBody.TransformWriteMode.Interpolate;

            var shapeDef = new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter { categories = category, contacts = contacts },
                contactEvents = contactEvents
            };

            body.CreateShape(new Unity.U2D.Physics.CircleGeometry { radius = radius }, shapeDef);
            return body;
        }
    }
}
