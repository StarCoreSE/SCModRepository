using Sandbox.ModAPI;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Utils;

namespace Jnick_SCModRepository.StarCoreCTF.Data.Scripts.CTF
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class NetworkDebug : MySessionComponentBase
    {
        const int AveragingPeriod = 60;

        private static NetworkDebug I;
        private int nextLog = 0;
        private Dictionary<string, List<int>> downLoad = new Dictionary<string, List<int>>();
        private Dictionary<string, List<int>> upLoad = new Dictionary<string, List<int>>();

        /// <summary>
        /// Use this method
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] SerializeLogged(string type, object obj)
        {
            byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(obj);
            I.LogUsageUp(type, serialized.Length);
            return serialized;
        }
        private void LogUsageUp(string type, int usage)
        {
            if (!upLoad.ContainsKey(type))
                upLoad.Add(type, new List<int>());
            upLoad[type].Add(usage);
        }

        /// <summary>
        /// Use this method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static T DeserializeLogged<T>(string type, byte[] serialized)
        {
            T obj = MyAPIGateway.Utilities.SerializeFromBinary<T>(serialized);
            I.LogUsageDown(type, serialized.Length);
            return obj;
        }
        private void LogUsageDown(string type, int usage)
        {
            if (!downLoad.ContainsKey(type))
                downLoad.Add(type, new List<int>());
            downLoad[type].Add(usage);
        }

        public override void UpdateAfterSimulation()
        {
            nextLog--;
            if (nextLog <= 0)
            {
                foreach (var upKvp in upLoad)
                {
                    float averageUp = 0;
                    long total = 0;
                    foreach (var size in upKvp.Value)
                    {
                        averageUp += size;
                        total += size;
                    }
                    averageUp /= (AveragingPeriod/60f);
                    MyLog.Default.WriteLineAndConsole($"NetworkDebug: [UP] {upKvp.Key} {Math.Round(averageUp/1000f, 2)}kb/s" + (upKvp.Value.Count > 0 ? $" (avg. {total / upKvp.Value.Count}b), ct. {upKvp.Value.Count / (AveragingPeriod / 60)})" : ""));
                    upKvp.Value.Clear();
                }
                foreach (var downKvp in downLoad)
                {
                    float averageDown = 0;
                    long total = 0;
                    foreach (var size in downKvp.Value)
                    {
                        averageDown += size;
                        total += size;
                    }
                    averageDown /= (AveragingPeriod / 60f);
                    MyLog.Default.WriteLineAndConsole($"NetworkDebug: [DOWN] {downKvp.Key} {Math.Round(averageDown / 1000f, 2)}kb/s" + (downKvp.Value.Count > 0 ? $" (avg. {total / downKvp.Value.Count}b, ct. {downKvp.Value.Count / (AveragingPeriod / 60)})" : ""));
                    downKvp.Value.Clear();
                }
                nextLog = AveragingPeriod;
            }
        }

        public override void BeforeStart()
        {
            I = this;
        }

        protected override void UnloadData()
        {
            I = null;
        }
    }
}
