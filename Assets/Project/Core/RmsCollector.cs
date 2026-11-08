using System;
using System.Collections.Generic;

// Collects RMS values for a fixed time span, e.g. to measure the background noise.
public class RmsCollector
{
    private readonly float _durationInSeconds;
    private readonly List<float> _values = new List<float>();
    private float _startTime;
    private bool _hasStarted;

    public IReadOnlyList<float> GetValues => _values;

    public RmsCollector(float durationInSeconds)
    {
        if (durationInSeconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(durationInSeconds), "Duration must be greater than 0.");

        _durationInSeconds = durationInSeconds;
    }

    // Clears old values and starts a new collection period.
    public void Start(float now)
    {
        _values.Clear();
        _startTime = now;
        _hasStarted = true;
    }

    public void Add(float rms)
    {
        if (!_hasStarted)
            return;

        _values.Add(rms);
    }

    // True once Start was called and the duration has passed.
    public bool IsDone(float now)
    {
        return _hasStarted && (now - _startTime) >= _durationInSeconds;
    }
}