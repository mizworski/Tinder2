using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinder.Pages
{
    class Utils
    {
        public static string GetConnectionString()
        {
            return @"Data Source=(LocalDB)\MSSQLLocalDB;
                    AttachDbFilename=C:\Users\Michal\documents\visual studio 2017\Projects\Tinder\Tinder\TinderDatabase.mdf;
                    Integrated Security=True";
        }
    }
}
