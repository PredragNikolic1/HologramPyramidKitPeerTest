using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AndroidLightInputHandler
{
    public AndroidLightInputHandler()
    {
        Debug.Log("[LIGHT] Light Sensor initialized");
        TryEnable();
    }

    private bool TryEnable()
    {
        var sensor = LightSensor.current;
        if (sensor == null)
            return false;

        if (!sensor.enabled)
        {
            InputSystem.EnableDevice(sensor);
            sensor.samplingFrequency = 16;
            Debug.Log("[LIGHT] Light Sensor enabled: " + sensor.enabled);
        }
        return true;
    }

    public float MeasureLux()
    {
        if (!TryEnable())
        {
            Debug.Log("[LIGHT] No LightSensor found");
            return false;
        }

        return LightSensor.current.lightLevel.ReadValue();
    }

}
