using System.Collections.Generic;
using UnityEngine;
using System;

namespace Simulation
{
    public enum TimeMode
    {
        Fast,
        Running,
        Rewind,
        Paused
    }

    public sealed class TimeManager : MonoBehaviour
    {
        private static TimeManager _instance;
        public static Action OnSecondChanged;
        public static Action OnMinuteChanged;
        public static Action OnHourChanged;
        public static Action OnDayChanged;
        public static Action OnMonthChanged;
        public static Action OnYearChanged;

        public static TimeMode Mode { get; private set; }
        public static DateTime _dateTime;

        // Ratio between ingame time and real world time. Ex. 1f is 1:1 ratio, 5f is 5:1 ratio.
        public static float SecondToRealTime = 1f;

        private static float _targetSecondToRealTime;
        private static float _timer;

        // Calendar for each month. 0 = January, 11 = December
        private static List<PriorityQueue<TimeManagerEvent>> _calendar = new List<PriorityQueue<TimeManagerEvent>>();

        public static int Second { get => _dateTime.Second; }
        public static int Minute { get => _dateTime.Minute; }
        public static int Hour { get => _dateTime.Hour; }
        public static int Day { get => _dateTime.Day; }
        public static int Month { get => _dateTime.Month; }
        public static int Year { get => _dateTime.Year; }

        public static TimeManager Instance
        {
            get {
                if(!_instance)
                {
                    _instance = new GameObject("TimeManager").AddComponent<TimeManager>();
                    
                    // Do not destroy this object when loading a new scene
                    DontDestroyOnLoad(_instance.gameObject);
                }
                    
                return _instance;
            }
        }

        /// </summary> Returns a formatted string of the current timestamp </summary>
        public string Timestamp
        {
            get => _dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        void Awake()
        {
            // Set start time
            SetTime(DateTime.Now);

            // Initialize calendar
            InitCalendar();

            _timer = SecondToRealTime;
            _targetSecondToRealTime = SecondToRealTime;
            Mode = TimeMode.Running;
        }

        void Update()
        {
            if(Mode == TimeMode.Fast)
                SecondToRealTime = _targetSecondToRealTime * 0.5f;

            if(Mode != TimeMode.Paused)
            {
                _timer -= Time.deltaTime;
                if(_timer <= 0)
                {
                    UpdateSimulationTime();
                    _timer = SecondToRealTime;
                }
                SecondToRealTime = _targetSecondToRealTime;
            }
            // Check if there are any events scheduled for the current time
            CheckSchedule();
        }

        /// <summary> Looks if the next scheduled event should execute at the current time </summary>
        private void CheckSchedule()
        {
            // Check if there are any events scheduled for the current time
            PriorityQueue<TimeManagerEvent> currentMonth = _calendar[Month - 1];
            
            // Dequeue all events that should execute at the current time
            while(currentMonth.Count > 0 && currentMonth.Peek().IsOnOrBefore(_dateTime))
            {
                TimeManagerEvent currentEvent = currentMonth.Dequeue();
                currentEvent.OnEvent?.Invoke();
            }
        }

        /// </summary> Sets the current simulation time </summary>
        private void SetTime(DateTime dateTime)
        {
            DateTime prevTime = _dateTime;
            _dateTime = FormatDateTime(dateTime);

            if(prevTime == null)
                return;

            if(prevTime.Year != _dateTime.Year)
                OnYearChanged?.Invoke();

            if(prevTime.Month != _dateTime.Month)
                OnMonthChanged?.Invoke();

            if(prevTime.Day != _dateTime.Day)
                OnDayChanged?.Invoke();
            
            if(prevTime.Hour != _dateTime.Hour)
                OnHourChanged?.Invoke();
            
            if(prevTime.Minute != _dateTime.Minute)
                OnMinuteChanged?.Invoke();

            if(prevTime.Second != _dateTime.Second)
                OnSecondChanged?.Invoke();
        }

        /// <summary> Updates the current simulation time, moves it forward or backward depending on the mode </summary>
        private void UpdateSimulationTime()
        {
            SetTime(_dateTime.AddSeconds(Mode == TimeMode.Rewind ? -1 : 1));
        }

        /// </summary> Initializes calendar with an empty priority queue for each month </summary>
        private void InitCalendar()
        {
            for(int i = 0; i < 12; i++)
                _calendar.Add(new PriorityQueue<TimeManagerEvent>());
        }

        /// </summary> Adds an event to the calendar </summary>
        public void AddEvent(TimeManagerEvent evt)
        {
            // DateTime.Month is 1-12, but the calendar is 0-11
            _calendar[evt.DateTime.Month - 1].Enqueue(evt);
        }

        /// </summary> Sets current mode to fast forward </summary>
        public void SetModeFastForward()
        {
            Mode = Mode != TimeMode.Fast ? TimeMode.Fast : TimeMode.Running;
        }

        /// </summary> Sets current mode to rewind </summary>
        public void SetModeRewind()
        {
            Mode = Mode != TimeMode.Rewind ? TimeMode.Rewind : TimeMode.Running;
        }

        /// </summary> Sets current mode to paused </summary>
        public void SetModePause()
        {
            Mode = Mode != TimeMode.Paused ? TimeMode.Paused : TimeMode.Running;
            Time.timeScale = Mode == TimeMode.Paused ? 0 : 1;
        }

        /// </summary> Formats a date time to the correct resolution </summary>
        public DateTime FormatDateTime(DateTime dateTime)
        {
            // Recreate the date time to force resolution to seconds
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, DateTimeKind.Local);
        }
    }
}