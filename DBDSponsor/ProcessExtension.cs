using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace DBDSponsor
{
    public static class ProcessExtension
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess,
           IntPtr lpThreadAttributes, uint dwStackSize, IntPtr
           lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
         uint processAccess,
         bool bInheritHandle,
         int processId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
        UIntPtr dwSize, uint dwFreeType);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
        int dwSize, int flAllocationType, int flProtect);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(
         IntPtr hProcess,
         IntPtr lpBaseAddress,
         byte[] lpBuffer,
         Int32 nSize,
         out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public static bool InjectDLL(this Process process, string dllPath)
        {
            log.Debug("Injection process is preparing");
            if (!File.Exists(dllPath))
            {
                log.Fatal("AutoExit.dll is not existed");
                return false;
            }

            log.Debug("OpenProcess handle");
            byte[] dllName = Encoding.ASCII.GetBytes(dllPath);
            IntPtr hProcess = OpenProcess(0x1F0FFF, true, process.Id);
            if (hProcess == IntPtr.Zero)
            {
                log.Fatal("OpenProcess Error. Handle is invalid");
                CloseHandle(hProcess);
                return false;
            }

            log.Debug("OpenProcess is success. Try to allocate memory in miner process");
            IntPtr allocMem = VirtualAllocEx(hProcess, IntPtr.Zero, dllName.Length, 0x1000, 0x40);
            if (allocMem == IntPtr.Zero)
            {
                log.Fatal("Error of memory allocation");
                CloseHandle(hProcess);
                return false;
            }

            log.Debug("Allocation is success. Try to write AutoExit.dll in memory");
            WriteProcessMemory(hProcess, allocMem, dllName, dllName.Length, out _);
            IntPtr kernel = GetModuleHandle("kernel32.dll");

            if (kernel == IntPtr.Zero)
            {
                log.Fatal("GetModuleHandle Kernel32 Error");
                CloseHandle(hProcess);
                return false;
            }

            log.Debug("Getting address of LoadLibraryA");
            IntPtr injector = GetProcAddress(kernel, "LoadLibraryA");
            if (injector == IntPtr.Zero)
            {
                log.Fatal("GetProcAddress LoadLibraryA Injection Error");
                CloseHandle(hProcess);
                return false;
            }

            log.Debug("Creating Remote Thread");
            IntPtr result = CreateRemoteThread(hProcess, IntPtr.Zero, 0, injector, allocMem, 0, IntPtr.Zero);
            if (result == IntPtr.Zero)
            {
                log.Fatal("Remote Thread Creating Error");
                CloseHandle(hProcess);
                return false;
            }

            CloseHandle(hProcess);
            log.Debug("Dll was injected");
            return true;
        }

        public static IntPtr CreateHandle(this Process process)
        {
            log.Debug("Creating a handle of process");
            return OpenProcess(0x1F0FFF, true, process.Id);
        }
    }
}
