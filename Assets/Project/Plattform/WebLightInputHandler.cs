using System;
using UnityEngine;

public class WebLightInputHandler
{
    private const int REQUESTED_WIDTH = 160;
    private const int REQUESTED_HEIGHT = 120;
    private const int REQUESTED_FPS = 15;

    private WebCamTexture _camera;
    private Color32[] _pixels;
    private bool _cameraStartedSuccessfully = false;
    private float _lastBrightness = 0f;

    public bool IsReady => _camera != null && _camera.isPlaying;

    public WebLightInputHandler()
    {
        //The browser shows the camera permission prompt.
        Application.RequestUserAuthorization(UserAuthorization.WebCam);
    }

    private void StartCamera()
    {
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogWarning("[WebLightInputHandler] No camera found, light is not detected.");
            return;
        }

        //Prefer the front camera, it sits next to where the light sensor is on the tablet.
        string deviceName = WebCamTexture.devices[0].name;
        foreach (WebCamDevice device in WebCamTexture.devices)
        {
            if (device.isFrontFacing)
            {
                deviceName = device.name;
                break;
            }
        }

        //Small resolution, reading the pixels every frame stays cheap.
        _camera = new WebCamTexture(deviceName, REQUESTED_WIDTH, REQUESTED_HEIGHT, REQUESTED_FPS);
        _camera.Play();
        _cameraStartedSuccessfully = true;
        Debug.Log("[WebLightInputHandler] Camera started: " + deviceName);
    }

    //Average brightness of the camera image, 0 (black) to 1 (white).
    public float MeasureBrightness()
    {
        if (!_cameraStartedSuccessfully && Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            StartCamera();
        }

        //The camera delivers fewer frames than the game renders, keep the last value in between.
        if (_camera == null || !_camera.didUpdateThisFrame)
        {
            return _lastBrightness;
        }

        //The real image size is only known once frames arrive, so the buffer is matched to it.
        int pixelCount = _camera.width * _camera.height;
        if (_pixels == null || _pixels.Length != pixelCount)
        {
            _pixels = new Color32[pixelCount];
        }

        _camera.GetPixels32(_pixels);

        float sum = 0f;
        foreach (Color32 p in _pixels)
        {
            sum += 0.299f * p.r + 0.587f * p.g + 0.114f * p.b;
        }
        _lastBrightness = sum / _pixels.Length / 255f;

        return _lastBrightness;
    }

    public void Stop()
    {
        if (_camera != null && _camera.isPlaying)
        {
            _camera.Stop();
        }
    }
}
