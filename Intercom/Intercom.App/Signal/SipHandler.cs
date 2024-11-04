using OCST.SIP;
using Org.BouncyCastle.Utilities.Encoders;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorcery.Sys;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using System.Drawing;
using System.IO;
using System.Net;
using vpxmd;

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
        _mediaSession.OnVideoSinkSample += _mediaSession_OnVideoSinkSample;
        _mediaSession.OnVideoFrameReceived += _mediaSession_OnVideoFrameReceived;
        var result = await _userAgent.Answer(_uas, _mediaSession);
        return;
    }

    private void _mediaSession_OnVideoFrameReceived(IPEndPoint arg1, uint arg2, byte[] arg3, VideoFormat arg4)
    {
        ;
    }

    private void _mediaSession_OnVideoSinkSample(byte[] sample, uint width, uint height, int stride, VideoPixelFormatsEnum pixelFormat)
    {
        ;
    }

    private static byte[] _currVideoFrame = new byte[65536];
    private static int _currVideoFramePosn = 0;
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
            //    try
            //    {
            //        var rtpPayload = rtpPacket.Payload;
            //        if ((rtpPayload[0] & 0x10) <= 0) return;
            //        RtpVP8Header vp8Header = RtpVP8Header.GetVP8Header(rtpPacket.Payload);
            //        Buffer.BlockCopy(rtpPacket.Payload, vp8Header.Length, _currVideoFrame, _currVideoFramePosn, rtpPacket.Payload.Length - vp8Header.Length);
            //        _currVideoFramePosn += rtpPacket.Payload.Length - vp8Header.Length;
            //        if (rtpPacket.Header.MarkerBit == 1)
            //        {
            //            unsafe
            //            {
            //                fixed (byte* p = _currVideoFrame)
            //                {
            //                    uint width = 0, height = 0;
            //                    byte[] i420 = null;

            //                    //Console.WriteLine($"Attempting vpx decode {_currVideoFramePosn} bytes.");
            //                    var _vpxEncoder = new VpxVideoEncoder();
            //                    int decodeResult = _vpxEncoder.de(p, _currVideoFramePosn, ref i420, ref width, ref height);

            //                    if (decodeResult != 0)
            //                    {
            //                        Console.WriteLine("VPX decode of video sample failed.");
            //                    }
            //                    else
            //                    {
            //                        //Console.WriteLine($"Video frame ready {width}x{height}.");

            //                        fixed (byte* r = i420)
            //                        {
            //                            byte[] bmp = null;
            //                            int stride = 0;
            //                            int convRes = _imgConverter.ConvertYUVToRGB(r, VideoSubTypesEnum.I420, (int)width, (int)height, VideoSubTypesEnum.BGR24, ref bmp, ref stride);

            //                            if (convRes == 0)
            //                            {
            //                                _form.BeginInvoke(new Action(() =>
            //                                {
            //                                    fixed (byte* s = bmp)
            //                                    {
            //                                        System.Drawing.Bitmap bmpImage = new System.Drawing.Bitmap((int)width, (int)height, stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, (IntPtr)s);
            //                                        _picBox.Image = bmpImage;
            //                                    }
            //                                }));
            //                            }
            //                            else
            //                            {
            //                                Console.WriteLine("Pixel format conversion of decoded sample failed.");
            //                            }
            //                        }
            //                    }
            //                }
            //            }

            //            _currVideoFramePosn = 0;
            //        }
            //    }
            //        else
            //    {
            //        Console.WriteLine("Discarding RTP packet, VP8 header Start bit not set.");
            //        Console.WriteLine($"rtp video, seqnum {rtpPacket.Header.SequenceNumber}, ts {rtpPacket.Header.Timestamp}, marker {rtpPacket.Header.MarkerBit}, payload {rtpPacket.Payload.Length}, payload[0-5] {rtpPacket.Payload.HexStr(5)}.");
            //    }
            //}
            //    catch (Exception)
            //    {


            //    }
            //}
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
