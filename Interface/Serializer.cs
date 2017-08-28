using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Interface
{
    public class Serializer
    {
        public static string SerializeObject(object o)
        {
            if (!o.GetType().IsSerializable)
            {
                return null;
            }

            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, o);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public static object DeserializeObject(string str)
        {
            var bytes = Convert.FromBase64String(str);

            using (var stream = new MemoryStream(bytes))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}
