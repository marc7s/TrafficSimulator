using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace UI
{
    public static class SceneLoader
    {
        public struct SceneConfig
        {
            public string DisplayName { get; }
            public string SceneName { get; }
            public string IconFileName { get; }

            public SceneConfig(string displayName, string sceneName, string iconFileName)
            {
                DisplayName = displayName;
                SceneName = sceneName;
                IconFileName = iconFileName;
            }
        }

        public class SceneInfo
        {
            public string DisplayName { get; }
            public string SceneName { get; }
            public Texture2D Icon { get; }

            public SceneInfo(string displayName, string sceneName, Texture2D icon)
            {
                DisplayName = displayName;
                SceneName = sceneName;
                Icon = icon;
            }
        }

        private static List<SceneConfig> _sceneConfigs = new List<SceneConfig>() {
            new SceneConfig(displayName: "Demo", sceneName: "Demo Build", iconFileName: "Demo Icon"),
            new SceneConfig(displayName: "Town", sceneName: "Town Build", iconFileName: "Town Icon"),
            new SceneConfig(displayName: "Masthugget", sceneName: "Masthugget Build", iconFileName: "Masthugget Icon")
        };

        private static List<SceneInfo> _sceneInfos = new List<SceneInfo>();

        public static Action<SceneInfo> OnSceneSwitch;
        public static SceneInfo DefaultScene = null;

        public static void LoadScenes(VisualElement root, VisualTreeAsset sceneTemplate)
        {
            VisualElement grid = root.Q<VisualElement>("Grid");

            _sceneInfos.Clear();
            
            foreach (SceneConfig sceneConfig in _sceneConfigs)
            {
                VisualElement sceneElement = sceneTemplate.CloneTree();
                SceneInfo sceneInfo = new SceneInfo(sceneConfig.DisplayName, sceneConfig.SceneName, Resources.Load<Texture2D>("UI/Scenes/" + sceneConfig.IconFileName));
                _sceneInfos.Add(sceneInfo);

                if(DefaultScene == null)
                    DefaultScene = sceneInfo;
                
                Action onClick = () => OnSceneSwitch.Invoke(sceneInfo);
                SetSceneInfo(ref sceneElement, sceneInfo, onClick);
                
                grid.Add(sceneElement);
            }
        }

        public static SceneInfo GetSceneInfo(string sceneName)
        {
            return _sceneInfos.Find(sceneInfo => sceneInfo.SceneName == sceneName) ?? DefaultScene;
        }

        public static void SetSceneInfo(ref VisualElement sceneElement, SceneInfo sceneInfo, Action onClick = null)
        {
            sceneElement.Q<Label>("Name").text = sceneInfo.DisplayName;
            sceneElement.Q<VisualElement>("SceneIcon").style.backgroundImage = sceneInfo.Icon;

            if(onClick != null)
                sceneElement.Q<Button>("Scene").clicked += () => onClick.Invoke();
        }
    }
}