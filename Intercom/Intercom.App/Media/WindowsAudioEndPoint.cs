﻿using NAudio.Wave;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OCST.SIP
{
    public class WindowsAudioEndPoint : IAudioSource, IAudioSink
    {
        private const int DEVICE_BITS_PER_SAMPLE = 16;
        private const int DEVICE_CHANNELS = 1;
        private const int INPUT_BUFFERS = 2;          // See https://github.com/sipsorcery/sipsorcery/pull/148.
        private const int AUDIO_SAMPLE_PERIOD_MILLISECONDS = 20;
        private const int AUDIO_INPUTDEVICE_INDEX = -1;
        private const int AUDIO_OUTPUTDEVICE_INDEX = -1;

        /// <summary>
        /// Microphone input is sampled at 8KHz.
        /// </summary>
        public readonly static AudioSamplingRatesEnum DefaultAudioSourceSamplingRate = AudioSamplingRatesEnum.Rate8KHz;

        public readonly static AudioSamplingRatesEnum DefaultAudioPlaybackRate = AudioSamplingRatesEnum.Rate8KHz;


        private WaveFormat _waveSinkFormat = new();

        private WaveFormat _waveSourceFormat = new WaveFormat(
            (int)DefaultAudioSourceSamplingRate,
            DEVICE_BITS_PER_SAMPLE,
            DEVICE_CHANNELS);

        /// <summary>
        /// Audio render device.
        /// </summary>
        private WaveOutEvent _waveOutEvent = new();

        /// <summary>
        /// Buffer for audio samples to be rendered.
        /// </summary>
        private BufferedWaveProvider? _waveProvider;

        /// <summary>
        /// Audio capture device.
        /// </summary>
        private WaveInEvent _waveInEvent = new();

        private IAudioEncoder _audioEncoder;
        private MediaFormatManager<AudioFormat> _audioFormatManager;

        private bool _disableSink;
        private int _audioOutDeviceIndex;
        private bool _disableSource;

        protected bool _isStarted;
        protected bool _isPaused;
        protected bool _isClosed;

        /// <summary>
        /// Not used by this audio source.
        /// </summary>
        public event EncodedSampleDelegate? OnAudioSourceEncodedSample;

        /// <summary>
        /// This audio source DOES NOT generate raw samples. Subscribe to the encoded samples event
        /// to get samples ready for passing to the RTP transport layer.
        /// </summary>
        [Obsolete("The audio source only generates encoded samples.")]
        public event RawAudioSampleDelegate? OnAudioSourceRawSample { add { } remove { } }

        public event SourceErrorDelegate? OnAudioSourceError;

        public event SourceErrorDelegate? OnAudioSinkError;

        /// <summary>
        /// Creates a new basic RTP session that captures and renders audio to/from the default system devices.
        /// </summary>
        /// <param name="audioEncoder">An audio encoder that can be used to encode and decode
        /// specific audio codecs.</param>
        /// <param name="externalSource">Optional. An external source to use in combination with the source
        /// provided by this end point. The application will need to signal which source is active.</param>
        /// <param name="disableSource">Set to true to disable the use of the audio source functionality, i.e.
        /// don't capture input from the microphone.</param>
        /// <param name="disableSink">Set to true to disable the use of the audio sink functionality, i.e.
        /// don't playback audio to the speaker.</param>
        public WindowsAudioEndPoint(IAudioEncoder audioEncoder,
            int audioOutDeviceIndex = AUDIO_OUTPUTDEVICE_INDEX,
            int audioInDeviceIndex = AUDIO_INPUTDEVICE_INDEX,
            bool disableSource = false,
            bool disableSink = false)
        {

            _audioFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);
            _audioEncoder = audioEncoder;

            _audioOutDeviceIndex = audioOutDeviceIndex;
            _disableSource = disableSource;
            _disableSink = disableSink;

            if (!_disableSink)
            {
                InitPlaybackDevice(_audioOutDeviceIndex, DefaultAudioPlaybackRate.GetHashCode());
            }

            if (!_disableSource)
            {
                if (WaveInEvent.DeviceCount > 0)
                {
                    if (WaveInEvent.DeviceCount > audioInDeviceIndex)
                    {
                        _waveInEvent = new WaveInEvent();
                        _waveInEvent.BufferMilliseconds = AUDIO_SAMPLE_PERIOD_MILLISECONDS;
                        _waveInEvent.NumberOfBuffers = INPUT_BUFFERS;
                        _waveInEvent.DeviceNumber = audioInDeviceIndex;
                        _waveInEvent.WaveFormat = _waveSourceFormat;
                        _waveInEvent.DataAvailable += LocalAudioSampleAvailable;
                    }
                    else
                    {
                        OnAudioSourceError?.Invoke($"The requested audio input device index {audioInDeviceIndex} exceeds the maximum index of {WaveInEvent.DeviceCount - 1}.");
                    }
                }
                else
                {
                    OnAudioSourceError?.Invoke("No audio capture devices are available.");
                }
            }
        }

        public void RestrictFormats(Func<AudioFormat, bool> filter) => _audioFormatManager.RestrictFormats(filter);
        public List<AudioFormat> GetAudioSourceFormats() => _audioFormatManager.GetSourceFormats();
        public void SetAudioSourceFormat(AudioFormat audioFormat) => _audioFormatManager.SetSelectedFormat(audioFormat);
        public List<AudioFormat> GetAudioSinkFormats() => _audioFormatManager.GetSourceFormats();

        public bool HasEncodedAudioSubscribers() => OnAudioSourceEncodedSample != null;
        public bool IsAudioSourcePaused() => _isPaused;
        public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample) =>
            throw new NotImplementedException();

        public void SetAudioSinkFormat(AudioFormat audioFormat)
        {
            _audioFormatManager.SetSelectedFormat(audioFormat);

            if (!_disableSink)
            {
                if (_waveSinkFormat.SampleRate != _audioFormatManager.SelectedFormat.ClockRate)
                {
                    // Reinitialise the audio output device.

                    InitPlaybackDevice(_audioOutDeviceIndex, _audioFormatManager.SelectedFormat.ClockRate);
                }
            }
        }

        public MediaEndPoints ToMediaEndPoints()
        {
            return new MediaEndPoints
            {
                AudioSource = (_disableSource) ? null : this,
                AudioSink = (_disableSink) ? null : this,
            };
        }

        /// <summary>
        /// Starts the media capturing/source devices.
        /// </summary>
        public Task StartAudio()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                _waveOutEvent?.Play();
                _waveInEvent?.StartRecording();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes the session.
        /// </summary>
        public Task CloseAudio()
        {
            if (!_isClosed)
            {
                _isClosed = true;

                _waveOutEvent?.Stop();

                if (_waveInEvent != null)
                {
                    _waveInEvent.DataAvailable -= LocalAudioSampleAvailable;
                    _waveInEvent.StopRecording();
                }
            }

            return Task.CompletedTask;
        }

        public Task PauseAudio()
        {
            _isPaused = true;
            _waveInEvent?.StopRecording();
            return Task.CompletedTask;
        }

        public Task ResumeAudio()
        {
            _isPaused = false;
            _waveInEvent?.StartRecording();
            return Task.CompletedTask;
        }

        private void InitPlaybackDevice(int audioOutDeviceIndex, int audioSinkSampleRate)
        {
            try
            {
                _waveOutEvent?.Stop();

                _waveSinkFormat = new WaveFormat(
                    audioSinkSampleRate,
                    DEVICE_BITS_PER_SAMPLE,
                    DEVICE_CHANNELS);

                // Playback device.
                _waveOutEvent = new WaveOutEvent();
                _waveOutEvent.DeviceNumber = audioOutDeviceIndex;
                _waveProvider = new BufferedWaveProvider(_waveSinkFormat);
                _waveProvider.DiscardOnBufferOverflow = true;
                _waveOutEvent.Init(_waveProvider);
            }
            catch (Exception excp)
            {
                OnAudioSinkError?.Invoke($"WindowsAudioEndPoint failed to initialise playback device. {excp.Message}");
            }
        }

        /// <summary>
        /// Event handler for audio sample being supplied by local capture device.
        /// </summary>
        private void LocalAudioSampleAvailable(object? sender, WaveInEventArgs args)
        {
            // Note NAudio.Wave.WaveBuffer.ShortBuffer does not take into account little endian.
            // https://github.com/naudio/NAudio/blob/master/NAudio/Wave/WaveOutputs/WaveBuffer.cs
            // WaveBuffer wavBuffer = new WaveBuffer(args.Buffer.Take(args.BytesRecorded).ToArray());
            // byte[] encodedSample = _audioEncoder.EncodeAudio(wavBuffer.ShortBuffer, _audioFormatManager.SelectedFormat);

            byte[] buffer = args.Buffer.Take(args.BytesRecorded).ToArray();
            short[] pcm = buffer.Where((x, i) => i % 2 == 0).Select((y, i) => BitConverter.ToInt16(buffer, i * 2)).ToArray();
            byte[] encodedSample = _audioEncoder.EncodeAudio(pcm, _audioFormatManager.SelectedFormat);
            OnAudioSourceEncodedSample?.Invoke((uint)encodedSample.Length, encodedSample);
        }

        /// <summary>
        /// Event handler for playing audio samples received from the remote call party.
        /// </summary>
        /// <param name="pcmSample">Raw PCM sample from remote party.</param>
        public void GotAudioSample(byte[] pcmSample)
        {
            if (_waveProvider != null)
            {
                _waveProvider.AddSamples(pcmSample, 0, pcmSample.Length);
            }
        }

        public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload)
        {
            if (_waveProvider != null && _audioEncoder != null)
            {
                var pcmSample = _audioEncoder.DecodeAudio(payload, _audioFormatManager.SelectedFormat);
                byte[] pcmBytes = pcmSample.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
                _waveProvider?.AddSamples(pcmBytes, 0, pcmBytes.Length);
            }
        }

        public Task PauseAudioSink()
        {
            return Task.CompletedTask;
        }

        public Task ResumeAudioSink()
        {
            return Task.CompletedTask;
        }

        public Task StartAudioSink()
        {
            return Task.CompletedTask;
        }

        public Task CloseAudioSink()
        {
            return Task.CompletedTask;
        }
    }
}