using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeeklyActivities
{
    public partial class Form1 : Form
    {
        private DateTime CurrentDateTime;
        private SynchronizationContext uiContext;

        public Form1()
        {
            uiContext = WindowsFormsSynchronizationContext.Current;

            InitializeComponent();

            GoToWeek(DateTime.Now.AddDays(1));

            var t = new Thread(new ThreadStart(Run));
            t.IsBackground = true;
            t.Start();
        }

        public void GoToWeek(DateTime date)
        {
            CurrentDateTime = date;

            labelTitle.Text = "Week: "
                + GetPreviousWeekday(date, DayOfWeek.Monday).ToLongDateString()
                + " - "
                + GetNextWeekday(date, DayOfWeek.Sunday).ToLongDateString();

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < 7; i++)
            {
                var curDate = GetPreviousWeekday(date, DayOfWeek.Monday).AddDays(i);
                var logFile = GetLogFile(curDate);
                int totalSeconds = 0;
                TimeSpan first = TimeSpan.MinValue;
                TimeSpan last = TimeSpan.MinValue;
                try
                {
                    if (File.Exists(logFile))
                    {
                        using (StreamReader sr = File.OpenText(logFile))
                        {
                            string s = String.Empty;
                            while ((s = sr.ReadLine()) != null)
                            {
                                int firstPos = s.IndexOf(';');
                                if (firstPos == -1)
                                {
                                    continue;
                                }
                                var duration = int.Parse(s.Substring(0, firstPos));
                                totalSeconds += duration;

                                var timeStr = s.Substring(firstPos + 1);
                                var hours = int.Parse(timeStr.Substring(0, 2));
                                var minutes = int.Parse(timeStr.Substring(2, 2));
                                var seconds = int.Parse(timeStr.Substring(4, 2));
                                var time = new TimeSpan(hours, minutes, seconds);
                                if (first == TimeSpan.MinValue)
                                {
                                    first = time;
                                }
                                last = time;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }

                builder.Append(curDate.DayOfWeek.ToString().PadRight(10));
                builder.Append(" ");

                builder.Append(curDate.ToShortDateString());
                builder.Append("   ");

                if (totalSeconds == 0)
                {
                    builder.Append("Not Available");
                }
                else
                {
                    builder.Append(first.ToString());
                    builder.Append(" - ");
                    builder.Append(last.ToString());
                    builder.Append("  ");
                    builder.Append("(");
                    builder.Append(TimeSpan.FromSeconds(totalSeconds));
                    builder.Append(")");
                }
                builder.Append(Environment.NewLine);
            }

            labelResult.Text = builder.ToString();
        }
        
        public void Run()
        {
            var tick = Environment.TickCount;
            var userActivity = string.Empty;
            var oldUserActivity = string.Empty;
            var tickSleep = 1000 * 60 * 15;
            var startPeriod = DateTime.Now;

            while (Thread.CurrentThread.IsAlive)
            {
                try
                {
                    if (int.TryParse(ConfigurationManager.AppSettings["InactivityDelayInMinutes"].ToString(), out int inactivityDelayInMinutes))
                    {
                        tickSleep = 1000 * 60 * inactivityDelayInMinutes;
                    }

                    var lastInput = Win32Wrapper.GetLastInputTime();
                    var isUserActive = lastInput >= tick - tickSleep;
                    userActivity = isUserActive.ToString() + ";" + lastInput;

                    if (!isUserActive)
                    {
                        startPeriod = DateTime.Now;
                    }
                    else
                    {
                        var endPeriod = DateTime.Now;
                        var diffSeconds = (endPeriod - startPeriod).Seconds;
                        var logEntry = diffSeconds + ";" +startPeriod.ToString("HHmmss");
                        startPeriod = endPeriod;
                        
                        // Log only active activity.
                        if (isUserActive)
                        {
                            var date = DateTime.Today;

                            var previousDate = GetPreviousWeekday(date, DayOfWeek.Monday);
                            var nextDate = GetNextWeekday(date, DayOfWeek.Monday);

                            uiContext.Send(new SendOrPostCallback(
                                delegate (object state)
                                {
                                    GoToWeek(CurrentDateTime);
                                }
                            ), null);

                            string logFile = GetLogFile(date);
                            File.AppendAllText(logFile, logEntry + Environment.NewLine);
                        }
                    }
                }
                catch (Exception)
                {
                }

                oldUserActivity = userActivity;
                tick = Environment.TickCount;
                Thread.Sleep(2000);
            }
        }

        private static string GetLogFile(DateTime date)
        {
            string path = GetLogDir(date);
            string logFile = path + date.Day + ".txt";
            return logFile;
        }

        private static string GetLogDir(DateTime date)
        {
            var path = @"log\" + date.Year + @"\" + date.Month + @"\";
            Directory.CreateDirectory(path);
            return path;
        }

        public static DateTime GetPreviousWeekday(DateTime start, DayOfWeek day)
        {
            return GetNextWeekday(start.AddDays(-7), day);
        }

        public static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            GoToWeek(CurrentDateTime.AddDays(-7));
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            GoToWeek(CurrentDateTime.AddDays(7));
        }
    }
}
