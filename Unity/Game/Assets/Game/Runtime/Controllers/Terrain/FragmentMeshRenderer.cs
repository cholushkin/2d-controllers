using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


namespace Game.Gameplay.Controllers.Terrain
{
    // Adapted from Assets/Game/RuntimeRnD/SpriteDestructionExample/FragmenRenderer.cs:
    // draws each rock/debris physics body as a mesh built from its polygons. UV mapping
    // to the source sprite was dropped for this pass - debris uses a flat material.
    public sealed class FragmentMeshRenderer : MonoBehaviour
    {
        [SerializeField] private Material material;

        private readonly Dictionary<Unity.U2D.Physics.PhysicsBody, Transform> _views = new();

        public void Add(Unity.U2D.Physics.PhysicsBody body, NativeArray<Unity.U2D.Physics.PolygonGeometry> polygons)
        {
            var positions = new List<Vector3>();
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
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            var go = new GameObject("RockFragment");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.transform.SetParent(transform, false);

            _views[body] = go.transform;
        }

        public void Remove(Unity.U2D.Physics.PhysicsBody body)
        {
            if (!_views.TryGetValue(body, out var view))
                return;

            if (view != null)
            {
                var mesh = view.GetComponent<MeshFilter>().sharedMesh;
                if (mesh != null)
                    Destroy(mesh);
                Destroy(view.gameObject);
            }

            _views.Remove(body);
        }

        private void LateUpdate()
        {
            foreach (var pair in _views)
            {
                var body = pair.Key;
                var view = pair.Value;
                if (view == null)
                    continue;

                var p = body.transform.position;
                view.position = new Vector3(p.x, p.y, 0f);
                view.rotation = Quaternion.Euler(0f, 0f, body.rotation.radians * Mathf.Rad2Deg);
            }
        }
    }
}
