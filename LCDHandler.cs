using GammaJul.LgLcd;
using System;
using System.Collections.Generic;
using System.Linq;

namespace zTWArena
{
    class zLCDDevice
    {
        private LcdDevice _Device;

        private String _CurrentPage = null;

        public zLCDPage CurrentPage
        {
            get
            {
                if (_CurrentPage == null || !_Pages.ContainsKey(_CurrentPage))
                    return null;

                return _Pages[_CurrentPage];
            }
            set
            {
                if (_Pages.ContainsValue(value))
                {
                    foreach (KeyValuePair<String, zLCDPage> Pair in _Pages)
                    {
                        if (value == Pair.Value)
                        {
                            _CurrentPage = Pair.Key;
                            _Device.CurrentPage = value._Page;
                            
                            if(this.UpdateOnChange)
                                this.DoUpdateAndDraw();

                            break;
                        }
                    }
                }
                else
                    throw new InvalidOperationException("zLCDDevice does not contain this page");
            }
        }

        private Dictionary<String, zLCDPage> _Pages = new Dictionary<String, zLCDPage>();

        public System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<String, zLCDPage>> Pages
        {
            get
            {
                return _Pages.ToList().AsReadOnly();
            }
        }

        public Boolean UpdateOnChange { get; set; }

        public bool SetAsForegroundApplet
        {
            get
            {
                return this._Device.SetAsForegroundApplet;
            }
            set
            {
                this._Device.SetAsForegroundApplet = value;
            }
        }

        public zLCDDevice(LcdDevice Device)
        {
            this._Device = Device;
            this._Device.SoftButtonsChanged += _Device_SoftButtonsChanged;
            this.UpdateOnChange = true;
        }

        public void ReOpen()
        {
            this._Device.ReOpen();
        }

        public LcdSoftButtons ReadSoftButtons()
        {
            return this._Device.ReadSoftButtons();
        }

        public zLCDPage AddPage(String Name)
        {
            AddPage(Name, new zLCDPage(this._Device));

            return _Pages[Name];
        }

        public void AddPage(String Name, zLCDPage Page)
        {
            if (_Pages.ContainsKey(Name))
                throw new ArgumentException("key " + Name + " already exists");

            Page._zDevice = this;
            _Pages.Add(Name, Page);
        }

        public zLCDPage GetPage(String Name)
        {
            if (!_Pages.ContainsKey(Name))
                return null;

            return _Pages[Name];
        }

        public bool DeletePage(String Name)
        {
            return _Pages.Remove(Name);
        }

        public void DoUpdateAndDraw()
        {
            this._Device.DoUpdateAndDraw();
        }

        private void _Device_SoftButtonsChanged(object sender, LcdSoftButtonsEventArgs e)
        {
            if(_CurrentPage != null && _Pages.ContainsKey(_CurrentPage))
                _Pages[_CurrentPage].OnLcdButtonsChanged(e.SoftButtons, this);
        }
    }

    class zLCDPage
    {
        internal LcdGdiPage _Page;

        internal zLCDDevice _zDevice;

        private Dictionary<String, LcdGdiObject> _Children = new Dictionary<String, LcdGdiObject>();

        public event EventHandler<zLcdSoftButtonsEventArgs> LcdButtonsChanged;

        public zLCDPage(LcdDevice Device) : this(new LcdGdiPage(Device))
        {

        }

        public zLCDPage(LcdGdiPage Page)
        {
            this._Page = Page;
        }

        public void SetAsCurrentDevicePage()
        {
            this._zDevice.CurrentPage = this;
        }

        public T Get<T>(String Name) where T : LcdGdiObject
        {
            if (!_Children.ContainsKey(Name))
                return default(T);
            else
                return (T)_Children[Name];
        }

        public void Add(String Name, LcdGdiObject obj)
        {
            if (_Children.ContainsKey(Name))
                throw new ArgumentException("Key " + Name + " already exists");

            obj.Changed += Obj_Changed;
            this._Page.Children.Add(obj);

            _Children.Add(Name, obj);
        }

        private void Obj_Changed(object sender, EventArgs e)
        {
            if (this._zDevice.UpdateOnChange && this._zDevice.CurrentPage == this)
                this._zDevice.DoUpdateAndDraw();
        }

        public bool Remove(String Name)
        {
            if (!_Children.ContainsKey(Name))
                return false;

            this._Page.Children.Remove(_Children[Name]);

            _Children[Name].Changed -= Obj_Changed;
            return _Children.Remove(Name);
        }

        internal void OnLcdButtonsChanged(LcdSoftButtons Buttons, zLCDDevice Device)
        {
            if (this.LcdButtonsChanged != null)
                this.LcdButtonsChanged(this, new zLcdSoftButtonsEventArgs(this, Device, Buttons));
        }
    }

    class zLcdSoftButtonsEventArgs : EventArgs
    {
        public zLCDPage Page { get; private set; }
        public zLCDDevice Device { get; private set; }
        public LcdSoftButtons Buttons { get; private set; }

        public zLcdSoftButtonsEventArgs(zLCDPage Page, zLCDDevice Device, LcdSoftButtons Buttons)
        {
            this.Page = Page;
            this.Device = Device;
            this.Buttons = Buttons;
        }

    }

    enum LCDPages
    {
        Splash,
        Matchmaking,
        Loading,
        Battle
    }
}
