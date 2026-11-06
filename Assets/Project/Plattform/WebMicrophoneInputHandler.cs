using System;
using System.Runtime.InteropServices;

public class WebMicrophoneInputHandler: IMicrophoneInputHandler
{
    [DllImport("__Internal")] private static extern void WebMicrophone_Start();
    [DllImport("__Internal")] private static extern int WebMicrophone_IsReady();
    [DllImport("__Internal")] private static extern float WebMicrophone_GetLoudness();
    [DllImport("__Internal")] private static extern float WebMicrophone_GetRms();

    private float _LastRms;

    public WebMicrophoneInputHandler()
    {
        //The browser shows the microphone permission prompt.
        WebMicrophone_Start();
    }

    //Returns the peak, same as AndroidMicrophoneInputHandler. GetLoudness has to run before GetRms.
    public float MeasureLoudness()
    {
        float peak = WebMicrophone_GetLoudness();
        _LastRms = WebMicrophone_GetRms();
        return peak;
    }

    public bool IsReady()
    {
        return WebMicrophone_IsReady() == 1;
    }

    public float GetLastRms()
    {
        return _LastRms;
    }
}
