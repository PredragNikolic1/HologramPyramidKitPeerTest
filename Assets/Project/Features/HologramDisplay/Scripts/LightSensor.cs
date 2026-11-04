using System;

public class LightSensor
{
    private float _luxThreshold = 50;

    public bool Detect(float measuredLux)
    {
        return measuredLux >= _luxThreshold;
    }
}
