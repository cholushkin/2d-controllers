using System;
using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // Resolves the live body from an editor-authored PhysicsPose (+ any PhysicsArea*
    // shape) instead of code creating one. A shape's category can only be overridden
    // after AddComponent via an asset (PhysicsArea.contactAsset), never a bare struct,
    // so this builds that asset at runtime - PhysicsCategories stays the single source
    // of truth in code and nobody has to hand-author N .asset files.
    public static class PoseBodyAuthoring
    {
        public static Unity.U2D.Physics.PhysicsBody Resolve(GameObject go, Unity.U2D.Physics.PhysicsMask categories, Unity.U2D.Physics.PhysicsMask contacts)
        {
            var pose = go.GetComponent<Unity.U2D.Physics.PhysicsPose>();
            if (pose == null)
                throw new InvalidOperationException(
                    $"{go.name}: expected a PhysicsPose (+ a PhysicsArea* shape) authored in the editor, found none.");

            var area = go.GetComponent<Unity.U2D.Physics.PhysicsArea>();
            if (area != null)
            {
                var filter = ScriptableObject.CreateInstance<Unity.U2D.Physics.Assets.PhysicsContactFilter>();
                filter.value = new Unity.U2D.Physics.PhysicsShape.ContactFilter { categories = categories, contacts = contacts };
                area.contactAsset = filter;
                area.ApplyDefinition();
            }

            return pose.body;
        }
    }
}
