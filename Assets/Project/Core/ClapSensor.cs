using System;

public class ClapSensor
{
    private enum SensorState
    {
        IDLE,
        LISTENING,
        ECHO,
    }
    private const float CLAP_MAX_DURATION_IN_SECONDS = 0.15f;

    private const float MIN_CLAP_ONSET_JUMP = 5.0f;
    private const float CLAP_ECHO_MAX_DURATION_IN_SECONDS = 0.6f;


    private const float ECHO_END_RATIO = 3f;

    private SensorState _state;

    private const float GATE = 15.0f;

    private float _previousTime;

    private float _previousRatio;

    private float _peakRatio;


    public ClapSensor()
    { 
        _state = SensorState.IDLE;
        _previousRatio = 1.0f;
    }


    // call EVERY frame
    public bool Detect(float ratioRmsToBackground, float currentTimeInSeconds)
    {
        bool isClap = false;
        float listeningDuration = 0.0f;
        switch (_state)
        {
            case SensorState.IDLE:
                float onsetClapJumpRatio = ratioRmsToBackground / MathF.Max(_previousRatio, 0.0001f);
                if ((ratioRmsToBackground >= GATE) && (onsetClapJumpRatio >= MIN_CLAP_ONSET_JUMP))
                {
                    _peakRatio = ratioRmsToBackground;
                    _state = SensorState.LISTENING;
                    _previousTime = currentTimeInSeconds;
                }
                break;

            case SensorState.LISTENING:
                listeningDuration = currentTimeInSeconds - _previousTime;       
                _peakRatio = MathF.Max(_peakRatio, ratioRmsToBackground);
                if (ratioRmsToBackground < (_peakRatio / 2))
                {
                    if (listeningDuration <= CLAP_MAX_DURATION_IN_SECONDS)
                    {
                        isClap = true;
                        _previousTime = currentTimeInSeconds;
                        _state = SensorState.ECHO;
                    }
                    else
                    {
                        _state = SensorState.IDLE;          // fell, but too slowly: blow or talk
                    }
                }
                else if (listeningDuration > CLAP_MAX_DURATION_IN_SECONDS * 3)
                {
                    _state = SensorState.IDLE;              // still loud long after: give up
                }
                break;

            case SensorState.ECHO:
                float echoDuration = currentTimeInSeconds - _previousTime;
                bool hasFaded = ratioRmsToBackground < ECHO_END_RATIO;
                bool hasTimedout = echoDuration >= CLAP_ECHO_MAX_DURATION_IN_SECONDS;
                if (hasFaded || hasTimedout)
                    _state = SensorState.IDLE;
                break;
        }
        //save the current ratio
        _previousRatio = ratioRmsToBackground;
        return isClap;
    }
}