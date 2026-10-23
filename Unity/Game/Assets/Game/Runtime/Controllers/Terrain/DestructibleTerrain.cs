using Game.Gameplay.Controllers.Core;
using Unity.Collections;
using UnityEngine;

using Random = Unity.Mathematics.Random;

namespace Game.Gameplay.Controllers.Terrain
{
    // Adapted from Assets/Game/RuntimeRnD/SpriteDestructionExample/DestructionController.cs:
    // same PhysicsDestructor.Fragment carving flow, but debris excludes the Player
    // category and is reaped on a timer instead of on ground contact (see
    // DebrisReaper). The initial rock shape is authored in the editor via
    // PhysicsPose + PhysicsArea* (any shape type) rather than a sprite outline -
    // Awake consumes it once into its own low-level body, then discards it.
    [DisallowMultipleComponent]
    public sealed class DestructibleTerrain : MonoBehaviour
    {
        [SerializeField] private TerrainConfig config;
        [SerializeField] private Vector2 fallbackBoxSize = new(6f, 3f);
        [SerializeField] private DebrisReaper debrisReaper;
        [SerializeField] private FragmentMeshRenderer fragmentRenderer;

        private Unity.U2D.Physics.PhysicsWorld _world;
        private Unity.U2D.Physics.PhysicsShape.ContactFilter _rockContactFilter;
        private Unity.U2D.Physics.PhysicsShape.SurfaceMaterial _rockSurfaceMaterial;
        private NativeArray<Unity.U2D.Physics.PolygonGeometry> _carveMask;
        private Random _rng = new(7919);

        private void Awake()
        {
            _world = PhysicsWorldBootstrap.World;

            _rockContactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter
            {
                categories = PhysicsCategories.Terrain,
                contacts = PhysicsCategories.Player | PhysicsCategories.Ground | PhysicsCategories.Terrain |
                           PhysicsCategories.Debris | PhysicsCategories.Projectile
            };
            _rockSurfaceMaterial = new Unity.U2D.Physics.PhysicsShape.SurfaceMaterial
            {
                friction = config.fragmentFriction,
                bounciness = config.fragmentBounciness
            };

            BuildCarveMask();
            SpawnRockFromAuthoredArea();
        }

        private void OnDestroy()
        {
            if (_carveMask.IsCreated)
                _carveMask.Dispose();
        }

        public bool HasRockAt(Vector2 point)
        {
            using var hits = _world.OverlapPoint(point, new Unity.U2D.Physics.PhysicsQuery.QueryFilter
            {
                categories = PhysicsCategories.Terrain,
                hitCategories = PhysicsCategories.Terrain
            });
            return hits.Length > 0;
        }

        public void Carve(Vector2 point, float radius)
        {
            using var hits = _world.OverlapPoint(point, new Unity.U2D.Physics.PhysicsQuery.QueryFilter
            {
                categories = PhysicsCategories.Terrain,
                hitCategories = PhysicsCategories.Terrain
            });
            if (hits.Length == 0)
                return;

            var body = hits[0].shape.body;
            using var shapes = body.GetShapes();

            var targetPolygons = new NativeList<Unity.U2D.Physics.PolygonGeometry>(shapes.Length, Allocator.Temp);
            foreach (var shape in shapes)
            {
                if (shape.shapeType == Unity.U2D.Physics.PhysicsShape.ShapeType.Polygon)
                    targetPolygons.Add(shape.polygonGeometry);
            }

            var target = new Unity.U2D.Physics.PhysicsDestructor.FragmentGeometry(body.transform, targetPolygons.AsReadOnly());
            targetPolygons.Dispose();

            var points = new NativeArray<Vector2>(config.fragmentCount, Allocator.Temp);
            points[0] = point;
            for (var i = 1; i < config.fragmentCount; ++i)
            {
                var angle = _rng.NextFloat(0f, Mathf.PI * 2f);
                var r = _rng.NextFloat(0.05f, radius);
                points[i] = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }

            var mask = new Unity.U2D.Physics.PhysicsDestructor.FragmentGeometry(point, _carveMask);
            using var results = Unity.U2D.Physics.PhysicsDestructor.Fragment(target, mask, points, Allocator.Temp);
            points.Dispose();

            fragmentRenderer?.Remove(body);
            body.Destroy();

            var fragmentTransform = results.transform;

            if (results.unbrokenGeometry.Length > 0)
            {
                var staticDef = new Unity.U2D.Physics.PhysicsBodyDefinition { position = fragmentTransform.position, rotation = fragmentTransform.rotation };
                var remainder = _world.CreateBody(staticDef);
                using var batch = remainder.CreateShapeBatch(results.unbrokenGeometry, new Unity.U2D.Physics.PhysicsShapeDefinition
                {
                    contactFilter = _rockContactFilter,
                    surfaceMaterial = _rockSurfaceMaterial
                });

                fragmentRenderer?.Add(remainder, results.unbrokenGeometry);
            }

            if (results.brokenGeometry.Length > 0)
                SpawnDebris(fragmentTransform, results.brokenGeometry);
        }

