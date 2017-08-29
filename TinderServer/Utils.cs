namespace TinderServer
{
    internal class Utils
    {
        public static string GetConnectionString()
        {
            return @"Data Source=(LocalDB)\MSSQLLocalDB;
                    AttachDbFilename=|DataDirectory|TinderDatabase.mdf;
                    Integrated Security=True";
        }
    }
}
