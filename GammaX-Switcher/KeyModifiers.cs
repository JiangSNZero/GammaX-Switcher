using System;

namespace GammaX_Switcher
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 0x0001,
        Ctrl = 0x0002,
        Shift = 0x0004
    }
}