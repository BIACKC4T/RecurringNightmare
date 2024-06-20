using System;
using UnityEngine;

namespace Unity.Muse.Animate
{
    static class EntityUtils
    {
        public static EntitySelectionView SetupSelectionView(SelectionModel<EntityID> selectionModel, EntityID entityID, GameObject gameObject)
        {
            // Setup colliders and layer for collision
            var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            ColliderUtils.SetupColliders(skinnedMeshRenderers);
            var meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
            ColliderUtils.SetupColliders(meshRenderers);
            gameObject.SetLayer(ApplicationLayers.LayerHandles, true);

            // Add selection view
            var selectionViewModel = new EntitySelectionViewModel(entityID, selectionModel);
            var selectionView = gameObject.AddComponent<EntitySelectionView>();
            if (!UnityEngine.Application.isPlaying)
            {
                selectionView.Awake();
            }
            selectionView.SetModel(selectionViewModel);

            return selectionView;
        }

        public static EntityView SetupEntityView(GameObject gameObject)
        {
            var contextMenuView = gameObject.AddComponent<EntityView>();
            return contextMenuView;
        }
    }
}
