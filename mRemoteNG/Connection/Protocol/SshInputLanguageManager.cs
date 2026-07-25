using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using mRemoteNG.App;

namespace mRemoteNG.Connection.Protocol
{
    [SupportedOSPlatform("windows")]
    internal static class SshInputLanguageManager
    {
        private const int WmInputLanguageChangeRequest = 0x0050;
        private const int EnglishUnitedStatesLanguageId = 0x0409;

        public static bool TryUseEnglish(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return false;

            int layoutCount = NativeMethods.GetKeyboardLayoutList(0, null);
            if (layoutCount <= 0)
                return false;

            IntPtr[] layouts = new IntPtr[layoutCount];
            int copiedCount = NativeMethods.GetKeyboardLayoutList(layouts.Length, layouts);
            IntPtr englishLayout = FindEnglishLayout(layouts.Take(copiedCount));
            if (englishLayout == IntPtr.Zero)
                return false;

            return NativeMethods.PostMessage(
                windowHandle,
                WmInputLanguageChangeRequest,
                IntPtr.Zero,
                englishLayout);
        }

        internal static IntPtr FindEnglishLayout(IEnumerable<IntPtr> layouts)
        {
            return layouts.FirstOrDefault(layout =>
                (layout.ToInt64() & 0xFFFF) == EnglishUnitedStatesLanguageId);
        }
    }
}
