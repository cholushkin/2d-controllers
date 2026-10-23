using Game.Gameplay.Controllers.Core;
using Unity.U2D.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Controllers.Editor
{
    // One-shot migration: Unity 6.7's Box2D core ships authoring components
    // (PhysicsPose + PhysicsAreaPolygon) for static geometry, so the hand-rolled
    // runtime LowLevelStaticBaker is no longer needed for level-authored blocks.
    public static class Box2DGroundConverter
    {
        [MenuItem("Tools/Controllers/Convert Static Blocks to Box2D")]
        private static void ConvertOpenScene()
        {
            var bakers = Object.FindObjectsByType<LowLevelStaticBaker>(FindObjectsSortMode.None);
            if (bakers.Length == 0)
            {
                Debug.Log("Box2DGroundConverter: no LowLevelStaticBaker components found in the open scene(s).");
                return;
            }

            var converted = 0;
            var scenes = new System.Collections.Generic.HashSet<Scene>();
            foreach (var baker in bakers)
            {
                var go = baker.gameObject;
                var boxSize = GetBoxSize(baker);
                scenes.Add(go.scene);

                Undo.RegisterCompleteObjectUndo(go, "Convert Static Block to Box2D");

                var pose = Undo.AddComponent<PhysicsPose>(go);
                pose.definition = new PhysicsBodyDefinition { type = PhysicsBody.BodyType.Static };
                pose.ApplyDefinition();

                var area = Undo.AddComponent<PhysicsAreaPolygon>(go);
                area.geometry = PolygonGeometry.CreateBox(boxSize, 0f, PhysicsTransform.identity);
                area.ApplyGeometry();

                Undo.DestroyObjectImmediate(baker);
                converted++;
            }

            foreach (var scene in scenes)
                EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"Box2DGroundConverter: converted {converted} static block(s) to PhysicsPose + PhysicsAreaPolygon.");
        }

        [MenuItem("Tools/Controllers/Convert Static Blocks to Box2D", true)]
        private static bool CanConvert() => Object.FindObjectsByType<LowLevelStaticBaker>(FindObjectsSortMode.None).Length > 0;

        private static Vector2 GetBoxSize(LowLevelStaticBaker baker)
        {
            var so = new SerializedObject(baker);
            var prop = so.FindProperty("boxSize");
            return prop != null ? prop.vector2Value : new Vector2(3f, 1f);
        }
    }
}
