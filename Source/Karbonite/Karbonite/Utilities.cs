using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Karbonite
{
    class Utilities
    {
        const int SECONDS_PER_MINUTE = 60;
        const int SECONDS_PER_HOUR = 3600;
        const int SECONDS_PER_DAY = 6 * SECONDS_PER_HOUR;

        public static int MaxDeltaTime
        {
            get { return SECONDS_PER_DAY; }
        }
        public static int ElectricityMaxDeltaTime
        {
            get { return 1; }
        }

        public static string Electricity { get { return "ElectricCharge"; } }

        public static int ElectricityId
        {
            get
            {
                return PartResourceLibrary.Instance.GetDefinition(Electricity).id;
            }
        }

        public static double GetValue(ConfigNode config, string name, double currentValue)
        {
            double newValue;
            if (config.HasValue(name) && double.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }


        public static string FormatTime(double time)
        {
            time = (int)time;

            string result = "";
            if (time < 0)
            {
                result += "-";
                time = -time;
            }

            int days = (int)(time / SECONDS_PER_DAY);
            time -= days * SECONDS_PER_DAY;

            int hours = (int)(time / SECONDS_PER_HOUR);
            time -= hours * SECONDS_PER_HOUR;

            int minutes = (int)(time / SECONDS_PER_MINUTE);
            time -= minutes * SECONDS_PER_MINUTE;

            int seconds = (int)time;

            if (days > 0)
            {
                result += days.ToString("#0") + ":";
            }
            result += hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
            return result;
        }
    }
}
