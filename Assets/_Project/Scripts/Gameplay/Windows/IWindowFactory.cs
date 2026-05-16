using UnityEngine;

namespace _Project.Scripts.Gameplay.Windows
{
    public interface IWindowFactory
    {
        void SetUIRoot(RectTransform uiRoot);
        BaseWindow CreateWindow(WindowId windowId);
    }
}
