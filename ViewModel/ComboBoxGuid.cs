using System;

namespace SolarNG.ViewModel;

public class ComboBoxGuid
{
    public Guid Key { get; set; }

    public string Display { get; set; }

    public ComboBoxGuid(Guid key, string display)
    {
        Key = key;
        Display = display;
    }
}
