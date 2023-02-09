using UnityEditor;
using UnityEngine;
using System;

namespace RoadGenerator
{
    public static class RoadGeneratorMenu
    {
        private const int MenuPriority = -50;
        private const string PrefabManagerPath = "Assets/RoadGenerator/Core/UI/PrefabManager.asset";
        private static PrefabManager LocatePrefabManager() => AssetDatabase.LoadAssetAtPath<PrefabManager>(PrefabManagerPath);

        [MenuItem("GameObject/RoadGenerator/Create road system", priority = MenuPriority)]
        private static void CreateRoadSystem()
        {
            SafeInstantiate(prefabManager => prefabManager.RoadSystem, instance => addRoadSystem(instance));
        }

        private static void SafeInstantiate(Func<PrefabManager, GameObject> itemSelector, Func<UnityEngine.Object, bool> onInstantiate)
        {
            var prefabManager = LocatePrefabManager();

            if (!prefabManager)
            {
                Debug.LogWarning($"PrefabManager not found at path {PrefabManagerPath}");
                return;
            }

            var item = itemSelector(prefabManager);
            var instance = PrefabUtility.InstantiatePrefab(item, Selection.activeTransform);
            onInstantiate(instance);

            Undo.RegisterCreatedObjectUndo(instance, $"Create {instance.name}");
        }

        private static bool addRoadSystem(UnityEngine.Object instance)
        {
            GameObject roadSystemObject = instance as GameObject;

            Transform roads = roadSystemObject.transform.Find("Roads");
            if(roads == null)
            {
                Debug.LogError("Roads not found in RoadSystem");
                return false;
            }

            GameObject roadToSelect = null;

            // Update all roads
            foreach(Transform road in roads)
            {
                if(roadToSelect == null)
                {
                    roadToSelect = road.gameObject;
                }

                road.GetComponent<PathSceneTool>().TriggerUpdate();
            }

            return true;
        }
    }
}
