//Live microphone level for the Web build, used by WebMicrophoneInputHandler.
//Unity's Microphone records in compressed chunks on the Web and its AudioClip.GetData does not work while recording,
//so the level is read from the browser's Web Audio API instead.
//
//Gap-free: an AudioWorklet runs on the audio thread and measures every 10 ms block of samples.
//Unity reads once per frame and gets the LOUDEST block since its last read, so a short clap can not fall
//between two frames. Same block size and formulas as audio-playground/index.html (keep both in sync).
var WebMicrophone = {
    $WebMicrophoneState: {
        context: null,
        ready: false,
        // loudest block since Unity's last read
        maxPeakSinceRead: 0,
        maxRmsSinceRead: 0,
        blocksSinceRead: 0,
        // values returned on the last read, reused when no new block arrived since then
        lastPeak: 0,
        lastRms: 0
    },

    WebMicrophone_Start: function () {
        var state = WebMicrophoneState;
        if (state.context) {
            return;
        }

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            console.warn("[WebMicrophone] The browser does not allow microphone access. The page has to be opened over https or localhost.");
            return;
        }

        var AudioContextType = window.AudioContext || window.webkitAudioContext;
        var context = new AudioContextType();
        state.context = context;

        if (!context.audioWorklet) {
            console.warn("[WebMicrophone] This browser does not support AudioWorklet, the microphone level is not available.");
            return;
        }

        //Runs on the audio thread: sums up blocks of 10 ms and sends peak and RMS of each block.
        var workletCode =
            "class LevelProcessor extends AudioWorkletProcessor {\n" +
            "  constructor(options) {\n" +
            "    super();\n" +
            "    this.blockSize = options.processorOptions.blockSize;\n" +
            "    this.count = 0; this.peak = 0; this.sumOfSquares = 0;\n" +
            "  }\n" +
            "  process(inputs) {\n" +
            "    var channel = inputs[0] && inputs[0][0];\n" +
            "    if (channel) {\n" +
            "      for (var i = 0; i < channel.length; i++) {\n" +
            "        var s = channel[i];\n" +
            "        var a = Math.abs(s);\n" +
            "        if (a > this.peak) this.peak = a;\n" +
            "        this.sumOfSquares += s * s;\n" +
            "        this.count++;\n" +
            "        if (this.count >= this.blockSize) {\n" +
            "          this.port.postMessage({ peak: this.peak, rms: Math.sqrt(this.sumOfSquares / this.count) });\n" +
            "          this.count = 0; this.peak = 0; this.sumOfSquares = 0;\n" +
            "        }\n" +
            "      }\n" +
            "    }\n" +
            "    return true;\n" +
            "  }\n" +
            "}\n" +
            "registerProcessor('level-processor', LevelProcessor);\n";

        var workletUrl = URL.createObjectURL(new Blob([workletCode], { type: "application/javascript" }));

        //Browsers only run audio after the user interacted with the page, so the audio is resumed on the first click, touch or key press.
        var resume = function () {
            if (context.state !== "running") {
                context.resume();
            }
        };
        ["click", "touchstart", "keydown"].forEach(function (eventName) {
            document.addEventListener(eventName, resume);
        });

        //The browser filters are turned off, they would soften or remove loud short sounds like claps.
        navigator.mediaDevices.getUserMedia({
            audio: {
                echoCancellation: false,
                noiseSuppression: false,
                autoGainControl: false
            },
            video: false
        }).then(function (stream) {
            return context.audioWorklet.addModule(workletUrl).then(function () {
                var blockSize = Math.round(context.sampleRate * 0.01);
                var source = context.createMediaStreamSource(stream);
                var levelNode = new AudioWorkletNode(context, "level-processor", {
                    processorOptions: { blockSize: blockSize }
                });

                levelNode.port.onmessage = function (event) {
                    var block = event.data;
                    if (block.peak > state.maxPeakSinceRead) state.maxPeakSinceRead = block.peak;
                    if (block.rms > state.maxRmsSinceRead) state.maxRmsSinceRead = block.rms;
                    state.blocksSinceRead++;
                };

                //A silent output keeps the audio graph running in every browser.
                var mute = context.createGain();
                mute.gain.value = 0;
                source.connect(levelNode);
                levelNode.connect(mute);
                mute.connect(context.destination);

                state.ready = true;
                resume();

                //Shows what the browser actually applied. Some browsers ignore the "off" requests.
                var settings = stream.getAudioTracks()[0].getSettings();
                console.log("[WebMicrophone] Started at " + context.sampleRate + " Hz, block " + blockSize +
                    " samples. Settings: " + JSON.stringify(settings));
            });
        }).catch(function (error) {
            console.warn("[WebMicrophone] No access to the microphone: " + error);
        });
    },

    //1 if the microphone is running, 0 otherwise (no permission yet, or waiting for a click).
    WebMicrophone_IsReady: function () {
        var state = WebMicrophoneState;
        return (state.ready && state.context && state.context.state === "running") ? 1 : 0;
    },

    //Peak (0 to 1) of the loudest 10 ms block since the last call, 0 while the microphone is not ready.
    //Also takes the RMS of the loudest block, read it afterwards with WebMicrophone_GetRms.
    WebMicrophone_GetLoudness: function () {
        var state = WebMicrophoneState;

        if (!state.ready || state.context.state !== "running") {
            state.lastPeak = 0;
            state.lastRms = 0;
            return 0;
        }

        //Only take new values if at least one block arrived since the last frame.
        //Otherwise (frame shorter than 10 ms) the previous values are returned again.
        if (state.blocksSinceRead > 0) {
            state.lastPeak = state.maxPeakSinceRead;
            state.lastRms = state.maxRmsSinceRead;
            state.maxPeakSinceRead = 0;
            state.maxRmsSinceRead = 0;
            state.blocksSinceRead = 0;
        }

        return state.lastPeak;
    },

    //RMS (0 to 1) of the loudest block from the last WebMicrophone_GetLoudness call.
    WebMicrophone_GetRms: function () {
        return WebMicrophoneState.lastRms;
    }
};

autoAddDeps(WebMicrophone, "$WebMicrophoneState");
mergeInto(LibraryManager.library, WebMicrophone);
