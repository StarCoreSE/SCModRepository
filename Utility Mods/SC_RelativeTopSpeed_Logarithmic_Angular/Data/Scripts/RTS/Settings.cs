using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.IO;
using System.Xml.Serialization;
using VRage.Collections;
using VRage.Game;
using VRage.Utils;

namespace RelativeTopSpeedGV
{
    [ProtoContract]
    public class Settings
    {
        public static Settings Instance;
        public static bool Debug = false;

        public const string Filename = "RelativeTopSpeed.cfg";

        public static readonly Settings Default = new Settings() {
            EnableBoosting = true,
            IgnoreGridsWithoutThrust = false,
            IgnoreGridsWithoutCockpit = false,
            ParachuteDeployHeight = 400,
            SpeedLimit = 400,
            RemoteControlSpeedLimit = 350,
			LargeGrid_MinCruise = 60,
            LargeGrid_MidCruise = 160,
			LargeGrid_MaxCruise = 300,
			LargeGrid_MaxBoostSpeed = 400,
			LargeGrid_ResistanceMultiplier = 1.5f,
			LargeGrid_MinMass = 2000000,
            LargeGrid_MidMass = 10000000,
			LargeGrid_MaxMass = 16000000,
			SmallGrid_MinCruise = 60,
            SmallGrid_MidCruise = 160,
			SmallGrid_MaxCruise = 300,
			SmallGrid_MaxBoostSpeed = 400,
			SmallGrid_ResistanceMultiplyer = 1f,
			SmallGrid_MinMass = 10000,
            SmallGrid_MidMass = 300000,
			SmallGrid_MaxMass = 400000,
			//SmallGrid_Cruise_a = -12.5f,
			//SmallGrid_Cruise_b = 5f,
			//SmallGrid_Cruise_c = 0,
			//SmallGrid_Cruise_d = 204,
			//SmallGrid_Boost_a = -16.3f,
			//SmallGrid_Boost_b = 6,
			//SmallGrid_Boost_c = 0,
			//SmallGrid_Boost_d = 233,
			//LargeGrid_Cruise_a = -4.3f,
			//LargeGrid_Cruise_b = 1.4f,
			//LargeGrid_Cruise_c = 614000,
			//LargeGrid_Cruise_d = 270,
			//LargeGrid_Boost_a = -34f,
			//LargeGrid_Boost_b = 17f,
			//LargeGrid_Boost_c = 65000,
			//LargeGrid_Boost_d = 263,
            EnableAngularLimits = false,
			SmallGrid_AngularMassMult = 0.1f,
			SmallGrid_AngularCruiseMult = 0.25f,
			LargeGrid_AngularMassMult = 0.1f,
			LargeGrid_AngularCruiseMult = 0.25f,
            GlobalMinAngularSpeed = 0.5f,
            MaxAngularSpeed = 5f,
            MinAngularSpeed = 0.0001f
        };

        [ProtoMember(1)]
        public bool EnableBoosting { get; set; }

        [ProtoMember(2)]
        public bool IgnoreGridsWithoutThrust { get; set; }

        [ProtoMember(3)]
        public bool IgnoreGridsWithoutCockpit { get; set; }

        [ProtoMember(4)]
        public float ParachuteDeployHeight { get; set; }

		[ProtoMember(5)]
        public float SpeedLimit { get; set; }

        [ProtoMember(6)]
        public float RemoteControlSpeedLimit { get; set; }

        [ProtoMember(7)]
        public float LargeGrid_MinCruise { get; set; }

        [ProtoMember(8)]
        public float LargeGrid_MidCruise { get; set; }

        [ProtoMember(9)]
        public float LargeGrid_MaxCruise { get; set; }

        [ProtoMember(10)]
        public float LargeGrid_MinMass { get; set; }

        [ProtoMember(11)]
        public float LargeGrid_MidMass { get; set; }

        [ProtoMember(12)]
        public float LargeGrid_MaxMass { get; set; }

        [ProtoMember(13)]
        public float LargeGrid_MaxBoostSpeed { get; set; }

        [ProtoMember(14)]
        public float LargeGrid_ResistanceMultiplier { get; set; }

        [ProtoMember(15)]
        public float SmallGrid_MinCruise { get; set; }

        [ProtoMember(16)]
        public float SmallGrid_MidCruise { get; set; }

        [ProtoMember(17)]
        public float SmallGrid_MaxCruise { get; set; }

        [ProtoMember(18)]
        public float SmallGrid_MinMass { get; set; }

        [ProtoMember(19)]
        public float SmallGrid_MidMass { get; set; }

        [ProtoMember(20)]
        public float SmallGrid_MaxMass { get; set; }

        [ProtoMember(21)]
        public float SmallGrid_MaxBoostSpeed { get; set; }

