using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

using Random = Unity.Mathematics.Random;

public sealed class DestructionController : MonoBehaviour, Unity.U2D.Physics.PhysicsCallbacks.IContactCallback
{
    private Unity.U2D.Physics.PhysicsWorld m_World;
    private FragmentRenderer m_Renderer;
    private Sprite m_Sprite;
    private DestructionSettings m_Settings;

    private readonly Unity.U2D.Physics.PhysicsMask m_ObstacleMask = new(1);
    private readonly Unity.U2D.Physics.PhysicsMask m_GroundMask = new(2);
    private readonly Unity.U2D.Physics.PhysicsMask m_DestructibleMask = new(3);
    private readonly Unity.U2D.Physics.PhysicsMask m_DebrisMask = new(4);

    private Unity.U2D.Physics.PhysicsShape.ContactFilter m_DestructibleContactFilter;
    private Unity.U2D.Physics.PhysicsShape.SurfaceMaterial m_SurfaceMaterial;
    private NativeArray<Unity.U2D.Physics.PolygonGeometry> m_FragmentMask;
    private Unity.U2D.Physics.SegmentGeometry m_VirtualGround;
    private Unity.U2D.Physics.PhysicsTransform m_VirtualGroundTransform;
    private Random m_Rng = new(1234);
    private readonly List<Vector2> m_OutlineVertices = new();

    public void Initialize(Unity.U2D.Physics.PhysicsWorld world, FragmentRenderer renderer, Sprite sprite, DestructionSettings settings)
    {
        m_World = world;
        m_Renderer = renderer;
        m_Sprite = sprite;
        m_Settings = settings;

        m_DestructibleContactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter
        {
            categories = m_DestructibleMask,
            contacts = m_GroundMask | m_DebrisMask | m_DestructibleMask
        };

        m_VirtualGround = new Unity.U2D.Physics.SegmentGeometry { point1 = new Vector2(-100f, 0f), point2 = new Vector2(100f, 0f) };
        m_VirtualGroundTransform = new Unity.U2D.Physics.PhysicsTransform(Vector2.down * 7.25f);

        CreateFragmentMask();
    }

    private void OnDestroy()
    {
        if (m_FragmentMask.IsCreated) m_FragmentMask.Dispose();
    }

    // Called on every click: fragment whatever destructible is under the point, then blast the area.
    public void Detonate(Vector2 point)
    {
        DestructAt(point);
        m_World.Explode(new Unity.U2D.Physics.PhysicsWorld.ExplosionDefinition
        {
            position = point,
            hitCategories = m_DebrisMask,
            impulsePerLength = m_Settings.explosionImpulse,
            radius = m_Settings.fragmentRadius * m_Settings.explosionRadiusMultiplier
        });
    }

    public void SpawnDestructible(Vector2 worldPosition)
    {
        if (m_Sprite == null || m_Sprite.packed)
        {
            Debug.LogError("Sprite is missing or packed in an atlas.");
            return;
        }

        var outlines = m_Sprite.GetPhysicsOutlineCount();
        if (outlines == 0)
        {
            Debug.LogError("Sprite has no physics shape. Set Custom Physics Shape in the Sprite Editor.");
            return;
        }

        var composer = Unity.U2D.Physics.PhysicsComposer.Create(Allocator.Temp);
        composer.useDelaunay = true;
        var path = new NativeList<Vector2>(Allocator.Temp);

        for (var i = 0; i < outlines; ++i)
        {
            if (m_Sprite.GetPhysicsOutline(i, m_OutlineVertices) > 0)
            {
                foreach (var v in m_OutlineVertices) path.Add(v);
                composer.AddLayer(path.AsArray(), Unity.U2D.Physics.PhysicsTransform.identity);
            }
            path.Clear();
        }

        using var polygons = composer.CreatePolygonGeometry(vertexScale: Vector2.one);
        path.Dispose();
        composer.Destroy();

        if (polygons.Length == 0)
            return;

        var body = m_World.CreateBody(new Unity.U2D.Physics.PhysicsBodyDefinition { position = worldPosition });
        var shapeDef = new Unity.U2D.Physics.PhysicsShapeDefinition
        {
            contactFilter = m_DestructibleContactFilter,
            contactEvents = true,
            surfaceMaterial = m_SurfaceMaterial
        };

        using var batch = body.CreateShapeBatch(polygons, shapeDef);
        m_Renderer.Add(body, polygons);
    }

