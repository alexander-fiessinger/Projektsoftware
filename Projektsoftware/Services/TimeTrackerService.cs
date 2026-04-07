using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace Projektsoftware.Services
{
    /// <summary>
    /// Timer-Service für Live-Zeiterfassung
    /// </summary>
    public class TimeTrackerService
    {
        private DispatcherTimer timer;
        private Stopwatch stopwatch;
        private DateTime startTime;
        private string currentProject;
        private string currentActivity;

        public bool IsRunning => timer != null && timer.IsEnabled;
        public TimeSpan ElapsedTime => stopwatch?.Elapsed ?? TimeSpan.Zero;
        public DateTime StartTime => startTime;
        public int CurrentProjectId { get; private set; }
        public string CurrentProject => currentProject;
        public string CurrentActivity => currentActivity;

        public event EventHandler<TimeSpan> Tick;
        public event EventHandler Started;
        public event EventHandler Stopped;

        public TimeTrackerService()
        {
            stopwatch = new Stopwatch();
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
        }

        public void Start(int projectId, string project, string activity)
        {
            if (!IsRunning)
            {
                CurrentProjectId = projectId;
                currentProject = project;
                currentActivity = activity;
                startTime = DateTime.Now;
                stopwatch.Restart();
                timer.Start();
                Started?.Invoke(this, EventArgs.Empty);
            }
        }

        public TimeSpan Stop()
        {
            if (IsRunning)
            {
                stopwatch.Stop();
                timer.Stop();
                var elapsed = stopwatch.Elapsed;
                Stopped?.Invoke(this, EventArgs.Empty);
                return elapsed;
            }
            return TimeSpan.Zero;
        }

        public void Reset()
        {
            Stop();
            stopwatch.Reset();
            CurrentProjectId = 0;
            currentProject = null;
            currentActivity = null;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Tick?.Invoke(this, stopwatch.Elapsed);
        }

        public string GetFormattedTime()
        {
            var time = ElapsedTime;
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
    }
}
