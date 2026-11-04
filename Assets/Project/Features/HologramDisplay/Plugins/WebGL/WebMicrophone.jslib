//Live microphone level for the Web build, used by SoundSensor.
//Unity's Microphone records in compressed chunks on the Web and its AudioClip.GetData does not work while recording, so the level is read from the browser's Web Audio API instead.
var WebMicrophone = {
    $WebMicrophoneState: {
        context: null,
        analyser: null,
        samples: null,
        rms: 0
    },

    WebMicrophone_Start: function () {
        if (WebMicrophoneState.analyser) {
            return;
        }

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            console.warn("[WebMicrophone] The browser does not allow microphone access. The page has to be opened over https or localhost.");
            return;
        }

        //The browser filters are turned off, they would soften or remove loud short sounds like claps.
        navigator.mediaDevices.getUserMedia({
            audio: {
                echoCancellation: false,
                noiseSuppression: false,
                autoGainControl: false
            },
            video: false
        }).then(function (stream) {
            var AudioContextType = window.AudioContext || window.webkitAudioContext;
            var context = new AudioContextType();
            var analyser = context.createAnalyser();
            analyser.fftSize = 1024;
            context.createMediaStreamSource(stream).connect(analyser);

            WebMicrophoneState.context = context;
            WebMicrophoneState.analyser = analyser;
            WebMicrophoneState.samples = new Float32Array(analyser.fftSize);

            //Browsers only run audio after the user interacted with the page, so the audio is resumed on the first click, touch or key press.
            var resume = function () {
                if (context.state !== "running") {
                    context.resume();
                }
            };
            ["click", "touchstart", "keydown"].forEach(function (eventName) {
                document.addEventListener(eventName, resume);
            });
            resume();
        }).catch(function (error) {
            console.warn("[SoundSensor] No access to the microphone: " + error);
        });
    },

    //1 if the microphone is running, 0 otherwise (no permission yet, or waiting for a click).
    WebMicrophone_IsReady: function () {
        var state = WebMicrophoneState;
        return (state.analyser && state.context.state === "running") ? 1 : 0;
    },

    //Peak level (0 to 1) of the newest samples, 0 while the microphone is not ready.
    //Also computes the RMS of the same samples, read it afterwards with WebMicrophone_GetRms.
    WebMicrophone_GetLoudness: function () {
        var analyser = WebMicrophoneState.analyser;

        if (!analyser || WebMicrophoneState.context.state !== "running") {
            WebMicrophoneState.rms = 0;
            return 0;
        }

        var samples = WebMicrophoneState.samples;
        analyser.getFloatTimeDomainData(samples);

        var loudness = 0;
        var sumOfSquares = 0;
        for (var i = 0; i < samples.length; i++) {
            loudness = Math.max(loudness, Math.abs(samples[i]));
            sumOfSquares += samples[i] * samples[i];
        }
        WebMicrophoneState.rms = Math.sqrt(sumOfSquares / samples.length);

        return loudness;
    },
    //RMS (0 to 1) of the samples read by the last WebMicrophone_GetLoudness call.
    WebMicrophone_GetRms: function () {
        return WebMicrophoneState.rms;
    }
};

autoAddDeps(WebMicrophone, "$WebMicrophoneState");
mergeInto(LibraryManager.library, WebMicrophone);
