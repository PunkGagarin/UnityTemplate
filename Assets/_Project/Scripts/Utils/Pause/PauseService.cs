using System.Collections.Generic;
using Zenject;
using _Project.Scripts.Gameplay.Common.Time;

namespace _Project.Scripts.Utils.Pause
{
    public class PauseService : IPauseHandler
    {
        private readonly List<IPauseHandler> _handlers = new();

        [Inject] private ITimeService _timeService;

        public bool IsPaused { get; private set; }

        public void Register(IPauseHandler handler)
        {
            _handlers.Add(handler);
        }

        public void Unregister(IPauseHandler handler)
        {
            _handlers.Remove(handler);
        }

        public void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;

            if (isPaused)
                _timeService.StopTime();
            else
                _timeService.StartTime();

            foreach (IPauseHandler handler in _handlers)
            {
                handler.SetPaused(isPaused);
            }
        }
    }
}
