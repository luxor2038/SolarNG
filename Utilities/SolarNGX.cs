using System.Runtime.InteropServices;
using System.Text;

namespace SolarNG.Utilities;

internal class SolarNGX
{
    [DllImport("SolarNGX.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, CharSet = CharSet.Unicode)]
    private static extern int GetProcessEnvironmentVariable(int pid, string name, byte[] value, int value_size);
    internal static string GetProcessEnvironmentVariable(int pid, string name)
    {
        byte[] value = new byte[128];
        int value_size = value.Length;
        int v_size = GetProcessEnvironmentVariable(pid, name, value, value_size);
        if(v_size == 0)
        {
            return null;
        }

        if (v_size > value_size)
        {
            value = new byte[v_size];
            v_size = GetProcessEnvironmentVariable(pid, name, value, value_size);
            if (v_size == 0 || v_size > value_size)
            {
                return null;
            }
        }

        return Encoding.Unicode.GetString(value, 0, v_size);
    }

    [DllImport("SolarNGX.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, CharSet = CharSet.Ansi)]
    internal static extern int StartPipeFileServer(int maxPipes);

    [DllImport("SolarNGX.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, CharSet = CharSet.Ansi)]
    internal static extern void StopPipeFileServer();

    [DllImport("SolarNGX.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, CharSet = CharSet.Ansi)]
    internal static extern int CreatePipeFile(string pipeName, byte[] pipeData, int pipeDataSize, int maxInstances);

    [DllImport("SolarNGX.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, CharSet = CharSet.Ansi)]
    internal static extern void ClosePipeFile(int inst);

    [DllImport("SolarNGX.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, CharSet = CharSet.Ansi)]
    internal static extern int TestPipeFile(int inst);


    [DllImport("SolarNGX.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, CharSet = CharSet.Ansi)]
    internal static extern uint AESGCMCrypt(int encrypt, byte[] input, uint inputlen, byte[] key, byte[] iv, byte[] nonce, byte[] auth, byte[] output, uint outputlen);

    [DllImport("SolarNGX.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false, CharSet = CharSet.Ansi)]
    internal static extern int Argon2dHash(byte[] pwd, uint pwdlen, byte[] salt, uint saltlen, byte[] hash, uint hashlen);

    internal const int AES_KEY_SIZE = 32;
    internal const int AES_GCM_NONCE_SIZE = 12;
    internal const int AES_GCM_IV_SIZE = 12;
    internal const int AES_GCM_AUTH_SIZE = 16;

}
