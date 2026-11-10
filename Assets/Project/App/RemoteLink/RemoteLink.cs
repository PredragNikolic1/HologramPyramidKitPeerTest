//Lets other devices act as extra sensors for this hologram, over a direct browser-to-browser connection.
//
//Opened normally, the device is the host: it shows a QR code and merges what the remotes detect into its own
//SensorContext. Opened through that QR code (?join=<hostId>), the device is a remote: it runs its own sensors
//and sends the results to the host.
//
//Called from RunSensor(): ProcessIncoming at the start of the frame, Apply after the local sensors.
public class RemoteLink
{
    private const string LIBRARY_FOLDER = "RemoteLink";
    private const string PEER_LIBRARY = "peerjs-1.5.4.min.js";
    private const string QR_LIBRARY = "qrcode-generator-1.4.4.js";

    private readonly IRemoteLinkPlatform _platform;
    private readonly IRemoteSession _session;
    private bool _isReady;

    //Exactly one of the two is set.
    public RemoteHostSession Host { get; }
    public RemoteDeviceSession Device { get; }

    //False while the libraries are still loading.
    public bool IsReady => _isReady;

    //Set when the libraries could not be loaded, null otherwise.
    public string LoadError { get; private set; }

    public RemoteLink(IRemoteLinkPlatform platform, string pageUrl, string streamingAssetsUrl)
    {
        _platform = platform;

        string joinId = RemoteProtocol.ReadJoinId(pageUrl);
        if (joinId == null)
        {
            Host = new RemoteHostSession(platform, pageUrl);
            _session = Host;
        }
        else
        {
            Device = new RemoteDeviceSession(platform, joinId);
            _session = Device;
        }

        string libraryUrl = streamingAssetsUrl + "/" + LIBRARY_FOLDER + "/";
        _platform.LoadScript(libraryUrl + PEER_LIBRARY);
        _platform.LoadScript(libraryUrl + QR_LIBRARY);
    }

    public void ProcessIncoming(float now)
    {
        //The platform keeps only a limited number of events, so this loop is short even if a remote floods messages.
        while (_platform.TryPollEvent(out PeerEvent peerEvent))
        {
            if (peerEvent.Kind == PeerEvent.LOADED)
            {
                CheckReady();
            }
            else if (peerEvent.Kind == PeerEvent.ERROR && peerEvent.Peer == RemoteProtocol.ERROR_LIBRARY)
            {
                LoadError = RemoteProtocol.DescribeError(peerEvent.Peer, peerEvent.Data);
            }
            else
            {
                _session.HandleEvent(peerEvent, now);
            }
        }

        _session.Tick(now);
    }

    public void Apply(SensorContext context, float now)
    {
        _session.Apply(context, now);
    }

    private void CheckReady()
    {
        if (_isReady || !_platform.IsReady())
        {
            return;
        }
        _isReady = true;
        _session.OnReady();
    }
}
