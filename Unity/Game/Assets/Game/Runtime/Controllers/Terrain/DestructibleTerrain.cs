using System.Collections.Generic;
using Game.Gameplay.Controllers.Core;
using Unity.Collections;
using UnityEngine;

using Random = Unity.Mathematics.Random;

namespace Game.Gameplay.Controllers.Terrain
{
    // Adapted from Assets/Game/RuntimeRnD/SpriteDestructionExample/DestructionController.cs:
    // same sprite-outline-to-polygon + PhysicsDestructor.Fragment carving flow, but
    // debris excludes the Player category and is reaped on a timer instead of on
    // ground contact (see DebrisReaper).
    [DisallowMultipleComponent]
    public sealed class DestructibleTerrain : MonoBehaviour
    {
        [SerializeField] private TerrainConfig config;
        [SerializeField] private Sprite rockSprite;
        [SerializeField] private Vector2 fallbackBoxSize = new(6f, 3f);
        [SerializeField] private DebrisReaper debrisReaper;
        [SerializeField] private FragmentMeshRenderer fragmentRenderer;

        private Unity.U2D.Physics.PhysicsWorld _world;
        private Unity.U2D.Physics.PhysicsShape.ContactFilter _rockContactFilter;
        private Unity.U2D.Physics.PhysicsShape.SurfaceMaterial _rockSurfaceMaterial;
        private NativeArray<Unity.U2D.Physics.PolygonGeometry> _carveMask;
        private Random _rng = new(7919);
        private readonly List<Vector2> _outline = new();

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
            SpawnRockBody();
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

        private void SpawnRockBody()
        {
            if (rockSprite == null || rockSprite.GetPhysicsOutlineCount() == 0)
            {
                SpawnRockBox();
                return;
            }

            SpawnRockFromSprite();
        }

        // No sprite / no Custom Physics Shape assigned (e.g. in a fresh sandbox scene) -
        // carve a plain rectangular slab instead, so the rock needs no Sprite Editor step.
        private void SpawnRockBox()
        {
            var box = Unity.U2D.Physics.PolygonGeometry.CreateBox(fallbackBoxSize, 0f, Unity.U2D.Physics.PhysicsTransform.identity);
            var polygons = new NativeArray<Unity.U2D.Physics.PolygonGeometry>(1, Allocator.Temp) { [0] = box };

            var body = _world.CreateBody(new Unity.U2D.Physics.PhysicsBodyDefinition { position = transform.position });
            body.CreateShape(box, new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = _rockContactFilter,
                surfaceMaterial = _rockSurfaceMaterial
            });

            fragmentRenderer?.Add(body, polygons);
            polygons.Dispose();
        }

        private void SpawnRockFromSprite()
        {
            var composer = Unity.U2D.Physics.PhysicsComposer.Create(Allocator.Temp);
            composer.useDelaunay = true;
            var path = new NativeList<Vector2>(Allocator.Temp);

            var outlines = rockSprite.GetPhysicsOutlineCount();
            for (var i = 0; i < outlines; ++i)
            {
                if (rockSprite.GetPhysicsOutline(i, _outline) > 0)
                {
                    foreach (var v in _outline)
                        path.Add(v);
                    composer.AddLayer(path.AsArray(), Unity.U2D.Physics.PhysicsTransform.identity);
                }
                path.Clear();
            }

            using var polygons = composer.CreatePolygonGeometry(vertexScale: Vector2.one);
            path.Dispose();
            composer.Destroy();

            if (polygons.Length == 0)
                return;

            var body = _world.CreateBody(new Unity.U2D.Physics.PhysicsBodyDefinition { position = transform.position });
            using var batch = body.CreateShapeBatch(polygons, new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = _rockContactFilter,
                surfaceMaterial = _rockSurfaceMaterial
            });

            fragmentRenderer?.Add(body, polygons);
        }
    }
}
