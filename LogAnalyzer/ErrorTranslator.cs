using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LogAnalyzer
{
    public class ErrorTranslator
    {
        const int MAX_PATH = 260;

        [DllImport("kernel32.dll")]
        private static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr Arguments);

        public static string TranslateErrorCode(int errorCode)
        {
            StringBuilder buffer = new StringBuilder(MAX_PATH);
            IntPtr nullPtr = new IntPtr(0);

            FormatMessage(0x00001000, nullPtr, errorCode, 0, buffer, MAX_PATH, nullPtr);

            return buffer.ToString();
        }
    }
}
