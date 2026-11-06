using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

//Microphone input for testing in the Unity Editor. Uses Unity's Microphone class, which works on desktop but not in WebGL.
public class EditorMicrophoneInputHandler : IMicrophoneInputHandler
{
    private const int SAMPLE_RATE_IN_FREQUENCY_HZ = 44100;
    private const int CLIP_DURATION_IN_SECONDS = 1;
    private const int SAMPLE_WINDOW = 1024;

    private AudioClip _mic;
    private readonly float[] _data = new float[SAMPLE_WINDOW];
    private float _lastRms;

    public EditorMicrophoneInputHandler()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[EditorMicrophoneInputHandler] No microphone found.");
            return;
        }

        _mic = Microphone.Start(null, true, CLIP_DURATION_IN_SECONDS, SAMPLE_RATE_IN_FREQUENCY_HZ);

        if (_mic == null)
            Debug.LogWarning("[EditorMicrophoneInputHandler] Microphone could not be started.");
    }

    public bool IsReady()
    {
        return _mic != null && Microphone.IsRecording(null);
    }

    //Returns the peak of the newest samples and stores their RMS.
    public float MeasureLoudness()
    {
        if (!IsReady()) return 0f;

        //Read the newest samples right before the recording position. GetData wraps around the looping clip.
        int position = Microphone.GetPosition(null) - SAMPLE_WINDOW;
        if (position < 0) position += _mic.samples;

        _mic.GetData(_data, position);

        float peak = 0f;
        float sumOfSquares = 0f;
        foreach (float f in _data)
        {
            peak = Mathf.Max(peak, Mathf.Abs(f));
            sumOfSquares += f * f;
        }
        _lastRms = Mathf.Sqrt(sumOfSquares / _data.Length);

        return peak;
    }

    public float GetLastRms()
    {
        return _lastRms;
    }
}