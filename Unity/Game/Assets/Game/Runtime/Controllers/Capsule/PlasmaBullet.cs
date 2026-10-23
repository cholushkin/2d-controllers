using Game.Gameplay.Controllers.Core;
using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // Spawned stationary at the muzzle while the owner charges; ShootingModule either
    // spawns a new one each interval or, when merging, grows an existing one in place
    // via Grow(). Destroys itself on any contact except a Shield shape, which reflects
    // it instead.
    public sealed class PlasmaBullet : MonoBehaviour, Unity.U2D.Physics.PhysicsCallbacks.IContactCallback
    {
        private Unity.U2D.Physics.PhysicsBody _body;
        private Unity.U2D.Physics.PhysicsShape _shape;
        private float _lifetime;
        private float _reflectSpeedBonus;
        private float _radius;
        private float _launchSpeed;

        public Vector2 Position => _body.isValid ? _body.position : (Vector2)transform.position;
        public float Radius => _radius;

        public void Initialize(Unity.U2D.Physics.PhysicsBody body, Unity.U2D.Physics.PhysicsShape shape, float lifetime,
            float reflectSpeedBonus, float radius, float launchSpeed)
        {
            _body = body;
            _shape = shape;
            _lifetime = lifetime;
            _reflectSpeedBonus = reflectSpeedBonus;
            _launchSpeed = launchSpeed;
            _shape.callbackTarget = this;
            SetRadius(radius);
        }

        public void Grow(float radiusDelta, float speedDelta)
        {
            SetRadius(_radius + radiusDelta);
            _launchSpeed += speedDelta;
        }

        private void SetRadius(float radius)
        {
            _radius = radius;
            _shape.circleGeometry = new Unity.U2D.Physics.CircleGeometry { radius = radius };
            transform.localScale = Vector3.one * (radius * 2f);
        }

        public void Launch(Vector2 direction) => _body.linearVelocity = direction.normalized * _launchSpeed;

        public void Abort() => Destroy(gameObject);

        private void Update()
        {
            _lifetime -= Time.unscaledDeltaTime;
            if (_lifetime <= 0f)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_body.isValid)
                _body.Destroy();
        }

        public void OnContactBegin2D(Unity.U2D.Physics.PhysicsEvents.ContactBeginEvent beginEvent)
        {
            var other = beginEvent.shapeA.isValid && beginEvent.shapeA.body == _body ? beginEvent.shapeB : beginEvent.shapeA;

            if (other.isValid && other.contactFilter.categories == PhysicsCategories.Shield)
            {
                Reflect(other);
                return;
            }

            Destroy(gameObject);
        }

        public void OnContactEnd2D(Unity.U2D.Physics.PhysicsEvents.ContactEndEvent endEvent) { }

        private void Reflect(Unity.U2D.Physics.PhysicsShape shieldShape)
        {
            var away = Position - shieldShape.body.position;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector2.up;

            var speed = Mathf.Max(_body.linearVelocity.magnitude, 0.01f) + _reflectSpeedBonus;
            _body.linearVelocity = away.normalized * speed;
        }
    }
}
