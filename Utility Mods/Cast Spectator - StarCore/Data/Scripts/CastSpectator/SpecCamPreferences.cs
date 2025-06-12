using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Input;

namespace KlimeDraygoMath.CastSpectator
{
    public class SpecCamPreferences
    {
        Keybind m_ToggleLock = new Keybind(MyKeys.T, false, false, false);
        Keybind m_SwitchMode = new Keybind(MyKeys.R, false, false, false);
        Keybind m_FindAndMove = new Keybind(MyKeys.Z, false, false, false);
        Keybind m_FindAndMoveSpin = new Keybind(MyKeys.Z, false, true, false);

        Keybind m_SwitchToFree = new Keybind(MyKeys.None, false, false, false);
        Keybind m_SwitchToFollow = new Keybind(MyKeys.None, false, false, false);
        Keybind m_SwitchToOrbit = new Keybind(MyKeys.None, false, false, false);
        Keybind m_SwitchToTrack = new Keybind(MyKeys.None, false, false, false);

        Keybind m_SmoothCamera = new Keybind(MyKeys.X, false, false, false);
        Keybind m_ToggleGlobalSmoothing = new Keybind(MyKeys.X, true, false, false);

        Keybind m_PeriodicSwitch = new Keybind(MyKeys.None, false, false, false); // NumPad +

        // Disable CyclePlayerUp and CyclePlayerDown by setting them to MyKeys.None
        Keybind m_CyclePlayerUp = new Keybind(MyKeys.None, false, false, false);
        Keybind m_CyclePlayerDown = new Keybind(MyKeys.None, false, false, false);

        List<VRage.Input.MyKeys> m_SaveTarget = new List<MyKeys>(10)
    { MyKeys.NumPad1, MyKeys.NumPad2, MyKeys.NumPad3, MyKeys.NumPad4, MyKeys.NumPad5, MyKeys.NumPad6, MyKeys.NumPad7, MyKeys.NumPad8, MyKeys.NumPad9, MyKeys.NumPad0 };
        bool m_HideHud = true;

        float m_SmoothLERP = 0.10f;

        private const string FILE = "CastSpecCamPreferences.xml";

        public List<MyKeys> SaveTarget
        {
            get
            {
                return m_SaveTarget;
            }
            set
            {
                m_SaveTarget = value;
                saveXML(this);
            }
        }

        public Keybind ToggleLock
        {
            get
            {
                return m_ToggleLock;
            }
            set
            {
                m_ToggleLock = value;
                saveXML(this);
            }
        }

        public Keybind SwitchMode
        {
            get
            {
                return m_SwitchMode;
            }
            set
            {
                m_SwitchMode = value;
                saveXML(this);
            }
        }

        public Keybind FreeMode
        {
            get
            {
                return m_SwitchToFree;
            }
            set
            {
                m_SwitchToFree = value;
                saveXML(this);
            }
        }

        public Keybind FollowMode
        {
            get
            {
                return m_SwitchToFollow;
            }
            set
            {
                m_SwitchToFollow = value;
                saveXML(this);
            }
        }

        public Keybind OrbitMode
        {
            get
            {
                return m_SwitchToOrbit;
            }
            set
            {
                m_SwitchToOrbit = value;
                saveXML(this);
            }
        }

        public Keybind TrackMode
        {
            get
            {
                return m_SwitchToTrack;
            }
            set
            {
                m_SwitchToTrack = value;
                saveXML(this);
            }
        }

        public Keybind FindAndMove
        {
            get
            {
                return m_FindAndMove;
            }
            set
            {
                m_FindAndMove = value;
                saveXML(this);
            }
        }

        public Keybind FindAndMoveSpin
        {
            get
            {
                return m_FindAndMoveSpin;
            }
            set
            {
                m_FindAndMoveSpin = value;
                saveXML(this);
            }
        }

        public Keybind ToggleSmoothCamera
        {
            get
            {
                return m_SmoothCamera;
            }
            set
            {
                m_SmoothCamera = value;
                saveXML(this);
            }
        }

        public Keybind PeriodicSwitch
        {
            get
            {
                return m_PeriodicSwitch;
            }
            set
            {
                m_PeriodicSwitch = value;
                saveXML(this);
            }
        }

        public Keybind CyclePlayerUp
        {
            get
            {
                return m_CyclePlayerUp;
            }
            set
            {
                m_CyclePlayerUp = value;
                saveXML(this);
            }
        }

        public Keybind CyclePlayerDown
        {
            get
            {
                return m_CyclePlayerDown;
            }
            set
            {
                m_CyclePlayerDown = value;
                saveXML(this);
            }
        }

        public float SmoothCameraLERP
        {
            get
            {
                return m_SmoothLERP;
            }
            set
            {
                m_SmoothLERP = value;
                saveXML(this);
            }
        }
        public bool HideHud
        {
            get
            {
                return m_HideHud;
            }
            set
            {
                m_HideHud = value;
                saveXML(this);
            }
        }

        public static void saveXML(SpecCamPreferences Pref)
        {
            var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(FILE, typeof(SpecCamPreferences));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(Pref));
            writer.Flush();
            writer.Close();
        }
        public static SpecCamPreferences loadXML(bool l_default = false)
        {

            if (l_default)
                return new SpecCamPreferences();
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(FILE, typeof(SpecCamPreferences)))
                {
                    var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(FILE, typeof(SpecCamPreferences));
                    var xmlText = reader.ReadToEnd();
                    reader.Close();
                    return MyAPIGateway.Utilities.SerializeFromXML<SpecCamPreferences>(xmlText);
                }
            }
            catch (Exception ex)
            {

            }

            return new SpecCamPreferences();
        }
    }
}