        [ProtoMember(22)]
        public float SmallGrid_ResistanceMultiplyer { get; set; }

        //In order to emulate the previous RTS setup with any accuracy I need to get rid of the logarithmic decay. Will explore later
        //[ProtoMember(23)]
        //public float SmallGrid_Cruise_a { get; set; }
        //
        //[ProtoMember(24)]
        //public float SmallGrid_Cruise_b { get; set; }
        //
        //[ProtoMember(25)]
        //public float SmallGrid_Cruise_c { get; set; }
        //
        //[ProtoMember(26)]
        //public float SmallGrid_Cruise_d { get; set; }
        //
        //[ProtoMember(27)]
        //public float SmallGrid_Boost_a { get; set; }
        //
        //[ProtoMember(28)]
        //public float SmallGrid_Boost_b { get; set; }
        //
        //[ProtoMember(29)]
        //public float SmallGrid_Boost_c { get; set; }
        //
        //[ProtoMember(30)]
        //public float SmallGrid_Boost_d { get; set; }
        //
        //[ProtoMember(31)]
        //public float LargeGrid_Cruise_a { get; set; }
        //
        //[ProtoMember(32)]
        //public float LargeGrid_Cruise_b { get; set; }
        //
        //[ProtoMember(33)]
        //public float LargeGrid_Cruise_c { get; set; }
        //
        //[ProtoMember(34)]
        //public float LargeGrid_Cruise_d { get; set; }
        //
        //[ProtoMember(35)]
        //public float LargeGrid_Boost_a { get; set; }
        //
        //[ProtoMember(36)]
        //public float LargeGrid_Boost_b { get; set; }
        //
        //[ProtoMember(37)]
        //public float LargeGrid_Boost_c { get; set; }
        //
        //[ProtoMember(38)]
        //public float LargeGrid_Boost_d { get; set; }

        [ProtoMember(39)]
        public bool EnableAngularLimits { get; set; }

        [ProtoMember(40)]
        public float SmallGrid_AngularMassMult { get; set; }

        [ProtoMember(41)]
        public float SmallGrid_AngularCruiseMult { get; set; }

        [ProtoMember(42)]
        public float LargeGrid_AngularMassMult { get; set; }

        [ProtoMember(43)]
        public float LargeGrid_AngularCruiseMult { get; set; }

        [ProtoMember(44)]
        public float GlobalMinAngularSpeed { get; set; }

        [ProtoMember(45)]
        public float MaxAngularSpeed { get; set; }

        [ProtoMember(46)]
        public float MinAngularSpeed { get; set; }

        // flipped the min and max cruise
        public float GetCruiseSpeed(float mass, float minMass, float midMass, float maxMass, float maxCruise, float midCruise, float minCruise) 
        {
            if (mass > maxMass)
            {
                return maxCruise;
            }

            if (mass < minMass)
            {
                return minCruise;
            }

            bool lessThanMid = (mass < midMass);

            double speed0;
            double speed1;
            double deltaX;
            double deltaY;
            double x;
            double slopeRatio = 1;

            if (lessThanMid)
            {
                speed0 = minCruise;
                speed1 = midCruise;
                deltaX = midMass - minMass;
                deltaY = midCruise - minCruise;
                x = (mass - minMass) / deltaX;
            }
            else
            {
                speed0 = midCruise;
                speed1 = maxCruise;
                deltaX = maxMass - midMass;
                deltaY = maxCruise - midCruise;
                x = (mass - midMass) / deltaX;
                slopeRatio = deltaX / (midMass - minMass);
            }

            double slope0 = deltaY * slopeRatio;
            double slope1 = slope0;

            double specialSlope = (maxCruise - minCruise) * slopeRatio * 0.2f;

            if (lessThanMid)
            {
                slope1 = specialSlope;
            }
            else
            {
                slope0 = specialSlope;
            }

            float interp = (float)CubicInterpolation(x, speed0, slope0, speed1, slope1);

            // dont flip the signs THEY ARE CORRECT
			if (interp > minCruise)
			{
				interp = minCruise;
			}
			if (interp < maxCruise)
			{
				interp = maxCruise;
			}

			return interp; //minCruise + (maxCruise - interp);
        }

        public float GetCruiseSpeed(float mass, bool isLargeGrid)
        {
            if (isLargeGrid)
            {
                return GetCruiseSpeed(mass, LargeGrid_MinMass, LargeGrid_MidMass, LargeGrid_MaxMass, LargeGrid_MinCruise, LargeGrid_MidCruise, LargeGrid_MaxCruise);
            }

            return GetCruiseSpeed(mass, SmallGrid_MinMass, SmallGrid_MidMass, SmallGrid_MaxMass, SmallGrid_MinCruise, SmallGrid_MidCruise, SmallGrid_MaxCruise);
        }

