using UnityEngine;

public class SensorContext
{
    public bool HasClap { get; set; }
    public bool HasBlow { get; set; }
    public bool HasLight { get; set; }
    // later: brightness, loudness, whatever new inputs you add

    public SensorContext(bool hasClap, bool hasBlow, bool hasLight)
    {
        HasClap = hasClap;
        HasBlow = hasBlow;
        HasLight = hasLight;
    }

    public SensorContext() : this(false, false, false)
    {
    }
}