    private void DestructAt(Vector2 hitPosition)
    {
        CreateFragmentMask();

        using var hits = m_World.OverlapPoint(hitPosition,
            new Unity.U2D.Physics.PhysicsQuery.QueryFilter { categories = m_DestructibleMask, hitCategories = m_DestructibleMask });
        if (hits.Length == 0)
            return;

        var body = hits[0].shape.body;
        var bodyType = body.type;
        var bodyVelocity = body.linearVelocity;
        using var shapes = body.GetShapes();

        var targetPolygons = new NativeList<Unity.U2D.Physics.PolygonGeometry>(shapes.Length, Allocator.Temp);
        foreach (var shape in shapes)
        {
            if (shape.shapeType == Unity.U2D.Physics.PhysicsShape.ShapeType.Polygon)
                targetPolygons.Add(shape.polygonGeometry);
        }
        var target = new Unity.U2D.Physics.PhysicsDestructor.FragmentGeometry(body.transform, targetPolygons.AsReadOnly());
        targetPolygons.Dispose();

        var points = new NativeArray<Vector2>(m_Settings.fragmentCount, Allocator.Temp);
        points[0] = hitPosition;
        for (var i = 1; i < m_Settings.fragmentCount; ++i)
        {
            var angle = m_Rng.NextFloat(0f, Mathf.PI * 2f);
            var radius = m_Rng.NextFloat(0.05f, m_Settings.fragmentRadius);
            points[i] = hitPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        var mask = new Unity.U2D.Physics.PhysicsDestructor.FragmentGeometry(hitPosition, m_FragmentMask);
        using var results = Unity.U2D.Physics.PhysicsDestructor.Fragment(target, mask, points, Allocator.Temp);
        points.Dispose();

        RemovePiece(body);

        m_SurfaceMaterial = new Unity.U2D.Physics.PhysicsShape.SurfaceMaterial
        {
            friction = m_Settings.fragmentFriction,
            bounciness = m_Settings.fragmentBounciness,
            tangentSpeed = 0f
        };

        var fragmentTransform = results.transform;
        var staticDef = new Unity.U2D.Physics.PhysicsBodyDefinition { position = fragmentTransform.position, rotation = fragmentTransform.rotation };
        var dynamicDef = new Unity.U2D.Physics.PhysicsBodyDefinition
        {
            type = Unity.U2D.Physics.PhysicsBody.BodyType.Dynamic,
            fastCollisionsAllowed = true,
            position = fragmentTransform.position,
            rotation = fragmentTransform.rotation,
            linearVelocity = bodyVelocity,
            gravityScale = 1f
        };

        // Remaining unbroken pieces: static if they touch the virtual ground (and the original was static), else they fall.
        if (results.unbrokenGeometry.Length > 0)
        {
            foreach (var range in results.unbrokenGeometryIslands)
            {
                var island = results.unbrokenGeometry.GetSubArray(range.start, range.length);
                var asDynamic = true;

                if (bodyType == Unity.U2D.Physics.PhysicsBody.BodyType.Static)
                {
                    foreach (var geometry in island)
                    {
                        if (geometry.Intersect(fragmentTransform, m_VirtualGround, m_VirtualGroundTransform).pointCount == 0)
                            continue;
                        asDynamic = false;
                        break;
                    }
                }

                var islandBody = m_World.CreateBody(asDynamic ? dynamicDef : staticDef);
                var shapeDef = new Unity.U2D.Physics.PhysicsShapeDefinition
                {
                    contactFilter = m_DestructibleContactFilter,
                    contactEvents = true,
                    surfaceMaterial = m_SurfaceMaterial
                };
                using var batch = islandBody.CreateShapeBatch(island, shapeDef);
                if (asDynamic)
                {
                    foreach (var shape in batch)
                        shape.callbackTarget = this;
                }
                m_Renderer.Add(islandBody, island);
            }
        }

        // Debris pieces: each becomes a dynamic body that can hit the ground and be removed.
        if (m_Settings.createFragments && results.brokenGeometry.Length > 0)
        {
            using var bodies = m_World.CreateBodyBatch(dynamicDef, results.brokenGeometry.Length);
            var debrisDef = new Unity.U2D.Physics.PhysicsShapeDefinition
            {
                contactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter
                {
                    categories = m_DebrisMask,
                    contacts = m_GroundMask | m_DestructibleMask | m_ObstacleMask | m_DebrisMask
                },
                contactEvents = true,
                surfaceMaterial = m_SurfaceMaterial
            };

            for (var i = 0; i < results.brokenGeometry.Length; ++i)
            {
                var piece = bodies[i];
                var geometry = results.brokenGeometry[i];
                var shape = piece.CreateShape(geometry, debrisDef);
                shape.callbackTarget = this;

                var one = new NativeArray<Unity.U2D.Physics.PolygonGeometry>(1, Allocator.Temp) { [0] = geometry };
                m_Renderer.Add(piece, one);
                one.Dispose();
            }
        }
    }

    private void CreateFragmentMask()
    {
        if (m_FragmentMask.IsCreated) m_FragmentMask.Dispose();

        var vertices = new NativeList<Vector2>(100, Allocator.Temp);
        const float tau = Mathf.PI * 2f;
        const float stride = tau / 36f;
        for (var angle = 0f; angle < tau; angle += stride)
        {
            var r = m_Rng.NextFloat(m_Settings.fragmentRadius * 0.9f, m_Settings.fragmentRadius * 1.1f);
            vertices.Add(new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r));
        }
        m_FragmentMask = Unity.U2D.Physics.PolygonGeometry.CreatePolygons(vertices.AsArray(), Unity.U2D.Physics.PhysicsTransform.identity, Vector2.one, Allocator.Persistent);
        vertices.Dispose();
    }

    private void RemovePiece(Unity.U2D.Physics.PhysicsBody body)
    {
        m_Renderer.Remove(body);
        body.Destroy();
    }

    // Debris that lands on the ground is removed to keep the scene clean.
    public void OnContactBegin2D(Unity.U2D.Physics.PhysicsEvents.ContactBeginEvent beginEvent)
    {
        var a = beginEvent.shapeA;
        var b = beginEvent.shapeB;
        if (!a.isValid || !b.isValid) return;

        var ca = a.contactFilter.categories;
        var cb = b.contactFilter.categories;
        if (ca != m_GroundMask && cb != m_GroundMask) return;

        var debris = ca == m_GroundMask ? b : a;
        RemovePiece(debris.body);
    }

    public void OnContactEnd2D(Unity.U2D.Physics.PhysicsEvents.ContactEndEvent endEvent) { }
}