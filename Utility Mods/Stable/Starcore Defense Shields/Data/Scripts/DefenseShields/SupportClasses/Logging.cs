using System;
using System.Globalization;
using System.IO;
using Sandbox.ModAPI;

namespace DefenseShields.Support
{
    public class Log
    {
        private static Log _instance = null;
        private TextWriter _file = null;
        private string _fileName = "";

        private Log()
        {
        }

        private static Log GetInstance()
        {
            if (Log._instance == null)
            {
                Log._instance = new Log();
            }

            return _instance;
        }

        public static bool Init(string name)
        {

            bool output = false;

            if (GetInstance()._file == null)
            {

                try
                {
                    //MyAPIGateway.Utilities.ShowNotification(name, 5000);
                    GetInstance()._fileName = name;
                    GetInstance()._file = MyAPIGateway.Utilities.WriteFileInLocalStorage(name, typeof(Log));
                    output = true;
                }
                catch (Exception e)
                {
                    MyAPIGateway.Utilities.ShowNotification(e.Message, 5000);
                }
            }
            else
            {
                output = true;
            }

            return output;
        }

        public static void Line(string text)
        {
            try
            {
                if (GetInstance()._file != null)
                {
                    var time = $"{DateTime.Now:MM-dd-yy_HH-mm-ss-fff} - ";
                    GetInstance()._file.WriteLine(time + text);
                    GetInstance()._file.Flush();
                }
            }
            catch (Exception e)
            {
            }
        }

        public static void Chars(string text)
        {
            try
            {
                if (GetInstance()._file != null)
                {
                    GetInstance()._file.Write(text);
                    GetInstance()._file.Flush();
                }
            }
            catch (Exception e)
            {
            }
        }

        public static void CleanLine(string text)
        {
            try
            {
                if (GetInstance()._file != null)
                {
                    GetInstance()._file.WriteLine(text);
                    GetInstance()._file.Flush();
                }
            }
            catch (Exception e)
            {
            }
        }

        public static void Close()
        {
            try
            {
                if (GetInstance()._file != null)
                {
                    GetInstance()._file.Flush();
                    GetInstance()._file.Close();
                }
            }
            catch (Exception e)
            {
            }
        }
    }
}

