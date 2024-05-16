using System;

namespace SolarNG.Utilities;

[Flags]
public enum KeyModifier
{
    None = 0,
    Alt = 1,
    Ctrl = 2,
    NoRepeat = 0x4000,
    Shift = 4,
    Win = 8
}