        public float GetBoostSpeed(float mass, bool isLargeGrid)
        {
            float cruiseSpeed = GetCruiseSpeed(mass, isLargeGrid);
            float maxBoostSpeed = isLargeGrid ? LargeGrid_MaxBoostSpeed : SmallGrid_MaxBoostSpeed;

            float boostSpeed = cruiseSpeed + maxBoostSpeed;

            if (boostSpeed > SpeedLimit)
            {
                boostSpeed = SpeedLimit;
            }

            return boostSpeed;
        }

        public static double CubicInterpolation(double x, double y0, double m0, double y1, double m1)
        {
            /*
             * This implements a cubic hermite spline interpolator
             * It interpolates between two points and the first derivatives at those two points
             * 
             * x must be in the domain [0,1]
             * m0 and m1 are first derivatives at points (0, y0) and (1, y1)
             * 
             * Returns the interpolated y value (speed) between y0 and y1 given x
             */
            double x2 = x * x;
            double x3 = x * x * x;
            return (2 * x3 - 3 * x2 + 1) * y0 +
                    (x3 - 2 * x2 + x) * m0 +
                    (-2 * x3 + 3 * x2) * y1 +
                    (x3 - x2) * m1;
        }

        public static double LogDecay(double x, double a, double b, double c, double d)
        {
            /*
			 * This implements a logarithmic decay curve that allows for very fast lightweight crafts
			 * but flattens out as mass increases
			 *
             * Speed = a*Log(mass+c,b)+d
             */
            return a * Math.Log(x + c, b) + d;
        }


