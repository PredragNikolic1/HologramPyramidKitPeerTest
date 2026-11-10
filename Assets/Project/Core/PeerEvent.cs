//One thing that happened on the remote link since the last frame, as reported by IRemoteLinkPlatform.
public struct PeerEvent
{
    //Kinds (keep in sync with WebRemoteLink.jslib).
    public const string LOADED = "loaded";         // a library finished loading
    public const string OPEN = "open";             // registered with the signaling server, Peer is the own ID
    public const string CONNECTED = "connected";   // messages can now be sent to Peer
    public const string DATA = "data";             // Peer sent Data
    public const string CLOSED = "closed";         // the connection to Peer ended
    public const string SIGNALING = "signaling";   // the signaling server was lost
    public const string ERROR = "error";           // Peer is the error type, Data the message

    public string Kind;
    public string Peer;
    public string Data;
}
