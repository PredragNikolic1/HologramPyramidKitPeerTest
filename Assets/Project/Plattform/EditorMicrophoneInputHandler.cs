using UnityEngine;

//Microphone input for testing in the Unity Editor. Uses Unity's Microphone class, which works on desktop but not in WebGL.
//
//Gap-free: every frame it reads ALL samples recorded since the last frame, splits them into 10 ms blocks and
//returns the loudest block. So a short clap can not fall between two frames.
//Same block size and formulas as the WebMicrophone.jslib worklet and the audio playground.
public class EditorMicrophoneInputHandler : IMicrophoneInputHandler
{
    private const int SAMPLE_RATE_IN_FREQUENCY_HZ = 44100;
    private const int CLIP_DURATION_IN_SECONDS = 1;
    private const float BLOCK_DURATION_IN_SECONDS = 0.01f;

    //After a long pause (e.g. a breakpoint), only the newest part is read, older audio is skipped.
    private const float MAX_READ_IN_SECONDS = 0.5f;

    private readonly AudioClip _mic;
    private readonly int _blockSize;
    private readonly float[] _buffer;

    private int _lastPosition;
    private float _lastPeak;
    private float _lastRms;

    public EditorMicrophoneInputHandler()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[EditorMicrophoneInputHandler] No microphone found.");
            return;
        }

        _mic = Microphone.Start(null, true, CLIP_DURATION_IN_SECONDS, SAMPLE_RATE_IN_FREQUENCY_HZ);

        if (_mic == null)
        {
            Debug.LogWarning("[EditorMicrophoneInputHandler] Microphone could not be started.");
            return;
        }

        _blockSize = Mathf.RoundToInt(_mic.frequency * BLOCK_DURATION_IN_SECONDS);
        _buffer = new float[_mic.samples];
        _lastPosition = 0;
        Debug.Log($"[EditorMicrophoneInputHandler] Started at {_mic.frequency} Hz, block {_blockSize} samples.");
    }

    public bool IsReady()
    {
        return _mic != null && Microphone.IsRecording(null);
    }

    //Returns the peak of the loudest 10 ms block since the last call and stores its RMS.
    public float MeasureLoudness()
    {
        if (!IsReady())
            return 0f;

        int position = Microphone.GetPosition(null);
        int clipSamples = _mic.samples;

        //How many new samples were recorded since the last frame (the clip loops, so wrap around)
        int newSamples = (position - _lastPosition + clipSamples) % clipSamples;

        //Too long since the last read: skip the old part, keep only the newest
        int maxSamples = Mathf.RoundToInt(_mic.frequency * MAX_READ_IN_SECONDS);
        if (newSamples > maxSamples)
        {
            _lastPosition = (position - maxSamples + clipSamples) % clipSamples;
            newSamples = maxSamples;
        }

        //Only whole blocks are read. The rest stays for the next frame.
        int blocks = newSamples / _blockSize;
        if (blocks == 0)
            return _lastPeak;   //frame shorter than one block: keep the previous values

        int samplesToRead = blocks * _blockSize;
        ReadSamples(_lastPosition, samplesToRead);
        _lastPosition = (_lastPosition + samplesToRead) % clipSamples;

        float loudestPeak = 0f;
        float loudestRms = 0f;
        for (int b = 0; b < blocks; b++)
        {
            float peak = 0f;
            float sumOfSquares = 0f;
            int start = b * _blockSize;
            for (int i = start; i < start + _blockSize; i++)
            {
                float s = _buffer[i];
                peak = Mathf.Max(peak, Mathf.Abs(s));
                sumOfSquares += s * s;
            }
            float rms = Mathf.Sqrt(sumOfSquares / _blockSize);

            if (peak > loudestPeak) loudestPeak = peak;
            if (rms > loudestRms) loudestRms = rms;
        }

        _lastPeak = loudestPeak;
        _lastRms = loudestRms;
        return _lastPeak;
    }

    public float GetLastRms()
    {
        return _lastRms;
    }

    //Copies count samples starting at startPosition into the beginning of _buffer, wrapping around the end of the clip.
    private void ReadSamples(int startPosition, int count)
    {
        int clipSamples = _mic.samples;
        int firstPart = Mathf.Min(count, clipSamples - startPosition);

        float[] part = new float[firstPart];
        _mic.GetData(part, startPosition);
        System.Array.Copy(part, 0, _buffer, 0, firstPart);

        int secondPart = count - firstPart;
        if (secondPart > 0)
        {
            float[] rest = new float[secondPart];
            _mic.GetData(rest, 0);
            System.Array.Copy(rest, 0, _buffer, firstPart, secondPart);
        }
    }
}
