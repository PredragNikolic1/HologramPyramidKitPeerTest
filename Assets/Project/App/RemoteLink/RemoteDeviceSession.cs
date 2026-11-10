//Remote side of the remote link: this device joined a host through its QR code and sends it what its own
//sensors detect. It never changes its own SensorContext.
public class RemoteDeviceSession : IRemoteSession
{
    public enum DeviceState { Loading, Connecting, Connected, Failed }

    private const float SECONDS_TO_MILLISECONDS = 1000f;
    private const int NAME_NUMBER_MIN = 10;
    private const int NAME_NUMBER_COUNT = 90;

    private readonly IRemoteLinkPlatform _platform;
    private readonly string _hostId;
    private readonly bool _isHostIdValid;

    private bool _connectRequested;
    private bool _isChannelOpen;
    private float _connectStartTime;
    private float _lastPongTime;
    private float _nextPingTime;
    private float _rttInMilliseconds = RemoteMessage.UNKNOWN_RTT;

    private int _sequence;
    private bool _pendingClap;
    private bool _pendingBlow;
    private bool _lastSentLight;
    private float _lastSensorSendTime;

    public DeviceState State { get; private set; } = DeviceState.Loading;

    public string Name { get; private set; }

    //Short text for the state, and a longer hint what to do about it.
    public string Status { get; private set; } = "Loading...";
    public string Hint { get; private set; } = "";

    //False if trying again can not help (broken join link).
    public bool CanReconnect => State == DeviceState.Failed && _isHostIdValid;

    public float RttInMilliseconds => _rttInMilliseconds;

    public RemoteDeviceSession(IRemoteLinkPlatform platform, string hostId)
    {
        _platform = platform;
        _hostId = hostId;
        _isHostIdValid = RemoteProtocol.IsValidHostId(hostId);

        if (!_isHostIdValid)
        {
            Fail("Invalid join link", "Scan the QR code on the host again.");
        }
    }

    public void OnReady()
    {
        byte[] random = new byte[1];
        _platform.FillRandom(random);
        Name = "Phone " + (NAME_NUMBER_MIN + random[0] % NAME_NUMBER_COUNT);

        Reconnect();
    }

    //The connection is opened in the next Tick, so that all timers start from the same clock.
    public void Reconnect()
    {
        if (_isHostIdValid && Name != null && State != DeviceState.Connecting && State != DeviceState.Connected)
        {
            _connectRequested = true;
        }
    }

    public void HandleEvent(PeerEvent peerEvent, float now)
    {
        if (State != DeviceState.Connecting && State != DeviceState.Connected)
        {
            return;
        }

        switch (peerEvent.Kind)
        {
            case PeerEvent.CONNECTED:
                _isChannelOpen = true;
                _lastPongTime = now;
                _nextPingTime = now + RemoteProtocol.PING_INTERVAL_IN_SECONDS;
                RemoteMessage hello = RemoteMessage.Create(RemoteMessage.HELLO);
                hello.name = Name;
                Send(hello);
                break;

            case PeerEvent.DATA:
                HandleMessage(peerEvent.Data, now);
                break;

            case PeerEvent.CLOSED:
                Fail("Disconnected", "The host closed the connection.");
                break;

            case PeerEvent.ERROR:
                //Once connected, the signaling server is no longer needed.
                bool canIgnore = State == DeviceState.Connected && RemoteProtocol.IsSignalingError(peerEvent.Peer);
                if (!canIgnore)
                {
                    Fail("Error", RemoteProtocol.DescribeError(peerEvent.Peer, peerEvent.Data));
                }
                break;
        }
    }

    public void Tick(float now)
    {
        if (_connectRequested)
        {
            _connectRequested = false;
            StartConnecting(now);
            return;
        }

        if (State == DeviceState.Connecting && now - _connectStartTime > RemoteProtocol.CONNECT_TIMEOUT_IN_SECONDS)
        {
            Fail("Could not connect",
                "The devices could not reach each other directly. Guest Wi-Fi, eduroam and some routers block " +
                "device-to-device traffic. Try a phone hotspot.");
            return;
        }

        if (!_isChannelOpen)
        {
            return;
        }

        if (now - _lastPongTime > RemoteProtocol.PEER_TIMEOUT_IN_SECONDS)
        {
            Fail("Host lost", "The host did not answer for " + RemoteProtocol.PEER_TIMEOUT_IN_SECONDS + " s.");
            return;
        }

        if (now >= _nextPingTime)
        {
            _nextPingTime = now + RemoteProtocol.PING_INTERVAL_IN_SECONDS;
            RemoteMessage ping = RemoteMessage.Create(RemoteMessage.PING);
            ping.t = (double)now * SECONDS_TO_MILLISECONDS;
            ping.rtt = _rttInMilliseconds;
            Send(ping);
        }
    }

    public void Apply(SensorContext context, float now)
    {
        if (State != DeviceState.Connected)
        {
            _pendingClap = false;
            _pendingBlow = false;
            return;
        }

        //A clap lasts one frame but messages are sent less often, so it is remembered until the next one goes out.
        _pendingClap |= context.HasClap;
        _pendingBlow |= context.HasBlow;

        bool hasNews = _pendingClap || _pendingBlow || context.HasLight != _lastSentLight;
        bool maySend = now - _lastSensorSendTime >= RemoteProtocol.MIN_SENSOR_SEND_INTERVAL_IN_SECONDS;
        if (!hasNews || !maySend)
        {
            return;
        }

        RemoteMessage message = RemoteMessage.Create(RemoteMessage.SENSOR);
        message.seq = ++_sequence;
        message.clap = _pendingClap;
        message.blow = _pendingBlow;
        message.light = context.HasLight;
        Send(message);

        _pendingClap = false;
        _pendingBlow = false;
        _lastSentLight = context.HasLight;
        _lastSensorSendTime = now;
    }

    private void StartConnecting(float now)
    {
        _isChannelOpen = false;
        _lastSentLight = false;
        _rttInMilliseconds = RemoteMessage.UNKNOWN_RTT;
        _connectStartTime = now;
        State = DeviceState.Connecting;
        Status = "Connecting...";
        Hint = "";
        _platform.Connect(_hostId, RemoteProtocol.MAX_MESSAGE_CHARS);
    }

    private void HandleMessage(string json, float now)
    {
        RemoteMessage message = RemoteMessage.Parse(json);
        if (message == null)
        {
            return;
        }

        switch (message.type)
        {
            case RemoteMessage.WELCOME:
                State = DeviceState.Connected;
                Status = "Connected as " + Name;
                Hint = "Clap, blow or shine a light on this device. The host's hologram reacts.";
                _platform.KeepScreenOn();
                break;

            case RemoteMessage.FULL:
                Fail("Host is full", "The host already has " + RemoteProtocol.MAX_REMOTES + " remotes.");
                break;

            case RemoteMessage.PONG:
                _lastPongTime = now;
                _rttInMilliseconds = (float)((double)now * SECONDS_TO_MILLISECONDS - message.t);
                break;
        }
    }

    private void Send(RemoteMessage message)
    {
        _platform.Send(_hostId, message.ToJson());
    }

    private void Fail(string status, string hint)
    {
        State = DeviceState.Failed;
        Status = status;
        Hint = hint;
        _isChannelOpen = false;
        _platform.Stop();
    }
}
