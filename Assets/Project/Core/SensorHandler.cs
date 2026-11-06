using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class SensorHandler
{
    private readonly ClapSensor _clapSensor;
    private readonly BlowSensor _blowSensor;
    private readonly LightDomainSensor _lightSensor;

#if UNITY_WEBGL && !UNITY_EDITOR
    private const float CLAP_MIN_PEAK = 0.5f;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    private const float CLAP_MIN_PEAK = 0.05f;
#endif

#if UNITY_EDITOR
    private const float CLAP_MIN_PEAK = 0.05f;
#endif

    public SensorHandler(ClapSensor clapSensor, BlowSensor blowSensor, LightDomainSensor lightSensor)
    {
        _clapSensor = clapSensor;
        _blowSensor = blowSensor;
        _lightSensor = lightSensor;
    }

    public static SensorHandler CreateHandler()
    {
        return new SensorHandler(new ClapSensor(CLAP_MIN_PEAK), new BlowSensor(), new LightDomainSensor());
    }
    // + background, gate, loud start, duration state

    // called once per frame by the orchestrator
    public void UpdateContext(float peak, float rms, float brightness, SensorContext context)
    {
        // 1. background + ratio + gate
        // 2. sound start/end + duration
        // 3. sensors decide
        // 4. write results into context

        bool isLoud = false;
        float gate = 0.17f;

    //keep value display logic for future debugging
        bool gateOpen = rms > gate;
        bool soundStarted = false;
        bool soundEnded = false;
        float loudStartTime = 0.0f;
        float lastDuration = 0.0f;

        if (gateOpen && !isLoud)
        {
            loudStartTime = Time.time;
            isLoud = true;
            soundStarted = true;
        }
        else if (!gateOpen && isLoud)
        {
            lastDuration = Time.time - loudStartTime;
            isLoud = false;
            soundEnded = true;
        }
        // sensors decide, every frame
        bool hasClapOccured = _clapSensor.Detect(soundStarted, isLoud, soundEnded, peak, rms, lastDuration, gate);
        bool hasBlowOccured = _blowSensor.Detect(soundStarted, isLoud, soundEnded, rms, lastDuration, gate);
        bool hasLightTrigger = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        hasLightTrigger = _lightSensor.DetectLux(luxMeasured);
#endif

#if UNITY_WEBGL && UNITY_EDITOR
        hasLightTrigger = _lightSensor.DetectBrightness(brightness);
#endif

        context.HasBlow = hasBlowOccured;
        context.HasClap = hasClapOccured;
        context.HasLight = hasLightTrigger;

        return;
    }
}
