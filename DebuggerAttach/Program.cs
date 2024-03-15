﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace DebuggerAttach
{
    internal class Program
    {
        static void Main()
        {
            var name = "VCamNetSample";
            var vs = GetVisualStudioInstance(name);
            if (vs == null)
            {
                Console.WriteLine($"Solution {name} was not found. You should retry...");
                return;
            }

            var frameServer = EnumerateLocalProcesses((object)vs).FirstOrDefault(IsFrameServer);
            if (frameServer == null)
            {
                Console.WriteLine($"Camera Frame Server was not found. You should retry...");
                return;
            }

            frameServer.Attach();
        }

        static bool IsFrameServer(IProcess4 process4) => IsSvcHost(process4, args => args.Contains("-k") && args.Contains("Camera") && args.Contains("-s") && args.Contains("FrameServer"));
        static bool IsCameraMonitor(IProcess4 process4) => IsSvcHost(process4, args => args.Contains("-k") && args.Contains("CameraMonitor"));
        static bool IsSvcHost(IProcess4 process4, Predicate<IReadOnlyList<string>> commandLinePredicate)
        {
            ArgumentNullException.ThrowIfNull(commandLinePredicate);
            var commandLine = process4.CommandLine;
            if (string.IsNullOrWhiteSpace(commandLine))
                return false;

            var svchost = Path.Combine(Environment.SystemDirectory, "svchost.exe");
            var args = CommandLineToArguments(commandLine);
            if (!args.Contains(svchost))
                return false;

            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }
            Console.WriteLine();
            return commandLinePredicate(args);
        }

        public static IEnumerable<IProcess4> EnumerateLocalProcesses(dynamic vsInstance)
        {
            var debugger = vsInstance.Debugger;
            foreach (var lp in debugger.LocalProcesses)
            {
                var process = (IProcess4)lp;
                yield return process;
            }
        }

        [ComImport, Guid("49DB35DD-FDD9-43BA-BD3F-2BAF50F5C45E"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface IProcess4
        {
            [DispId(1)]
            void Attach();

            [DispId(2)]
            void Detach(bool WaitForBreakOrEnd = true);

            [DispId(3)]
            void Break(bool WaitForBreakMode = true);

            [DispId(4)]
            void Terminate(bool WaitForBreakOrEnd = true);

            [DispId(0)]
            string Name { get; }

            [DispId(100)]
            int ProcessID { get; }

            [DispId(2102)]
            string CommandLine { get; }

            [DispId(2103)]
            string CurrentDirectory { get; }
        }

        public static dynamic? GetVisualStudioInstance(string name)
        {
            foreach (var instance in EnumerateVisualStudioInstances())
            {
                try
                {
                    var solution = instance.Solution;
                    var filePath = (string)solution.FullName;
                    if (filePath == null)
                        continue;

                    if (filePath.EndsWith(name + ".sln", StringComparison.OrdinalIgnoreCase))
                        return instance;
                }
                catch
                {
                    // continue
                }
            }
            return null;
        }

        public static IEnumerable<dynamic> EnumerateVisualStudioInstances()
        {
            _ = GetRunningObjectTable(0, out var table);
            if (table == null)
                yield break;

            table.EnumRunning(out var enumerator);
            if (enumerator == null)
                yield break;

            var mks = new IMoniker[1];
            do
            {
                if (enumerator.Next(1, mks, IntPtr.Zero) != 0)
                    break;

                var mk = mks[0];
                if (mk != null)
                {
                    string displayName;
                    try
                    {
                        _ = CreateBindCtx(0, out var ctx);
                        mk.GetDisplayName(ctx, null, out displayName);
                    }
                    catch
                    {
                        continue;
                    }
                    if (displayName == null || !displayName.StartsWith("!VisualStudio.DTE."))
                        continue;

                    table.GetObject(mk, out dynamic instance);
                    if (instance != null)
                        yield return instance;
                }
            }
            while (true);
        }

        public static IReadOnlyList<string> CommandLineToArguments(string cmdLine)
        {
            if (string.IsNullOrEmpty(cmdLine))
                return [];

            var intPtr = IntPtr.Zero;
            try
            {
                intPtr = CommandLineToArgvW(cmdLine, out var count);
                if (intPtr == IntPtr.Zero)
                    return [];

                var args = new string[count];
                for (var i = 0; i < count; i++)
                {
                    var ptr = Marshal.ReadIntPtr(intPtr, i * Marshal.SizeOf(typeof(IntPtr)));
                    args[i] = Marshal.PtrToStringUni(ptr) ?? string.Empty;
                }
                return args;
            }
            finally
            {
                LocalFree(intPtr);
            }
        }

        [DllImport("ole32")]
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable pprot);

        [DllImport("ole32")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        [DllImport("kernel32")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("shell32")]
        private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string cmdLine, out int numArgs);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    }
}
