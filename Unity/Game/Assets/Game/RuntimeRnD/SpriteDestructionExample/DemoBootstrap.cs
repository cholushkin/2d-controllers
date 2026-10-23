using UnityEngine;
using UnityEngine.InputSystem;


public sealed class DemoBootstrap : MonoBehaviour
{
    [SerializeField] private Sprite buildingSprite;
    [SerializeField] private Camera cam;
    [SerializeField] private DestructionSettings settings = new();
    [SerializeField] private float gravityScale = 5f;
    [SerializeField] private int columns;
    [SerializeField] private int rows;

    private DestructionController m_Destruction;

    private void Start()
    {
        if (cam == null) cam = Camera.main;

        var world = Unity.U2D.Physics.PhysicsWorld.defaultWorld;
        world.gravity = world.gravity * gravityScale;

        //CreateGround(world);

        var rendererGO = new GameObject("FragmentRenderer");
        var renderer = rendererGO.AddComponent<FragmentRenderer>();
        renderer.Initialize(buildingSprite.texture, buildingSprite.bounds.size);

        m_Destruction = gameObject.AddComponent<DestructionController>();
        m_Destruction.Initialize(world, renderer, buildingSprite, settings);
        
        float spacingX = 12f;
        float spacingY = 15f;

        float startX = -((columns - 1) * spacingX) / 2f;
        float startY = 0f; // Starting Y elevation

        for (int y = 0; y < rows; ++y)
        {
            for (int x = 0; x < columns; ++x)
            {
                var spawnPos = new Vector2(startX + (x * spacingX), startY + (y * spacingY));
                m_Destruction.SpawnDestructible(spawnPos);
            }
        }
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePos = mouse.position.ReadValue();
        // Pass the distance from the camera (e.g., -cam.transform.position.z for orthographic cameras)
        Vector3 screenPoint = new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z);
    
        var worldPoint = (Vector2)cam.ScreenToWorldPoint(screenPoint);
        Debug.Log($"{mousePos} -> {worldPoint}");
        m_Destruction.Detonate(worldPoint);
    }

    private static void CreateGround(Unity.U2D.Physics.PhysicsWorld world)
    {
        var body = world.CreateBody();
        var def = new Unity.U2D.Physics.PhysicsShapeDefinition
        {
            contactFilter = new Unity.U2D.Physics.PhysicsShape.ContactFilter
            {
                categories = new Unity.U2D.Physics.PhysicsMask(2),
                contacts = new Unity.U2D.Physics.PhysicsMask(4) | new Unity.U2D.Physics.PhysicsMask(3)
            }
        };
        body.CreateShape(Unity.U2D.Physics.PolygonGeometry.CreateBox(new Vector2(110f, 10f), 0f, new Unity.U2D.Physics.PhysicsTransform(Vector2.down * 50f)), def);
        body.CreateShape(Unity.U2D.Physics.PolygonGeometry.CreateBox(new Vector2(110f, 10f), 0f, new Unity.U2D.Physics.PhysicsTransform(Vector2.up * 50f)), def);
        body.CreateShape(Unity.U2D.Physics.PolygonGeometry.CreateBox(new Vector2(10f, 100f), 0f, new Unity.U2D.Physics.PhysicsTransform(Vector2.left * 50f)), def);
        body.CreateShape(Unity.U2D.Physics.PolygonGeometry.CreateBox(new Vector2(10f, 100f), 0f, new Unity.U2D.Physics.PhysicsTransform(Vector2.right * 50f)), def);
    }
}