        private void SpawnDebris(Unity.U2D.Physics.PhysicsTransform fragmentTransform, NativeArray<Unity.U2D.Physics.PolygonGeometry> brokenGeometry)
        {
            var debrisDef = new Unity.U2D.Physics.PhysicsBodyDefinition
            {
                type = Unity.U2D.Physics.PhysicsBody.BodyType.Dynamic,
                fastCollisionsAllowed = true,
                position = fragmentTransform.position,
                rotation = fragmentTransform.rotation,
                gravityScale = 1f
            };
            using var bodies = _world.CreateBodyBatch(debrisDef, brokenGeometry.Length);

            var debrisShapeDef = new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter
                {
                    categories = PhysicsCategories.Debris,
                    // Deliberately excludes PhysicsCategories.Player.
                    contacts = PhysicsCategories.Ground | PhysicsCategories.Terrain | PhysicsCategories.Debris
                },
                surfaceMaterial = _rockSurfaceMaterial
            };

            for (var i = 0; i < brokenGeometry.Length; ++i)
            {
                var piece = bodies[i];
                var geometry = brokenGeometry[i];
                piece.CreateShape(geometry, debrisShapeDef);

                var one = new NativeArray<Unity.U2D.Physics.PolygonGeometry>(1, Allocator.Temp) { [0] = geometry };
                fragmentRenderer?.Add(piece, one);
                one.Dispose();

                debrisReaper?.Register(piece, config.debrisLifetime);
            }
        }

        private void BuildCarveMask()
        {
            var vertices = new NativeList<Vector2>(36, Allocator.Temp);
            const float tau = Mathf.PI * 2f;
            const float stride = tau / 24f;
            for (var angle = 0f; angle < tau; angle += stride)
                vertices.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * config.fragmentRadius);

            _carveMask = Unity.U2D.Physics.PolygonGeometry.CreatePolygons(vertices.AsArray(), Unity.U2D.Physics.PhysicsTransform.identity, Vector2.one, Allocator.Persistent);
            vertices.Dispose();
        }

        // Consumes whatever PhysicsPose + PhysicsArea* was authored in the editor
        // (any shape type that resolves to polygon shapes, e.g. PhysicsAreaPolygon or
        // PhysicsAreaContour) into this component's own low-level rock body, then
        // discards the authoring components - from here on the rock only exists as
        // the destructible body Carve()/HasRockAt() already know how to operate on.
        private void SpawnRockFromAuthoredArea()
        {
            var pose = GetComponent<Unity.U2D.Physics.PhysicsPose>();
            var authoredPolygons = new NativeList<Unity.U2D.Physics.PolygonGeometry>(Allocator.Temp);

            if (pose != null && pose.body.isValid)
            {
                using var shapes = pose.body.GetShapes();
                foreach (var shape in shapes)
                {
                    if (shape.shapeType == Unity.U2D.Physics.PhysicsShape.ShapeType.Polygon)
                        authoredPolygons.Add(shape.polygonGeometry);
                }
            }

            var body = _world.CreateBody(new Unity.U2D.Physics.PhysicsBodyDefinition { position = transform.position });
            var shapeDef = new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = _rockContactFilter,
                surfaceMaterial = _rockSurfaceMaterial
            };

            if (authoredPolygons.Length > 0)
            {
                using var batch = body.CreateShapeBatch(authoredPolygons.AsArray(), shapeDef);
                fragmentRenderer?.Add(body, authoredPolygons.AsArray());
            }
            else
            {
                // No authored area (or it produced no polygon shapes) - fall back to
                // a plain slab so the rock needs no editor-authoring step at all.
                var box = Unity.U2D.Physics.PolygonGeometry.CreateBox(fallbackBoxSize, 0f, Unity.U2D.Physics.PhysicsTransform.identity);
                var one = new NativeArray<Unity.U2D.Physics.PolygonGeometry>(1, Allocator.Temp) { [0] = box };
                body.CreateShape(box, shapeDef);
                fragmentRenderer?.Add(body, one);
                one.Dispose();
            }

            authoredPolygons.Dispose();

            var area = GetComponent<Unity.U2D.Physics.PhysicsArea>();
            if (area != null)
                Destroy(area);
            if (pose != null)
                Destroy(pose);
        }
    }
}
