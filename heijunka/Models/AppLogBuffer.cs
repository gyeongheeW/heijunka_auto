using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace heijunka.Models
{
    public static class AppLogBuffer
    {
        private static readonly List<string> _logs = new();

        public static void Add(string message)
        {
            _logs.Add(message);
        }

        public static string GetLog()
            => string.Join("\n", _logs);

        public static void Clear()
            => _logs.Clear();

        public static int Count => _logs.Count;
    }
}