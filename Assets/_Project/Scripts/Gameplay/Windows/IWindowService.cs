namespace _Project.Scripts.Gameplay.Windows
{
    public interface IWindowService
    {
        void Open(WindowId windowId);
        void Close(WindowId windowId);
    }
}
