using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DBDSponsor
{
    public static class ProcessExtension
    {
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
            if (!File.Exists(dllPath)) return false;
            byte[] dllName = Encoding.ASCII.GetBytes(dllPath);

            IntPtr hProcess = OpenProcess(0x1F0FFF, true, process.Id);
            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine("Handle Error");
                CloseHandle(hProcess);
                return false;
            }

            IntPtr allocMem = VirtualAllocEx(hProcess, IntPtr.Zero, dllName.Length, 0x1000, 0x40);
            if (allocMem == IntPtr.Zero)
            {
                Console.WriteLine("Alloc Error");
                CloseHandle(hProcess);
                return false;
            }

            WriteProcessMemory(hProcess, allocMem, dllName, dllName.Length, out _);
            IntPtr kernel = GetModuleHandle("kernel32.dll");

            if (kernel == IntPtr.Zero)
            {
                Console.WriteLine("Kernel Error");
                CloseHandle(hProcess);
                return false;
            }
            IntPtr injector = GetProcAddress(kernel, "LoadLibraryA");

            if (injector == IntPtr.Zero)
            {
                Console.WriteLine("Inject Error");
                CloseHandle(hProcess);
                return false;
            }

            IntPtr result = CreateRemoteThread(hProcess, IntPtr.Zero, 0, injector, allocMem, 0, IntPtr.Zero);

            if (result == IntPtr.Zero)
            {
                Console.WriteLine("CreateRemoteThread Error");
                CloseHandle(hProcess);
                return false;
            }

            CloseHandle(hProcess);
            Console.WriteLine("Dll was injected");
            return true;
        }

        public static IntPtr CreateHandle(this Process process)
        {
            return OpenProcess(0x1F0FFF, true, process.Id);
        }
        public static void Close(this IntPtr handle)
        {
            CloseHandle(handle);
            handle = IntPtr.Zero;
        }
    }
}
