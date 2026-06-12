using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using _Project.Scripts.Gameplay.Windows.Configs;

namespace _Project.Scripts.Gameplay.Windows
{
    public class WindowFactory : IWindowFactory
    {
        private const string WINDOWS_CONFIG_PATH = "Configs/Windows/windowConfig";

        [Inject] private IInstantiator _instantiator;

        private RectTransform _uiRoot;
        private Dictionary<WindowId, GameObject> _windowPrefabsById;

        public void SetUIRoot(RectTransform uiRoot) =>
            _uiRoot = uiRoot;

        public BaseWindow CreateWindow(WindowId windowId)
        {
            if (_uiRoot == null)
                throw new InvalidOperationException("UIRoot is not set. Add UIInitializer to the scene before opening windows.");

            BaseWindow window = _instantiator.InstantiatePrefabForComponent<BaseWindow>(PrefabFor(windowId), _uiRoot);
            StretchToUIRoot(window);
            window.gameObject.SetActive(true);

            return window;
        }

        private static void StretchToUIRoot(BaseWindow window)
        {
            RectTransform rectTransform = window.GetComponent<RectTransform>();

            if (rectTransform == null)
                return;

            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private GameObject PrefabFor(WindowId windowId)
        {
            LoadConfigIfNeeded();

            return _windowPrefabsById.TryGetValue(windowId, out GameObject prefab)
                ? prefab
                : throw new InvalidOperationException($"Prefab config for window {windowId} was not found.");
        }

        private void LoadConfigIfNeeded()
        {
            if (_windowPrefabsById != null)
                return;

            WindowsConfig config = Resources.Load<WindowsConfig>(WINDOWS_CONFIG_PATH);

            if (config == null)
                throw new InvalidOperationException($"Windows config was not found at Resources/{WINDOWS_CONFIG_PATH}.");

            _windowPrefabsById = config.WindowConfigs.ToDictionary(x => x.Id, x => x.Prefab);
        }
    }
}
