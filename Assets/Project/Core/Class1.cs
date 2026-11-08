using System;
using System.Collections.Generic;
using System.Text;

public class BackgroundCalibrator
{
    private readonly RmsCollector _collector;

    public float Background { get; private set; }
    public bool IsCalibrating { get; private set; }

    public static BackgroundCalibrator CreateBackgroundCalibration(float durationInSeconds)
    {
        return new BackgroundCalibrator(new RmsCollector(durationInSeconds));
    }
    public BackgroundCalibrator(RmsCollector collector)
    {
        _collector = collector;
    }

    public void Start(float now)
    {
        if (!IsCalibrating)
        {
            _collector.Start(now);
            IsCalibrating = true;
        }
    }

    // call every frame
    public void Update(float rms, float now)
    {
        if (!IsCalibrating)
            return;                     

        _collector.Add(rms);

        if (_collector.IsDone(now))
        {
            Background = BackgroundCalculator.Calculate(_collector.GetValues);
            IsCalibrating = false;           
        }
    }
}