using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class SensorHandler
{
    private readonly ClapSensor _clapSensor;
    private readonly BlowSensor _blowSensor;
    private readonly LightDomainSensor _lightSensor;

    private readonly BackgroundCalibrator _calibrator;

    private const float CALIBRATION_DURATION_IN_SECONDS = 2f;

    private bool _hasCalibrationBeenRequested;


    public SensorHandler(ClapSensor clapSensor, BlowSensor blowSensor, LightDomainSensor lightSensor, BackgroundCalibrator calibrator)
    {
        _clapSensor = clapSensor;
        _blowSensor = blowSensor;
        _lightSensor = lightSensor;
        _calibrator = calibrator;
        _hasCalibrationBeenRequested = true;
    }

    public static SensorHandler CreateHandler()
    {
        return new SensorHandler(new ClapSensor(), new BlowSensor(), new LightDomainSensor(), new BackgroundCalibrator(new RmsCollector(CALIBRATION_DURATION_IN_SECONDS)));
    }
    // + background, gate, loud start, duration state

    // called once per frame by the orchestrator
    public void UpdateContext(float peak, float rms, float brightness, float now, SensorContext context)
    {


        if (!_calibrator.IsCalibrating && !_hasCalibrationBeenRequested)
        {
            // sensors decide, every frame
            bool hasClapOccured = _clapSensor.Detect((rms / _calibrator.Background), Time.time);
            //TODO FIX BLOW SENSOR LATER ON
            //bool hasBlowOccured = _blowSensor.Detect(soundStarted, isLoud, soundEnded, rms, lastDuration, gate);
            bool hasLightTrigger = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        hasLightTrigger = _lightSensor.DetectLux(luxMeasured);
#endif

#if UNITY_WEBGL && UNITY_EDITOR
            hasLightTrigger = _lightSensor.DetectBrightness(brightness);
#endif
            context.SetToNonCalibrationContext();
            //context.HasBlow = hasBlowOccured;
            context.HasClap = hasClapOccured;
            context.HasLight = hasLightTrigger;
        }
        else
        {
            context.SetToCalibrationContext();
            RunCalibration(rms, now);
        }


        return;
    }

    private void RunCalibration(float rms,float now)
    {
        if (_hasCalibrationBeenRequested)
        {
            _calibrator.Start(now);
            _hasCalibrationBeenRequested = false;
        }
        else
            _calibrator.Update(rms, now);

    }

    public void Recalibrate()
    {
        _hasCalibrationBeenRequested = true;
    }
}
