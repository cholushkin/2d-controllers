using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

// Draws each physics body as a mesh built from its polygons, UV-mapped to the source sprite texture.
public sealed class FragmentRenderer : MonoBehaviour
{
    private readonly Dictionary<PhysicsBody, Transform> m_Views = new();
    private Material m_Material;
    private Vector2 m_UvScale;

    public void Initialize(Texture2D texture, Vector2 spriteSize)
    {
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        m_Material = new Material(shader) { mainTexture = texture };
        m_UvScale = new Vector2(1f / spriteSize.x, 1f / spriteSize.y);
    }

    public void Add(PhysicsBody body, NativeArray<PolygonGeometry> polygons)
    {
        var positions = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        foreach (var polygon in polygons)
        {
            var count = polygon.count;
            if (count < 3)
                continue;

            var root = positions.Count;
            for (var i = 0; i < count; ++i)
            {
                var v = polygon.vertices[i];
                positions.Add(new Vector3(v.x, v.y, 0f));
                uvs.Add(new Vector2(v.x * m_UvScale.x, v.y * m_UvScale.y) + Vector2.one * 0.5f);
            }

            for (var i = 2; i < count; ++i)
            {
                triangles.Add(root);
                triangles.Add(root + i);
                triangles.Add(root + i - 1);
            }
        }

        if (positions.Count == 0)
            return;

        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(positions);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        var go = new GameObject("Fragment");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = m_Material;
        go.transform.SetParent(transform, false);

        m_Views[body] = go.transform;
    }

    public void Remove(PhysicsBody body)
    {
        if (!m_Views.TryGetValue(body, out var view))
            return;

        if (view != null)
        {
            var mesh = view.GetComponent<MeshFilter>().sharedMesh;
            if (mesh != null) Destroy(mesh);
            Destroy(view.gameObject);
        }
        m_Views.Remove(body);
    }

    private void LateUpdate()
    {
        foreach (var pair in m_Views)
        {
            var body = pair.Key;
            var view = pair.Value;
            if (view == null) continue;

            var p = body.transform.position;
            view.position = new Vector3(p.x, p.y, 0f);
            view.rotation = Quaternion.Euler(0f, 0f, body.rotation.angle * Mathf.Rad2Deg);
        }
    }

    private void OnDestroy()
    {
        if (m_Material != null) Destroy(m_Material);
    }
}