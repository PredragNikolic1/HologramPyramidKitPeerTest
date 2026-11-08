using System;

public class LightDomainSensor
{
    private float _luxThreshold = 50;

    public bool DetectLux(float measured)
    {
        return measured >= _luxThreshold;
    }

    public bool DetectBrightness(float measured)
    {
        return (measured >= 0.85) && (measured <= 1);
    }
}
