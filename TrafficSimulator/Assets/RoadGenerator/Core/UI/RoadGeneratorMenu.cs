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
            Debug.Log("Creating road system...");
            SafeInstantiate(prefabManager => prefabManager.RoadSystem, instance => instantiateRoads(instance));
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

        private static bool instantiateRoads(UnityEngine.Object instance)
        {
            var roadSystem = instance as GameObject;
            Transform roads = roadSystem.transform.Find("Roads");
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

            //Selection.activeGameObject = roadToSelect;

            return true;
        }
    }
}
