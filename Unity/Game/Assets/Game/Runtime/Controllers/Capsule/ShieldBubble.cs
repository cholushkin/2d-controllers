using UnityEngine;

namespace Game.Gameplay.Controllers.Capsule
{
    // Chases its owner via velocity-matching each FixedUpdate rather than writing
    // body.position directly - the low-level body's position setter is unconfirmed
    // against this engine build, while linearVelocity is already relied on elsewhere.
    public sealed class ShieldBubble : MonoBehaviour
    {
        private Unity.U2D.Physics.PhysicsBody _body;
        private Transform _owner;
        private float _lifetime;
        private ShieldModule _module;

        public Vector2 Position => _body.isValid ? _body.position : (Vector2)transform.position;

        public void Initialize(Unity.U2D.Physics.PhysicsBody body, Transform owner, float lifetime, ShieldModule module)
        {
            _body = body;
            _owner = owner;
            _lifetime = lifetime;
            _module = module;
        }

        private void Update()
        {
            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
                Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (!_body.isValid || _owner == null)
                return;

            var delta = (Vector2)_owner.position - _body.position;
            _body.linearVelocity = delta / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        }

        private void OnDestroy()
        {
            _module?.NotifyExpired(this);

            if (_body.isValid)
                _body.Destroy();
        }
    }
}
