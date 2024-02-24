using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace VCamNetSampleSource.Utilities
{
    public sealed class EventProvider : IDisposable
    {
        // we don't use OutputDebugString because it's 100% crap, truncating, slow, etc.
        // use WpfTraceSpy https://github.com/smourier/TraceSpy to see these traces (configure an ETW Provider with guid set to 964d4572-adb9-4f3a-8170-fcbecec27467)
        public static Guid DefaultProviderId { get; } = new Guid("964d4572-adb9-4f3a-8170-fcbecec27467");

        public static EventProvider? Current => _current.Value;
        private static readonly Lazy<EventProvider?> _current = new(() => new EventProvider(DefaultProviderId));

        private long _handle;
        public Guid Id { get; }

        public EventProvider(Guid id)
        {
            Id = id;
            var hr = EventRegister(id, nint.Zero, nint.Zero, out _handle);
            if (hr != 0)
                throw new Win32Exception(hr);
        }

        public bool WriteMessageEvent(string? text, byte level = 0, long keywords = 0) => EventWriteString(_handle, level, keywords, text) == 0;

        public void Dispose()
        {
            var handle = Interlocked.Exchange(ref _handle, 0);
            if (handle != 0)
            {
                _ = EventUnregister(handle);
            }
        }

        public static void LogError(string? message = null, [CallerMemberName] string? methodName = null, [CallerFilePath] string? filePath = null) => Log(TraceLevel.Error, message, methodName, filePath);
        public static void LogWarning(string? message = null, [CallerMemberName] string? methodName = null, [CallerFilePath] string? filePath = null) => Log(TraceLevel.Warning, message, methodName, filePath);
        public static void LogInfo(string? message = null, [CallerMemberName] string? methodName = null, [CallerFilePath] string? filePath = null) => Log(TraceLevel.Info, message, methodName, filePath);
        public static void LogVerbose(string? message = null, [CallerMemberName] string? methodName = null, [CallerFilePath] string? filePath = null) => Log(TraceLevel.Verbose, message, methodName, filePath);
        public static void Log(TraceLevel level, string? message = null, [CallerMemberName] string? methodName = null, [CallerFilePath] string? filePath = null)
        {
            var current = Current;
            if (current == null)
                return;

            var name = filePath != null ? Path.GetFileNameWithoutExtension(filePath) : null;
            current.WriteMessageEvent($"[{Environment.CurrentManagedThreadId}]{name}::{methodName}:{message}", (byte)level);
        }

        [DllImport("advapi32")]
        private static extern int EventRegister([MarshalAs(UnmanagedType.LPStruct)] Guid ProviderId, nint EnableCallback, nint CallbackContext, out long RegHandle);

        [DllImport("advapi32")]
        private static extern int EventUnregister(long RegHandle);

        [DllImport("advapi32")]
        private static extern int EventWriteString(long RegHandle, byte Level, long Keyword, [MarshalAs(UnmanagedType.LPWStr)] string? String);
    }
}
