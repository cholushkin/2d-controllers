using UnityEngine;


namespace Game.Gameplay.Controllers.Capsule
{
    public sealed class PlasmaBullet : MonoBehaviour, Unity.U2D.Physics.PhysicsCallbacks.IContactCallback
    {
        private Unity.U2D.Physics.PhysicsBody _body;
        private float _lifetime;

        public void Initialize(Unity.U2D.Physics.PhysicsBody body, float lifetime)
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

        public void OnContactBegin2D(Unity.U2D.Physics.PhysicsEvents.ContactBeginEvent beginEvent) => Destroy(gameObject);
        public void OnContactEnd2D(Unity.U2D.Physics.PhysicsEvents.ContactEndEvent endEvent) { }
    }
}
