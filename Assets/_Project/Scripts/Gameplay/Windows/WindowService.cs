using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Gameplay.Windows
{
    public class WindowService : IWindowService
    {
        [Inject] private IWindowFactory _windowFactory;

        private readonly List<BaseWindow> _openedWindows = new();

        public void Open(WindowId windowId) =>
            _openedWindows.Add(_windowFactory.CreateWindow(windowId));

        public void Close(WindowId windowId)
        {
            BaseWindow window = _openedWindows.Find(x => x.Id == windowId);

            if (window == null)
                return;

            _openedWindows.Remove(window);
            Object.Destroy(window.gameObject);
        }
    }
}
