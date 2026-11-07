using System.Runtime.CompilerServices;
using UnityEngine;

public class SensorDebugUI
{
    private float gate;

    public void Draw(
        float peak,
        float heldPeak,
        float rms,
        float heldRms,
        float crest,
        float crestAtPeak,
        float lastDuration,
        SensorContext context,
        float gate = 0f,
        float brightness = 0f)
    {
        //keep value display logic for future debugging
        GUI.skin.label.fontSize = 50;
        GUILayout.Label($"Peak: {peak:F3}   (max 2s: {heldPeak:F3})");
        GUILayout.Label($"RMS:  {rms:F3}   (max 2s: {heldRms:F3})");
        GUILayout.Label($"Crest: {crest:F1}   (at max peak: {crestAtPeak:F1})");
        GUILayout.Label($"Last duration: {lastDuration:F3} s");
        GUILayout.Label($"is Clap: {context.HasClap}");
        GUILayout.Label($"is Blow: {context.HasBlow}");
        GUILayout.Label($"Gate: {gate:F3}");
        GUILayout.Label($"Brightness: {brightness}");
        GUILayout.Label($"Calibrating: {context.IsCalibrating}");
    }
}
