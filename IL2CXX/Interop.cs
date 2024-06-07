using System.Runtime.InteropServices;

namespace IL2CXX.Interop;

internal class Sys
{
    [DllImport("libSystem.Native", EntryPoint = "SystemNative_SetPosixSignalHandler")]
    public static extern void SetPosixSignalHandler(IntPtr handler);
    [DllImport("libSystem.Native", EntryPoint = "SystemNative_SetTerminalInvalidationHandler")]
    public static extern void SetTerminalInvalidationHandler(IntPtr handler);
}
internal class Globalization
{
    [DllImport("libSystem.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EnumCalendarInfo")]
    public static extern bool EnumCalendarInfo(IntPtr callback, string localeName, ushort calendarId, int calendarDataType, IntPtr context);
}
