using System;

public interface IMicrophoneInputHandler
{
    public float MeasureLoudness();

    public bool IsReady();

    public float GetLastRms();
}
