using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogWindow.Core
{
    public static class IoC
    {
        private static readonly Dictionary<Type, object> MAP;

        static IoC()
        {
            MAP = new Dictionary<Type, object>();
        }

        public static void Register<T>(T instance)
        {
            MAP[typeof(T)] = instance;
        }

        public static T Provide<T>()
        {
            if (MAP.TryGetValue(typeof(T), out object obj) && obj is T t)
            {
                return t;
            }

            throw new Exception("No registered service of type " + typeof(T));
        }
    }
}
