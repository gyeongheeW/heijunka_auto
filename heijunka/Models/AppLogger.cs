using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using heijunka.Models;

namespace heijunka.Models
{
    public static class AppLogger
    {
        public static bool IsEnabled { get; set; } = true;

        public static void Log(string message)
        {
            if (!IsEnabled) return;
            var msg = $"[LOG] {message}";
            Console.WriteLine(msg);
            System.Diagnostics.Debug.WriteLine(msg);
            AppLogBuffer.Add(msg);
        }

        public static void Info(string message)
        {
            if (!IsEnabled) return;
            var msg = $"[INFO] {message}";
            Console.WriteLine(msg);
            System.Diagnostics.Debug.WriteLine(msg);
            AppLogBuffer.Add(msg);
        }

        public static void Warn(string message)
        {
            if (!IsEnabled) return;
            var msg = $"[WARN] ⚠ {message}";
            Console.WriteLine(msg);
            System.Diagnostics.Debug.WriteLine(msg);
            AppLogBuffer.Add(msg);
        }

        public static void Error(string message)
        {
            if (!IsEnabled) return;
            var msg = $"[ERROR] ✕ {message}";
            Console.WriteLine(msg);
            System.Diagnostics.Debug.WriteLine(msg);
            AppLogBuffer.Add(msg);
        }

        public static void Section(string title)
        {
            if (!IsEnabled) return;
            var msg = $"\n── {title} ──────────────────";
            Console.WriteLine(msg);
            System.Diagnostics.Debug.WriteLine(msg);
            AppLogBuffer.Add(msg);
        }
    }
}