        public void CalculateCurve()
        {
            MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed = SpeedLimit;
            MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed = SpeedLimit;


            // parachute deploy hight code is taken directly from midspaces configurable speed mod. All credit goes to them. 
            DictionaryReader<string, MyDropContainerDefinition> dropContainers = MyDefinitionManager.Static.GetDropContainerDefinitions();
            foreach (var kvp in dropContainers)
            {
                foreach (MyObjectBuilder_CubeGrid grid in kvp.Value.Prefab.CubeGrids)
                {
                    foreach (MyObjectBuilder_CubeBlock block in grid.CubeBlocks)
                    {
                        MyObjectBuilder_Parachute chute = block as MyObjectBuilder_Parachute;
                        if (chute != null)
                        {
                            chute.DeployHeight = ParachuteDeployHeight;
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return MyAPIGateway.Utilities.SerializeToXML(this);
        }

        public static void Validate(ref Settings s)
        {
            if (s.SpeedLimit <= 0)
            {
                s.SpeedLimit = 100;
            }

            if (s.RemoteControlSpeedLimit <= 0)
            {
                s.RemoteControlSpeedLimit = 100;
            }
            else if (s.RemoteControlSpeedLimit > s.SpeedLimit)
            {
                s.RemoteControlSpeedLimit = s.SpeedLimit;
            }

            if (s.ParachuteDeployHeight < 0)
            {
                s.ParachuteDeployHeight = 0;
            }

            #region Large Grid Validation

            if (s.LargeGrid_MinCruise < 0.01f)
            {
                s.LargeGrid_MinCruise = 0.01f;
            }
            else if (s.LargeGrid_MinCruise > s.SpeedLimit)
            {
                s.LargeGrid_MinCruise = s.SpeedLimit;
            }

            if (s.LargeGrid_MaxCruise < s.LargeGrid_MinCruise)
            {
                s.LargeGrid_MaxCruise = s.LargeGrid_MinCruise;
            }
            else if (s.LargeGrid_MaxCruise > s.SpeedLimit)
            {
                s.LargeGrid_MaxCruise = s.SpeedLimit;
            }

            if (s.LargeGrid_MidCruise < s.LargeGrid_MinCruise)
            {
                s.LargeGrid_MidCruise = s.LargeGrid_MinCruise;
            }
            if (s.LargeGrid_MidCruise > s.LargeGrid_MaxCruise)
            {
                s.LargeGrid_MidCruise = s.LargeGrid_MaxCruise;
            }

            if (s.LargeGrid_MaxBoostSpeed < s.LargeGrid_MaxCruise)
            {
                s.LargeGrid_MaxBoostSpeed = s.LargeGrid_MaxCruise;
            }
            else if (s.LargeGrid_MaxBoostSpeed > s.SpeedLimit)
            {
                s.LargeGrid_MaxBoostSpeed = s.SpeedLimit;
            }

            if (s.LargeGrid_ResistanceMultiplier <= 0)
            {
                s.LargeGrid_ResistanceMultiplier = 1f;
            }

            if (s.LargeGrid_MinMass < 0)
            {
                s.LargeGrid_MinMass = 0;
            }

            if (s.LargeGrid_MaxMass < s.LargeGrid_MinMass)
            {
                s.LargeGrid_MaxMass = s.LargeGrid_MinMass;
            }

            if (s.LargeGrid_MidMass < s.LargeGrid_MinMass)
            {
                s.LargeGrid_MidMass = s.LargeGrid_MinMass;
            }
            else if (s.LargeGrid_MidMass > s.LargeGrid_MaxMass)
            {
                s.LargeGrid_MidMass = s.LargeGrid_MaxMass;
            }
            if (s.MaxAngularSpeed < s.MinAngularSpeed)
            {
                s.MaxAngularSpeed = s.MinAngularSpeed;
            }

            if (s.MinAngularSpeed < 0)
            {
                s.MinAngularSpeed = 0;
            }

            #endregion

            #region Small Grid Validation

            if (s.SmallGrid_MinCruise < 0.01)
            {
                s.SmallGrid_MinCruise = 0.01f;
            }
            else if (s.SmallGrid_MinCruise > s.SpeedLimit)
            {
                s.SmallGrid_MinCruise = s.SpeedLimit;
            }

            if (s.SmallGrid_MaxCruise < s.SmallGrid_MinCruise)
            {
                s.SmallGrid_MaxCruise = s.SmallGrid_MinCruise;
            }
            else if (s.SmallGrid_MaxCruise > s.SpeedLimit)
            {
                s.SmallGrid_MaxCruise = s.SpeedLimit;
            }

            if (s.SmallGrid_MidCruise < s.SmallGrid_MinCruise)
            {
                s.SmallGrid_MidCruise = s.SmallGrid_MinCruise;
            }
            else if (s.SmallGrid_MidCruise > s.SmallGrid_MaxCruise)
            {
                s.SmallGrid_MidCruise = s.SmallGrid_MaxCruise;
            }

            if (s.SmallGrid_MaxBoostSpeed < s.SmallGrid_MaxCruise)
            {
                s.SmallGrid_MaxBoostSpeed = s.SmallGrid_MaxCruise;
            }
            else if (s.SmallGrid_MaxBoostSpeed > s.SpeedLimit)
            {
                s.SmallGrid_MaxBoostSpeed = s.SpeedLimit;
            }

            if (s.SmallGrid_ResistanceMultiplyer <= 0)
            {
                s.SmallGrid_ResistanceMultiplyer = 1f;
            }

            if (s.SmallGrid_MinMass < 0)
            {
                s.SmallGrid_MinMass = 0;
            }

            if (s.SmallGrid_MaxMass < s.SmallGrid_MinMass)
            {
                s.SmallGrid_MaxMass = s.SmallGrid_MinMass;
            }

            if (s.SmallGrid_MidMass < s.SmallGrid_MinMass)
            {
                s.SmallGrid_MidMass = s.SmallGrid_MinMass;
            }
            else if (s.SmallGrid_MidMass > s.SmallGrid_MaxMass)
            {
                s.SmallGrid_MidMass = s.SmallGrid_MaxMass;
            }

            #endregion
        }

        public static Settings Load()
        {
			if (!MyAPIGateway.Multiplayer.IsServer)
			{
				return Default;
			}

            Settings s = null;
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage(Filename, typeof(Settings)))
                {
                    MyLog.Default.Info("[RelativeTopSpeed] Loading settings from world storage");
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(Filename, typeof(Settings));
                    string text = reader.ReadToEnd();
                    reader.Close();

                    s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);
                    Validate(ref s);
                    Save(s);
                }
                else
                {
                    MyLog.Default.Info("[RelativeTopSpeed] Config file not found. Loading from local storage");
					if (MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, typeof(Settings)))
					{
						MyLog.Default.Info("[RelativeTopSpeed] Loading settings from local storage");
						TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Filename, typeof(Settings));
						string text = reader.ReadToEnd();
						reader.Close();

						s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);
						Validate(ref s);
						Save(s);
					}
					else
					{
						MyLog.Default.Info("[RelativeTopSpeed] Config file not found. Loading defaults");
						s = Default;
						Save(s);
					}
                }
            }
            catch (Exception e)
            {
                MyLog.Default.Warning($"[RelativeTopSpeed] Failed to load saved configuration. Loading defaults\n {e.ToString()}");
                s = Default;
                Save(s);
            }

            MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed = s.SpeedLimit;
            MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed = s.SpeedLimit;
            s.CalculateCurve();
            return s;
        }

        public static void Save(Settings settings)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                try
                {
					MyLog.Default.Info("[RelativeTopSpeed] Saving Settings");
					TextWriter writer;
					if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, typeof(Settings)))
					{
						writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Filename, typeof(Settings));
						writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
						writer.Close();
					}

					writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(Filename, typeof(Settings));
					writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
					writer.Close();

                }
                catch (Exception e)
                {
                    MyLog.Default.Error($"[RelativeTopSpeed] Failed to save settings\n{e.ToString()}");
                }
            }
        }
    }
}
