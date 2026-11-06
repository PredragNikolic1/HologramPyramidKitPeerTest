using System;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class ShapeSwitcher : MonoBehaviour
{
    [Tooltip("Shapes placed in the scene. Only one of them is shown at a time.")]
    public List<GameObject> shapes = new List<GameObject>();

    [Tooltip("Whether the shapes spin when the scene starts.")]
    public bool spinning = true;


    [Header("Sound")]
    [Tooltip("Peak level (0 to 1) the microphone has to reach to count as a loud sound.")]
    [Range(0f, 1f)]
    public float soundThreshold = 0.4f;

    [Tooltip("Seconds after a detected sound in which no new sound is detected.")]
    public float soundCooldownSeconds = 0.3f;

    private int current = 0;

    public bool _hasSoundOccured;

     //keep value display logic for future debugging
     private LightDomainSensor _lightSensor;
     private IMicrophoneInputHandler _microphoneInputHandler;
     private float peak, rms;
     private float heldPeak, heldRms, holdTimer, crest, crestAtPeak;
     private float gate = 0.17f;  
     private bool isLoud = false;
     private float loudStartTime;
     private float lastDuration; 
     private BlowSensor _blowSensor;
     private ClapSensor _clapSensor;

    private ShapeSwitchUI _shapeSwitchUI;

     private SensorDebugUI _debugUI;

    private ShapeReactionDispatcher _dispatcher;

    private SensorContext _sensorContext;
     private bool _hasClapOccured = false;
     private bool _hasBlowOccured = false;

    private bool _cycleNextPressed = false;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidLightInputHandler _androidLightInput;
    private float luxMeasured;
#endif

    private WebLightInputHandler _webLightInput;
    private float brightness;


    public void SetShapes(List<GameObject> shapes)
    {
        if (shapes == null)
        {
            throw new ArgumentNullException(nameof(shapes), "Shape list cannot be null!");
        }
        this.shapes = shapes;
        Debug.Log($"[ShapeSwitcher] Shapes zugewiesen. Anzahl: {this.shapes.Count}");
    }

    public void StartSwitcher()
    {
        if (shapes == null || shapes.Count == 0)
        {
            Debug.LogWarning("[ShapeSwitcher] StartSwitcher aufgerufen, aber 'shapes' ist leer!");
            return;
        }
        Show(0);
        SetSpinning(spinning);
        _shapeSwitchUI = new ShapeSwitchUI(this);
        _lightSensor = new LightDomainSensor();
        _blowSensor = new BlowSensor();
        _dispatcher = new ShapeReactionDispatcher();
        _sensorContext = new SensorContext();
#if UNITY_ANDROID && !UNITY_EDITOR
        _microphoneInputHandler = new AndroidMicrophoneInputHandler();
        _androidLightInput = new AndroidLightInputHandler();
        _clapSensor = new ClapSensor(0.5f); 
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        _webLightInput = new WebLightInputHandler();
        _microphoneInputHandler = new WebMicrophoneInputHandler();
        _clapSensor = new ClapSensor(0.05f);
#endif

#if UNITY_EDITOR
        _webLightInput = new WebLightInputHandler();
        _microphoneInputHandler = new EditorMicrophoneInputHandler();
        _clapSensor = new ClapSensor(0.05f);
#endif
        _hasSoundOccured = false;
        _debugUI = new SensorDebugUI();
    }

    public void Show(int index)
    {
        if (shapes == null || shapes.Count == 0) return;

        // Sicherstellen, dass der Index innerhalb der Grenzen bleibt
        current = Mathf.Clamp(index, 0, shapes.Count - 1);

        for (int i = 0; i < shapes.Count; i++)
        {
            if (shapes[i] != null)
            {
                bool shouldBeActive = (i == current);
                shapes[i].SetActive(shouldBeActive);
            }
        }

        Debug.Log($"[ShapeSwitcher] Zeige Form Index {current}: {shapes[current].name}");
    }

    public void NextShape()
    {
        if (shapes == null || shapes.Count == 0) return;
        int nextIndex = (current + 1) % shapes.Count;
        Show(nextIndex);
    }

    public void SetSpinning(bool value)
    {
        spinning = value;

        foreach (GameObject shape in shapes)
        {
            if (shape == null) continue;

            foreach (CubeAnimation animation in shape.GetComponentsInChildren<CubeAnimation>(true))
            {
                animation.enabled = spinning;
            }
        }
    }

    public string getCurrentShapeName()
    {
        return shapes[current].name;
    }

    void OnGUI()
    {
        if (shapes == null || shapes.Count == 0 || current >= shapes.Count || shapes[current] == null)
        {
            return;
        }

        // Animation Button, nur für Formen mit mehreren Animationen (z.B. die Katze), über dem Shape Button
        AnimationCycler cycler = shapes[current].GetComponentInChildren<AnimationCycler>();

        _debugUI.Draw(peak, heldPeak, rms, heldRms, crest, crestAtPeak, lastDuration, _hasClapOccured, _hasBlowOccured, isLoud, gate, brightness);

#if UNITY_ANDROID && !UNITY_EDITOR
    GUILayout.Label($"Lux: {luxMeasured}");
#endif

        _shapeSwitchUI.Draw(cycler, spinning);
    }

    public void RunSensor()
    {
        if (_clapSensor == null || shapes == null || current >= shapes.Count || shapes[current] == null)
        {
            return;
        }

        bool hasLightTrigger = false;
        peak = _microphoneInputHandler.MeasureLoudness();
        rms = _microphoneInputHandler.GetLastRms();
        crest = peak / Mathf.Max(rms, 0.0001f);

        //keep value display logic for future debugging
        bool gateOpen = rms > gate;
        bool soundStarted = false;
        bool soundEnded = false;

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
        _hasClapOccured = _clapSensor.Detect(soundStarted, isLoud, soundEnded, peak, rms, lastDuration, gate);
        _hasBlowOccured = _blowSensor.Detect(soundStarted, isLoud, soundEnded, rms, lastDuration, gate);
        _hasSoundOccured = _hasClapOccured;

        holdTimer -= Time.deltaTime;
        if (holdTimer <= 0f) { heldPeak = 0f; heldRms = 0f; holdTimer = 2f; }
        if (peak > heldPeak)          // new loudest moment in this 2 s window
            crestAtPeak = crest;
        heldPeak = Mathf.Max(heldPeak, peak);
        heldRms  = Mathf.Max(heldRms, rms);

#if UNITY_ANDROID && !UNITY_EDITOR
        luxMeasured = _androidLightInput.MeasureLux();
        hasLightTrigger = _lightSensor.DetectLux(luxMeasured);
#endif

#if UNITY_WEBGL && UNITY_EDITOR
        brightness = _webLightInput.MeasureBrightness();
        hasLightTrigger = _lightSensor.DetectBrightness(brightness);
#endif

        _sensorContext.HasBlow = _hasBlowOccured;
        _sensorContext.HasClap = _hasClapOccured;
        _sensorContext.HasLight = hasLightTrigger;

        
        _dispatcher.Dispatch(_sensorContext, shapes[current], _cycleNextPressed);
    }
}