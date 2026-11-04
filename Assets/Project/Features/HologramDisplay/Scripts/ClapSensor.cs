using System;
using UnityEngine;

public class ClapSensor
{
    private const float CLAP_MAX_DURATION_IN_SECONDS = 0.07f;
    //private const float CLAP_MIN_PEAK = 0.5f;
    private float _clap_min_peak;
    private float maxPeak, maxRms;

    public ClapSensor(float clap_min_peak)
    {
        _clap_min_peak = clap_min_peak;
    }

    // call EVERY frame
    public bool Detect(bool soundStarted, bool isLoud, bool soundEnded,
                       float peak, float rms, float durationInSeconds, float gate)
    {
        if (soundStarted)
        {
            maxPeak = 0f;
            maxRms = 0f;
        }

        if (isLoud)
        {
            maxPeak = Mathf.Max(maxPeak, peak);
            maxRms = Mathf.Max(maxRms, rms);
        }

        return soundEnded
            && durationInSeconds <= CLAP_MAX_DURATION_IN_SECONDS
            && maxPeak >= _clap_min_peak
            && maxRms >= gate;
    }
}