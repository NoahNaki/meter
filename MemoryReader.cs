using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KiwiAFK.Memory
{
    public class MemoryReader
    {
        private const string PROCESS_NAME = "BNSR.exe";
        private const int COMBAT_LOG_LENGTH = 600;

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            PROCESS_VM_READ = 0x0010,
            PROCESS_QUERY_INFORMATION = 0x0400
        }

        private Process process;
        private IntPtr processHandle;
        private IntPtr baseModuleAddress;
        private readonly long[] offsets = { 0x7485118, 0x490, 0x490, 0x670, 0x8, 0x70 };

        public MemoryReader()
        {
            InitializeProcess();
        }

        private void InitializeProcess()
        {
            Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(PROCESS_NAME));
            if (processes.Length == 0)
            {
                throw new Exception($"Process {PROCESS_NAME} not found.");
            }

            process = processes[0];
            processHandle = process.Handle;
            baseModuleAddress = process.MainModule.BaseAddress;
        }

        public List<string> GetCombatChatLog()
        {
            List<string> lines = new List<string>();

            try
            {
                IntPtr currentAddress = baseModuleAddress;

                for (int i = 0; i < offsets.Length - 1; i++)
                {
                    currentAddress = ReadPointer(currentAddress, (int)offsets[i]);
                    if (currentAddress == IntPtr.Zero)
                    {
                        throw new Exception($"Null pointer encountered at offset index {i}");
                    }
                }

                for (int i = 0; i < COMBAT_LOG_LENGTH; i++)
                {
                    try
                    {
                        IntPtr logEntryAddress = ReadPointer(currentAddress, i * (int)offsets[offsets.Length - 1]);

                        if (logEntryAddress != IntPtr.Zero)
                        {
                            string line = ReadString(logEntryAddress, 512);
                            int periodIndex = line.IndexOf('.');

                            if (periodIndex != -1)
                            {
                                lines.Add(line.Substring(0, periodIndex + 1));
                            }
                            else
                            {
                                lines.Add("");
                            }
                        }
                        else
                        {
                            lines.Add("");
                        }
                    }
                    catch
                    {
                        lines.Add("");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading memory. Please contact caera1911 on discord.", ex);
            }

            return lines;
        }

        private IntPtr ReadPointer(IntPtr baseAddress, int offset)
        {
            byte[] buffer = new byte[8];
            IntPtr bytesRead;

            if (!ReadProcessMemory(processHandle, IntPtr.Add(baseAddress, offset), buffer, buffer.Length, out bytesRead))
            {
                throw new Exception($"Failed to read memory at address {baseAddress.ToInt64() + offset:X}");
            }

            return new IntPtr(BitConverter.ToInt64(buffer, 0));
        }

        private string ReadString(IntPtr address, int size, string encoding = "utf-16le")
        {
            byte[] buffer = new byte[size];
            IntPtr bytesRead;

            if (!ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead))
            {
                throw new Exception($"Failed to read string at address {address.ToInt64():X}");
            }

            int stringLength = 0;
            if (encoding == "utf-16le")
            {
                for (int i = 0; i < buffer.Length - 1; i += 2)
                {
                    if (buffer[i] == 0 && buffer[i + 1] == 0)
                    {
                        stringLength = i;
                        break;
                    }
                }
                if (stringLength == 0) stringLength = buffer.Length;
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == 0)
                    {
                        stringLength = i;
                        break;
                    }
                }
                if (stringLength == 0) stringLength = buffer.Length;
            }

            if (encoding == "utf-16le")
            {
                return Encoding.Unicode.GetString(buffer, 0, stringLength);
            }
            else
            {
                return Encoding.UTF8.GetString(buffer, 0, stringLength);
            }
        }
    }
}
