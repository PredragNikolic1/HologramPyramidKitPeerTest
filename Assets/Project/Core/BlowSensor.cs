using System;

public class BlowSensor
{
    private enum SensorState { IDLE, BLOWING, FIRED }

    private const float BLOW_GATE = 8f;                     // above silence flicker (~6x)
    private const float BLOW_MIN_DURATION_IN_SECONDS = 0.6f; // "candles take effort"
    private const float GAP_TOLERANCE_IN_SECONDS = 0.15f;    // short dips don't end a blow
    private const float REARM_QUIET_IN_SECONDS = 0.3f;       // quiet this long before the next blow

    private const float MINIMUM_PEAK_RATIO = 35;

    private SensorState _state = SensorState.IDLE;
    private float _blowStartTime;
    private float _lastLoudTime;

    private float _peakRatio = 1f;



    public bool Detect(float ratioRmsToBackground, float currentTimeInSeconds)
    {
        bool isBlow = false;
        bool isLoud = ratioRmsToBackground >= BLOW_GATE;

        if (isLoud)
        {
            _lastLoudTime = currentTimeInSeconds;
        }

        switch (_state)
        {
            case SensorState.IDLE:
                if (isLoud)
                {
                    _blowStartTime = currentTimeInSeconds;
                    _peakRatio = ratioRmsToBackground;
                    _state = SensorState.BLOWING;
                }
                break;

            case SensorState.BLOWING:
                float duration = MathF.Abs(_blowStartTime - currentTimeInSeconds);
                float lastLoudTimeDuration = MathF.Abs(_lastLoudTime - currentTimeInSeconds);
                _peakRatio = MathF.Max(_peakRatio, ratioRmsToBackground);

                if (lastLoudTimeDuration > GAP_TOLERANCE_IN_SECONDS)
                {
                    _blowStartTime = 0f;
                    _state = SensorState.IDLE;
                }
                else if(duration >= BLOW_MIN_DURATION_IN_SECONDS && _peakRatio >= MINIMUM_PEAK_RATIO)
                {
                    _blowStartTime = 0f;
                    isBlow = true;
                    _state = SensorState.FIRED;
                }
                break;

            case SensorState.FIRED:
                float firedDuration = MathF.Abs(_lastLoudTime - currentTimeInSeconds);
                if(firedDuration > REARM_QUIET_IN_SECONDS)
                {
                    _state = SensorState.IDLE;
                }
                break;
        }

        return isBlow;
    }
}