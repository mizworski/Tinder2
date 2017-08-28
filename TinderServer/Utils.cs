using System;

namespace TinderServer
{
    internal class Utils
    {
        public static string GetConnectionString()
        {
            return @"Data Source=(LocalDB)\MSSQLLocalDB;
                    AttachDbFilename=C:\Users\Michal\documents\visual studio 2017\Projects\Tinder\TinderServer\TinderDatabase.mdf;
                    Integrated Security=True";
        }
    }
}
