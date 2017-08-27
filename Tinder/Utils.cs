using System;

namespace Tinder.Pages
{
    internal class Utils
    {
        public static string GetConnectionString()
        {
            return @"Data Source=(LocalDB)\MSSQLLocalDB;
                    AttachDbFilename=C:\Users\Michal\documents\visual studio 2017\Projects\Tinder\Tinder\TinderClientDatabase.mdf;
                    Integrated Security=True";
        }

        public static long UnixTimestampFromDateTime(DateTime date)
        {
            var unixTimestamp = date.Ticks - new DateTime(1970, 1, 1).Ticks;
            unixTimestamp /= TimeSpan.TicksPerSecond;
            return unixTimestamp;
        }

        private static DateTime TimeFromUnixTimestamp(int unixTimestamp)
        {
            var unixYear0 = new DateTime(1970, 1, 1);
            var unixTimeStampInTicks = unixTimestamp * TimeSpan.TicksPerSecond;
            var dtUnix = new DateTime(unixYear0.Ticks + unixTimeStampInTicks);
            return dtUnix;
        }
    }
}
