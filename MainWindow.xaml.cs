using GammaJul.LgLcd;
using System;
using System.Drawing;
using System.Timers;
using System.Windows;

namespace zTWArena
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ArenaHandler ah = new ArenaHandler();

        bool InMatchmaking = false;

        bool InBattle = false;

        String CurrentMap = "";

        int PlayersLoaded = 0;

        #region LCD
        LcdApplet _Applet;

        zLCDDevice _Device = null;
        #endregion

        Timer runningTimer = new Timer(5000);

        public MainWindow()
        {
            InitializeComponent();

            _Applet = new LcdApplet("zTWArena", LcdAppletCapabilities.Monochrome, false);
            _Applet.DeviceArrival += _Applet_DeviceArrival;

            _Applet.Connect();

            ah.OnLine += Ah_OnLine;

            runningTimer.AutoReset = true;
            runningTimer.Elapsed += RunningTimer_Elapsed;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void RunningTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(ah.IsArenaRunning() && !ah.Started)
                ah.Start();
        }

        private void _Applet_DeviceArrival(object sender, LcdDeviceTypeEventArgs e)
        {
            if(_Device == null)
            {
                LcdDevice device = _Applet.OpenDeviceByType(e.DeviceType);
                _Device = new zLCDDevice(device);
                SetupDevice();
                _Device.SetAsForegroundApplet = true;

                runningTimer.Start();
            }
        }

        private void SetupDevice()
        {
            _Device.UpdateOnChange = false;
            zLCDPage Splash = _Device.AddPage("Splash");

            Splash.Add("BG", new LcdGdiImage(Properties.Resources.zTWArenaBG));


            zLCDPage Matchmaking = _Device.AddPage("Matchmaking");
            
            Matchmaking.Add("Title", new LcdGdiText());
            Matchmaking.Get<LcdGdiText>("Title").HorizontalAlignment = LcdGdiHorizontalAlignment.Left;
            Matchmaking.Get<LcdGdiText>("Title").VerticalAlignment = LcdGdiVerticalAlignment.Top;
            Matchmaking.Get<LcdGdiText>("Title").Text = "Matchmaking";

            Matchmaking.Add("Players", new LcdGdiText());
            Matchmaking.Get<LcdGdiText>("Players").HorizontalAlignment = LcdGdiHorizontalAlignment.Right;
            Matchmaking.Get<LcdGdiText>("Players").VerticalAlignment = LcdGdiVerticalAlignment.Top;
            Matchmaking.Get<LcdGdiText>("Players").Text = "Players in Queue: 0";

            Matchmaking.Add("ProgressBar", new LcdGdiProgressBar());
            Matchmaking.Get<LcdGdiProgressBar>("ProgressBar").HorizontalAlignment = LcdGdiHorizontalAlignment.Center;
            Matchmaking.Get<LcdGdiProgressBar>("ProgressBar").VerticalAlignment = LcdGdiVerticalAlignment.Middle;
            Matchmaking.Get<LcdGdiProgressBar>("ProgressBar").Size = new SizeF(140, 16);
            Matchmaking.Get<LcdGdiProgressBar>("ProgressBar").Value = 0;
            Matchmaking.Get<LcdGdiProgressBar>("ProgressBar").Minimum = 0;
            Matchmaking.Get<LcdGdiProgressBar>("ProgressBar").Maximum = 100;

            Matchmaking.Add("Procent", new LcdGdiText());
            Matchmaking.Get<LcdGdiText>("Procent").HorizontalAlignment = LcdGdiHorizontalAlignment.Center;
            Matchmaking.Get<LcdGdiText>("Procent").VerticalAlignment = LcdGdiVerticalAlignment.Bottom;
            Matchmaking.Get<LcdGdiText>("Procent").Text = "0%";


            zLCDPage Loading = _Device.AddPage("Loading");

            Loading.Add("Title", new LcdGdiText());
            Loading.Get<LcdGdiText>("Title").HorizontalAlignment = LcdGdiHorizontalAlignment.Left;
            Loading.Get<LcdGdiText>("Title").VerticalAlignment = LcdGdiVerticalAlignment.Top;
            Loading.Get<LcdGdiText>("Title").Text = "Loading";

            Loading.Add("ProgressBar", new LcdGdiProgressBar());
            Loading.Get<LcdGdiProgressBar>("ProgressBar").HorizontalAlignment = LcdGdiHorizontalAlignment.Center;
            Loading.Get<LcdGdiProgressBar>("ProgressBar").VerticalAlignment = LcdGdiVerticalAlignment.Middle;
            Loading.Get<LcdGdiProgressBar>("ProgressBar").Size = new SizeF(140, 16);
            Loading.Get<LcdGdiProgressBar>("ProgressBar").Value = 0;
            Loading.Get<LcdGdiProgressBar>("ProgressBar").Minimum = 0;
            Loading.Get<LcdGdiProgressBar>("ProgressBar").Maximum = 20;

            Loading.Add("Players", new LcdGdiText());
            Loading.Get<LcdGdiText>("Players").HorizontalAlignment = LcdGdiHorizontalAlignment.Right;
            Loading.Get<LcdGdiText>("Players").VerticalAlignment = LcdGdiVerticalAlignment.Bottom;
            Loading.Get<LcdGdiText>("Players").Text = "Players Loaded: 0/20";

            Loading.Add("Map", new LcdGdiText());
            Loading.Get<LcdGdiText>("Map").HorizontalAlignment = LcdGdiHorizontalAlignment.Left;
            Loading.Get<LcdGdiText>("Map").VerticalAlignment = LcdGdiVerticalAlignment.Bottom;
            Loading.Get<LcdGdiText>("Map").Text = "";


            zLCDPage InBattle = _Device.AddPage("InBattle");

            InBattle.Add("Text", new LcdGdiText());
            InBattle.Get<LcdGdiText>("Text").HorizontalAlignment = LcdGdiHorizontalAlignment.Center;
            InBattle.Get<LcdGdiText>("Text").VerticalAlignment = LcdGdiVerticalAlignment.Middle;
            InBattle.Get<LcdGdiText>("Text").Text = "In Battle";

            _Device.UpdateOnChange = true;
            _Device.CurrentPage = Splash;
            _Device.DoUpdateAndDraw();
        }

        private void Ah_OnLine(object sender, OnLineEventArgs e)
        {
            if (e.Line.StartsWith("matchmaking version") && ah.CurrentPhase == "MANAGER_PHASE_MATCHMAKE")
            {
                this.InMatchmaking = true;

                _Device.GetPage("Matchmaking").Get<LcdGdiText>("Players").Text = "Players in Queue: 0";
                _Device.GetPage("Matchmaking").Get<LcdGdiText>("Procent").Text = "0%";
                _Device.GetPage("Matchmaking").Get<LcdGdiProgressBar>("ProgressBar").Value = 0;
                _Device.GetPage("Matchmaking").SetAsCurrentDevicePage();

                Console.WriteLine("Entering Matchmaking");
            }
            else if (e.Line.StartsWith("Resetting SSMatchmakingClient to state: CLIENT_STATUS_IDLE"))
            {
                this.InMatchmaking = false;

                _Device.GetPage("Splash").SetAsCurrentDevicePage();

                Console.WriteLine("Exiting Matchmaking");
            }
            else if (e.Line.StartsWith("leaving game"))
            {
                this.InBattle = false;
                this.PlayersLoaded = 0;

                Console.WriteLine("Exiting Battle");

                _Device.GetPage("Splash").SetAsCurrentDevicePage();
            }
            else if (e.Line.StartsWith("EMPIRE_MP_BATTLE_SETUP_INFO dbkey"))
            {
                String[] words = e.Line.Split(' ');

                this.CurrentMap = words[2].Substring(0, words[2].Length - 1);

                this.PlayersLoaded = 1;

                _Device.GetPage("Loading").Get<LcdGdiProgressBar>("ProgressBar").Value = 0;
                _Device.GetPage("Loading").Get<LcdGdiText>("Players").Text = "Players Loaded: 1/20";
                _Device.GetPage("Loading").Get<LcdGdiText>("Map").Text = Capitalize(this.CurrentMap);
                _Device.GetPage("Loading").SetAsCurrentDevicePage();

                Console.WriteLine("Changed map to: " + this.CurrentMap);
            }
            else if (e.Line.StartsWith("received SERVER_MESSAGE_PLAYER_FINISHED_LOADING about") && this.InBattle)
            {
                this.PlayersLoaded++;

                _Device.GetPage("Loading").Get<LcdGdiProgressBar>("ProgressBar").Value = this.PlayersLoaded;
                _Device.GetPage("Loading").Get<LcdGdiText>("Players").Text = "Players Loaded: " + this.PlayersLoaded + "/20";

                Console.WriteLine("Players loaded: " + this.PlayersLoaded);
            }
            else if(e.Line.StartsWith("notified of EMPIREBATTLE::REPORT_ON_ENTER_BATTLE"))
            {
                Console.WriteLine("Battle Commencing!");

                _Device.GetPage("InBattle").SetAsCurrentDevicePage();
            }

            if (ah.CurrentPhase == "MANAGER_PHASE_WAIT_FOR_CHECK_RESPONSE")
            {
                if(e.Line.StartsWith("MM check - still waiting; queue size: "))
                {
                    String[] words = e.Line.Split(' ');

                    double x, y;
                    if(Double.TryParse(words[7].Replace(",", ""), out x))
                    {
                        if(Double.TryParse(words[9], out y))
                        {
                            double r = (x / y) * 100;
                            int ri = (int)Math.Floor(r);
                            Console.WriteLine("Matchmaking: " + ri + "% Players in queue: " + words[7].Replace(",", ""));

                            _Device.UpdateOnChange = false;
                            _Device.GetPage("Matchmaking").Get<LcdGdiProgressBar>("ProgressBar").Value = ri;
                            _Device.GetPage("Matchmaking").Get<LcdGdiText>("Players").Text = "Players in Queue: " + words[7].Replace(",", "");
                            _Device.GetPage("Matchmaking").Get<LcdGdiText>("Procent").Text = ri + "%";
                            _Device.UpdateOnChange = true;
                            _Device.DoUpdateAndDraw();
                            return;
                        }
                    }
                    
                    Console.WriteLine("Matchmaking failed?! - " + words[7] + " " + words[9] + " - " + e.Line);
                }
            }
            else if(ah.CurrentPhase == "MANAGER_PHASE_BATTLE_READY" && ah.LastPhase != "MANAGER_PHASE_BATTLE_READY" && this.InMatchmaking)
            {
                this.InMatchmaking = false;
                this.InBattle = true;

                _Device.GetPage("Loading").Get<LcdGdiProgressBar>("ProgressBar").Value = 0;
                _Device.GetPage("Loading").Get<LcdGdiText>("Players").Text = "Players Loaded: 0/20";

                Console.WriteLine("Exiting Matchmaking, Entering Battle");
            }
        }

        private String Capitalize(String str)
        {
            String ret = str[0] + "";
            ret = ret.ToUpper();
            ret += str.Substring(1);

            return ret;
        }

    }
}
