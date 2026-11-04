using System;

public class LightDomainSensor
{
    private float _luxThreshold = 50;

    public bool Detect(float measuredLux)
    {
        return measuredLux >= _luxThreshold;
    }
}
