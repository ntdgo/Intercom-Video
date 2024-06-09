using SIPSorcery.Media;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using System.Net;
using OCST.SIP;
using SIPSorcery.Net;

namespace Intercom.App.Signal;
public class SipHandler
{
    public SIPTransport Transport;
    private readonly SIPUserAgent _userAgent;
    private VoIPMediaSession? _mediaSession;
    private SIPServerUserAgent? _uas;

    public SipHandler()
    {
        Transport = new SIPTransport();
        var udpChannel = new SIPUDPChannel(new IPEndPoint(IPAddress.Any, 5060));
        var tcpChannel = new SIPTCPChannel(new IPEndPoint(IPAddress.Any, 5060));
        Transport.AddSIPChannel([udpChannel, tcpChannel]);
        Transport.SIPTransportRequestReceived += SipTransportRequestReceived;
        _userAgent = new SIPUserAgent(Transport, null);
    }

    private Task SipTransportRequestReceived(SIPEndPoint localSipEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
    {
        if (sipRequest.Method == SIPMethodsEnum.INFO)
        {
            var notAllowedResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.MethodNotAllowed, null);
            return Transport.SendResponseAsync(notAllowedResponse);
        }

        if (sipRequest.Header.From != null &&
            sipRequest.Header.From.FromTag != null &&
            sipRequest.Header.To != null &&
            sipRequest.Header.To.ToTag != null)
        {
            // This is an in-dialog request that will be handled directly by a user agent instance.
        }
        else if (sipRequest.Method == SIPMethodsEnum.INVITE)
        {
            _uas = _userAgent.AcceptCall(sipRequest);
            Task.Run(() => AutoAnswerCall(sipRequest));

            var uasTransaction = new UASInviteTransaction(Transport, sipRequest, null);
            var busyResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.BusyHere, null);
            uasTransaction.SendFinalResponse(busyResponse);
        }
        else
        {
            var notAllowedResponse = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.MethodNotAllowed, null);
            return Transport.SendResponseAsync(notAllowedResponse);
        }

        return Task.FromResult(0);
    }

    public async Task AutoAnswerCall(SIPRequest inviteRequest)
    {
        await Task.Delay(1000);
        _mediaSession = CreateMediaSessionAsync();
        _mediaSession.OnRtpPacketReceived += VoipMediaSession_OnRtpPacketReceived;
        var result = await _userAgent.Answer(_uas, _mediaSession);
        return;
    }

    private void VoipMediaSession_OnRtpPacketReceived(IPEndPoint remoteEndPoint, SDPMediaTypesEnum mediaType, RTPPacket rtpPacket)
    {
        if (mediaType == SDPMediaTypesEnum.audio)
        {
            var sample = rtpPacket.Payload;

            for (int index = 0; index < sample.Length; index++)
            {
                if (rtpPacket.Header.PayloadType == (int)SDPWellKnownMediaFormatsEnum.PCMA)
                {
                    short pcm = NAudio.Codecs.ALawDecoder.ALawToLinearSample(sample[index]);
                    byte[] pcmSample = new byte[] { (byte)(pcm & 0xFF), (byte)(pcm >> 8) };
                    //_speakerWriter?.Write(pcmSample);
                }
                else
                {
                    short pcm = NAudio.Codecs.MuLawDecoder.MuLawToLinearSample(sample[index]);
                    byte[] pcmSample = new byte[] { (byte)(pcm & 0xFF), (byte)(pcm >> 8) };
                    //_speakerWriter?.Write(pcmSample);
                }
                //_speakerWriter?.Flush();
            }
        }
        else if (mediaType == SDPMediaTypesEnum.video)
        {
            ;
        }
    }

    private VoIPMediaSession CreateMediaSessionAsync()
    {
        var windowsAudioEndPoint = new WindowsAudioEndPoint(new AudioEncoder(), -1, -1);
        var testPattern = new VideoTestPatternSource(new VpxVideoEncoder());
        var vp8VideoSink = new VideoEncoderEndPoint();

        var mediaEndPoints = new MediaEndPoints
        {
            AudioSink = windowsAudioEndPoint,
            AudioSource = windowsAudioEndPoint,
            VideoSink = vp8VideoSink,
            VideoSource = testPattern,
        };

        var voipMediaSession = new VoIPMediaSession(mediaEndPoints);
        voipMediaSession.AcceptRtpFromAny = true;
        return voipMediaSession;
    }
}
