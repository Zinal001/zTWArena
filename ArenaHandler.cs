using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace zTWArena
{
    /*
        TODO:
        1. Make events for OnLine and OnPhaseChange
        2. Make an event for OnArenaRunningChange


    */

    class OnLineEventArgs : EventArgs
    {
        public String Date { get; private set; }
        public String File { get; private set; }
        public String Line { get; private set; }

        public OnLineEventArgs(String Date, String File, String Line)
        {
            this.Date = Date;
            this.File = File;
            this.Line = Line;
        }
    }

    class OnPhaseChangeArgs : EventArgs
    {
        public String Date { get; private set; }
        public String File { get; private set; }
        public String LastPhase { get; private set; }
        public String NewPhase { get; private set; }

        public OnPhaseChangeArgs(String Date, String File, String LastPhase, String NewPhase)
        {
            this.Date = Date;
            this.File = File;
            this.LastPhase = LastPhase;
            this.NewPhase = NewPhase;
        }
    }

    class ArenaHandler
    {
        private static readonly UTF8Encoding UTF8WHBom = new UTF8Encoding(false);

        public static ArenaHandler Instance;

        public event EventHandler<OnLineEventArgs> OnLine;

        public event EventHandler<OnPhaseChangeArgs> OnPhaseChange;

        public String CurrentPhase { get; private set; }
        public String LastPhase { get; private set; }

        private Timer updateTimer;

        private FileStream logFS;

        private int logFSIndex = 0;

        private StreamReader logReader;

        private String lastLine = "";
        private bool useLastLine = false;


        public ArenaHandler()
        {
            Instance = this;
            this.LastPhase = "Unknown";
            this.updateTimer = new Timer(250);

            this.updateTimer.AutoReset = true;
            this.updateTimer.Elapsed += UpdateTimer_Elapsed;
        }
        public bool IsArenaRunning()
        {
            Process[] ps = Process.GetProcessesByName("Arena");

            return ps.Length > 0 && ps[0].MainWindowTitle.StartsWith("Total War: Arena");
        }

        public bool Started
        {
            get
            {
                return this.updateTimer.Enabled;
            }
        }

        public void Start()
        {
            String LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"The Creative Assembly\Arena\logs\mp_log.txt");

            if(!File.Exists(LogPath))
                throw new FileNotFoundException("Missing mp_log.txt");

            this.logFS = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            this.logReader = new StreamReader(logFS, UTF8WHBom, false);

            while (!this.logReader.EndOfStream)
                ParseLine(this.logReader.ReadLine());

            this.updateTimer.Start();
        }

        public void Stop()
        {
            this.updateTimer.Stop();
            if(logReader != null)
            {
                this.logReader.Close();
                this.logReader.Dispose();
                this.logReader = null;

            }

            if(this.logFS != null)
            {
                this.logFS.Close();
                this.logFS.Dispose();
                this.logFS = null;
            }
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(!this.IsArenaRunning())
            {
                this.Stop();
                return;
            }

            String line = logReader.ReadLine();

            ParseLine(line);
        }

        private void ParseLine(String line)
        {
            if (!String.IsNullOrEmpty(line))
            {
                if (!line.StartsWith("["))
                {
                    lastLine += line;
                    useLastLine = true;
                }
                else
                {
                    if (useLastLine)
                    {
                        OnLineRead(lastLine);
                        lastLine = "";
                        useLastLine = false;
                    }

                    OnLineRead(line);
                    lastLine = line;
                }
            }
        }

        private void OnLineRead(String line)
        {
            int timeStart = 0;
            int timeEnd = line.IndexOf(']', timeStart);

            if (timeEnd == -1)
                return;

            String timeStr = line.Substring(timeStart, timeEnd - timeStart + 1);

            int fileStart = line.IndexOf('[', timeEnd);
            int fileEnd = line.IndexOf(']', fileStart);

            if (fileEnd == -1)
                return;

            String fileStr = line.Substring(fileStart, fileEnd - fileStart + 1);

            String message = line.Substring(fileEnd + 1);

            if(message.StartsWith(" : "))
                message = message.Remove(0, 3);

            String[] words = message.Split(' ');

            if(message.StartsWith("Setting phase to "))
            {
                //New phase is words[3]. last phase is words[5]
                this.CurrentPhase = words[3];
                this.LastPhase = words[5];

                if (this.OnPhaseChange != null)
                    this.OnPhaseChange(this, new OnPhaseChangeArgs(timeStr, fileStr, this.CurrentPhase, this.LastPhase));
            }

            if (this.OnLine != null)
                this.OnLine(this, new OnLineEventArgs(timeStr, fileStr, message));
        }

    }
}
