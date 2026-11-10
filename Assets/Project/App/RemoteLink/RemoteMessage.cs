using System;
using UnityEngine;

//One message between a remote and the host, sent as JSON. Which fields are used depends on the type.
[Serializable]
public class RemoteMessage
{
    public const string HELLO = "hello";       // remote -> host: name
    public const string WELCOME = "welcome";   // host -> remote: accepted
    public const string FULL = "full";         // host -> remote: refused, too many remotes
    public const string SENSOR = "sensor";     // remote -> host: seq, clap, blow, light
    public const string PING = "ping";         // remote -> host: t, rtt
    public const string PONG = "pong";         // host -> remote: t of the ping
    public const string COMMAND = "command";   // remote -> host: command (and spin for SET_SPIN)
    public const string STATE = "state";       // host -> remote: shape, spin, animation

    //Commands a remote can give, they do the same as the buttons on the host.
    public const string NEXT_SHAPE = "nextShape";
    public const string SET_SPIN = "setSpin";
    public const string NEXT_ANIMATION = "nextAnimation";

    public const float UNKNOWN_RTT = -1f;

    //Protocol version. Not set by default, so a received message without it is invalid.
    public int v;
    public string type;
    public string name;
    public int seq;

    //clap and blow: happened since the previous sensor message. light: is on right now.
    public bool clap;
    public bool blow;
    public bool light;

    public string command;

    //What the host shows right now. animation is empty if the shape has no animations to switch between.
    public string shape;
    public bool spin;
    public string animation;

    //Send time of a ping in milliseconds, the pong returns it unchanged.
    public double t;

    //Round trip time in milliseconds the remote measured with its previous ping.
    public float rtt = UNKNOWN_RTT;

    public static RemoteMessage Create(string type)
    {
        return new RemoteMessage { v = RemoteProtocol.VERSION, type = type };
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    //Returns a checked message, or null. Never trust what arrives over the network.
    public static RemoteMessage Parse(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Length > RemoteProtocol.MAX_MESSAGE_CHARS)
        {
            return null;
        }

        RemoteMessage message;
        try
        {
            message = JsonUtility.FromJson<RemoteMessage>(json);
        }
        catch (Exception)
        {
            //Whatever a stranger sends must not stop the frame, so every parse failure is just an invalid message.
            return null;
        }

        if (message == null || message.v != RemoteProtocol.VERSION)
        {
            return null;
        }

        switch (message.type)
        {
            case HELLO:
                message.name = RemoteProtocol.CleanName(message.name);
                return message;
            case PING:
            case PONG:
                return double.IsNaN(message.t) || double.IsInfinity(message.t) ? null : message;
            case COMMAND:
                bool isKnown = message.command == NEXT_SHAPE || message.command == SET_SPIN || message.command == NEXT_ANIMATION;
                return isKnown ? message : null;
            case STATE:
                message.shape = RemoteProtocol.CleanText(message.shape, "");
                message.animation = RemoteProtocol.CleanText(message.animation, "");
                return message;
            case WELCOME:
            case FULL:
            case SENSOR:
                return message;
            default:
                return null;
        }
    }
}
