using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AppPositioner
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr h, int cmd);

        static ManagementEventWatcher processStartEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStartTrace");
        //static ManagementEventWatcher processStopEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStopTrace");
    
        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        public static string MonitorName { get; set; }
        public static int MonitorNumber { get; set; }
        public static string ProgramName { get; set; }
        public static int Width { get; set; }
        public static int Height { get; set; }
        public static bool FullScreen { get; set; }

        static void Main(string[] args)
        {
            try
            {
                ProgramName = ConfigurationManager.AppSettings["ProgramName"];
                MonitorNumber = Convert.ToInt32(ConfigurationManager.AppSettings["MonitorNumber"]);
                MonitorName = ConfigurationManager.AppSettings["MonitorName"];
                Width = Convert.ToInt32(ConfigurationManager.AppSettings["Width"]);
                Height = Convert.ToInt32(ConfigurationManager.AppSettings["Height"]);
                FullScreen = Convert.ToBoolean(ConfigurationManager.AppSettings["FullScreen"]);

                processStartEvent.EventArrived += new EventArrivedEventHandler(processStartEvent_EventArrived);
                processStartEvent.Start();
                //processStopEvent.EventArrived += new EventArrivedEventHandler(processStopEvent_EventArrived);
                //processStopEvent.Start();

                HideConsole();

                //Console.WriteLine("Press any Key to exit...");
                //Console.ReadKey();
                Application.Run();
            }
            catch (Exception ex)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(ex.Message, EventLogEntryType.Error, 101, 1);
                }
            }
        }

        static void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();

                if (processName.ToLower() == ProgramName.ToLower())
                {
                    using (EventLog eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = "Application";
                        eventLog.WriteEntry($"{ProgramName} started. Repositioning.", EventLogEntryType.Information, 101, 1);
                    }

                    int processID = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);

                    Process process = Process.GetProcessById(processID);

                    IntPtr ptr = process.MainWindowHandle;
                    Rect posRect = new Rect();
                    GetWindowRect(ptr, ref posRect);

                    Screen destinationMonitor;
                    if (!string.IsNullOrEmpty(MonitorName))
                    {
                        destinationMonitor = Screen.AllScreens.Where(f => f.DeviceName.ToLower() == MonitorName.ToLower()).First();
                    }
                    else
                    {
                        destinationMonitor = Screen.AllScreens[MonitorNumber - 1];
                    }

                    if(FullScreen)
                    {
                        MoveWindow(ptr, destinationMonitor.WorkingArea.X, destinationMonitor.WorkingArea.Y, destinationMonitor.WorkingArea.Width, destinationMonitor.WorkingArea.Height, true);
                    }
                    else
                    {
                        MoveWindow(ptr, destinationMonitor.WorkingArea.X, destinationMonitor.WorkingArea.Y, destinationMonitor.WorkingArea.Width, destinationMonitor.WorkingArea.Height, true);
                        MoveWindow(ptr, destinationMonitor.WorkingArea.X, destinationMonitor.WorkingArea.Y, Width, Height, true);
                    }
                    

                    using (EventLog eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = "Application";
                        eventLog.WriteEntry($"Finished repositioning {ProgramName} to monitor {MonitorNumber}.", EventLogEntryType.Information, 101, 1);
                    }
                }
            }
            catch (Exception ex)
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry(ex.Message, EventLogEntryType.Error, 101, 1);
                }

                throw;
            }
            finally
            {
                e.NewEvent.Dispose();
            }
        }

        static void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //Stop code

            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();

            if (processName.ToLower() == ProgramName.ToLower())
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    eventLog.WriteEntry($"{ProgramName} closed.", EventLogEntryType.Information, 101, 1);
                }
            }
        }

        static void HideConsole()
        {
            IntPtr h = GetConsoleWindow();
            if (h != IntPtr.Zero)
            {
                ShowWindow(h, 0);
            }
        }
    }
}
