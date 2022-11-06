using System.Diagnostics;
using System.Runtime.CompilerServices;
using Godot;
using Path = System.IO.Path;

namespace GodotSharp.BuildingBlocks
{
    public static class Log
    {
        public static bool EnableTimestamp { get; set; } = true;
        public static bool EnableRuntime { get; set; } = true;
        public static bool EnableThreadId { get; set; } = false;
        public static bool EnableFileName { get; set; } = true;
        public static bool EnableMemberName { get; set; } = false;

        [Conditional("DEBUG")]
        public static void Debug(object msg = null, [CallerFilePath] string filePath = null, [CallerMemberName] string memberName = null) => GD.Print(Format(filePath, memberName, msg));
        public static void Info(object msg = null, [CallerFilePath] string filePath = null, [CallerMemberName] string memberName = null) => GD.Print(Format(filePath, memberName, msg));
        public static void Warn(object msg = null, [CallerFilePath] string filePath = null, [CallerMemberName] string memberName = null) => GD.PushWarning(Format(filePath, memberName, msg));
        public static void Error(object msg = null, [CallerFilePath] string filePath = null, [CallerMemberName] string memberName = null) => GD.PushError(Format(filePath, memberName, msg));

        [Conditional("DEBUG")] public static void If(bool log, object msg = null, [CallerFilePath] string filePath = null, [CallerMemberName] string memberName = null) { if (log) GD.Print(Format(filePath, memberName, msg)); }

        private static string Format(string filePath, string memberName, object msg)
            => $"{Timestamp()}{Runtime()}{ThreadId()}{FileName(filePath)}{MemberName(memberName)}{msg}";

        private static string Timestamp() => EnableTimestamp ? DateTime.Now.ToString("[HH:mm:ss.fff] ") : null;
        private static string Runtime() => EnableRuntime ? $"[{TimeSpan.FromMilliseconds(Time.GetTicksMsec()).Format()}] " : null;
        private static string ThreadId() => EnableThreadId ? $"[THRD{System.Threading.Thread.CurrentThread.ManagedThreadId}] " : null;
        private static string FileName(string x) => EnableFileName ? $"[{Path.GetFileNameWithoutExtension(x)}] " : null;
        private static string MemberName(string x) => EnableMemberName && x is not null ? $"[{x}] " : null;

        private static string Format(this TimeSpan value, string noTimeStr = "0ms")
        {
            var timeStr = value.ToString("d'.'hh':'mm':'ss'.'fff").TrimStart('0', ':', '.');
            return timeStr == "" ? noTimeStr
                : !timeStr.Contains('.') ? $"{timeStr}ms"
                : timeStr;
        }

        #region Extensions

        public static string Var<T>(T value, [CallerArgumentExpression(nameof(value))] string key = null)
            => $"{key}: {value}";

        public static string Var<T1, T2>(T1 value1, T2 value2, [CallerArgumentExpression(nameof(value1))] string key1 = null, [CallerArgumentExpression(nameof(value2))] string key2 = null)
            => $"{key1}: {value1}, {key2}: {value2}";

        public static string Var<T1, T2, T3>(T1 value1, T2 value2, T3 value3, [CallerArgumentExpression(nameof(value1))] string key1 = null, [CallerArgumentExpression(nameof(value2))] string key2 = null, [CallerArgumentExpression(nameof(value3))] string key3 = null)
            => $"{key1}: {value1}, {key2}: {value2}, {key3}: {value3}";

        public static string Var<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4, [CallerArgumentExpression(nameof(value1))] string key1 = null, [CallerArgumentExpression(nameof(value2))] string key2 = null, [CallerArgumentExpression(nameof(value3))] string key3 = null, [CallerArgumentExpression(nameof(value4))] string key4 = null)
            => $"{key1}: {value1}, {key2}: {value2}, {key3}: {value3}, {key4}: {value4}";

        public static string Var<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, [CallerArgumentExpression(nameof(value1))] string key1 = null, [CallerArgumentExpression(nameof(value2))] string key2 = null, [CallerArgumentExpression(nameof(value3))] string key3 = null, [CallerArgumentExpression(nameof(value4))] string key4 = null, [CallerArgumentExpression(nameof(value5))] string key5 = null)
            => $"{key1}: {value1}, {key2}: {value2}, {key3}: {value3}, {key4}: {value4}, {key5}: {value5}";

        public static string Var<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, [CallerArgumentExpression(nameof(value1))] string key1 = null, [CallerArgumentExpression(nameof(value2))] string key2 = null, [CallerArgumentExpression(nameof(value3))] string key3 = null, [CallerArgumentExpression(nameof(value4))] string key4 = null, [CallerArgumentExpression(nameof(value5))] string key5 = null, [CallerArgumentExpression(nameof(value6))] string key6 = null)
            => $"{key1}: {value1}, {key2}: {value2}, {key3}: {value3}, {key4}: {value4}, {key5}: {value5}, {key6}: {value6}";

        #endregion
    }
}
