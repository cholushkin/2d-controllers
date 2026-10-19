using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

namespace Game.Gameplay.Controllers.Capsule
{
    public sealed class PlasmaBullet : MonoBehaviour, PhysicsCallbacks.IContactCallback
    {
        private PhysicsBody _body;
        private float _lifetime;

        public void Initialize(PhysicsBody body, float lifetime)
        {
            _body = body;
            _lifetime = lifetime;

            using var shapes = body.GetShapes();
            foreach (var shape in shapes)
                shape.callbackTarget = this;
        }

        private void Update()
        {
            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_body.isValid)
                _body.Destroy();
        }

        public void OnContactBegin2D(PhysicsEvents.ContactBeginEvent beginEvent) => Destroy(gameObject);
        public void OnContactEnd2D(PhysicsEvents.ContactEndEvent endEvent) { }
    }
}
