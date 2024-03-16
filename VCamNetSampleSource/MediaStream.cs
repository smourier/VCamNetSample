using System;
using System.Runtime.InteropServices;
using System.Threading;
using DirectN;
using VCamNetSampleSource.Utilities;

namespace VCamNetSampleSource
{
    public class MediaStream : MFAttributes, IMFMediaStream2, IKsControl
    {
        public const int NUM_IMAGE_COLS = 1280;
        public const int NUM_IMAGE_ROWS = 960;

        private readonly object _lock = new();
        private readonly uint _index;
        private readonly MediaSource _source;
        private IComObject<IMFMediaEventQueue>? _queue;
        private IComObject<IMFStreamDescriptor>? _descriptor;
        private IComObject<IMFVideoSampleAllocatorEx>? _allocator;
        private _MF_STREAM_STATE _state;
        private Guid _format;
        private FrameGenerator _generator = new();

        public MediaStream(MediaSource source, uint index)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(source);
                _source = source;
                _index = index;

                SetGUID(MFConstants.MF_DEVICESTREAM_STREAM_CATEGORY, KSMedia.PINNAME_VIDEO_CAPTURE).ThrowOnError();
                SetUINT32(MFConstants.MF_DEVICESTREAM_STREAM_ID, index).ThrowOnError();
                SetUINT32(MFConstants.MF_DEVICESTREAM_FRAMESERVER_SHARED, 1).ThrowOnError();
                SetUINT32(MFConstants.MF_DEVICESTREAM_ATTRIBUTE_FRAMESOURCE_TYPES, (uint)_MFFrameSourceTypes.MFFrameSourceTypes_Color).ThrowOnError();

                Functions.MFCreateEventQueue(out var queue).ThrowOnError();
                _queue = new ComObject<IMFMediaEventQueue>(queue);

                // set 1 here to force RGB32 only
                var mediaTypes = new IMFMediaType[2];

                // RGB
                Functions.MFCreateMediaType(out var rgbType).ThrowOnError();
                rgbType.SetGUID(MFConstants.MF_MT_MAJOR_TYPE, MFConstants.MFMediaType_Video).ThrowOnError();
                rgbType.SetGUID(MFConstants.MF_MT_SUBTYPE, MFConstants.MFVideoFormat_RGB32).ThrowOnError();
                rgbType.SetSize(MFConstants.MF_MT_FRAME_SIZE, NUM_IMAGE_COLS, NUM_IMAGE_ROWS);
                rgbType.SetUINT32(MFConstants.MF_MT_DEFAULT_STRIDE, NUM_IMAGE_COLS * 4).ThrowOnError();
                rgbType.SetUINT32(MFConstants.MF_MT_INTERLACE_MODE, (uint)_MFVideoInterlaceMode.MFVideoInterlace_Progressive).ThrowOnError();
                rgbType.SetUINT32(MFConstants.MF_MT_ALL_SAMPLES_INDEPENDENT, 1).ThrowOnError();
                rgbType.SetRatio(MFConstants.MF_MT_FRAME_RATE, 30, 1);
                var bitrate = NUM_IMAGE_COLS * NUM_IMAGE_ROWS * 4 * 8 * 30;
                rgbType.SetUINT32(MFConstants.MF_MT_AVG_BITRATE, (uint)bitrate).ThrowOnError();
                rgbType.SetRatio(MFConstants.MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
                mediaTypes[0] = rgbType;

                // NV12
                if (mediaTypes.Length > 1)
                {
                    Functions.MFCreateMediaType(out var nv12Type).ThrowOnError();
                    nv12Type.SetGUID(MFConstants.MF_MT_MAJOR_TYPE, MFConstants.MFMediaType_Video).ThrowOnError();
                    nv12Type.SetGUID(MFConstants.MF_MT_SUBTYPE, MFConstants.MFVideoFormat_NV12).ThrowOnError();
                    nv12Type.SetSize(MFConstants.MF_MT_FRAME_SIZE, NUM_IMAGE_COLS, NUM_IMAGE_ROWS);
                    nv12Type.SetUINT32(MFConstants.MF_MT_DEFAULT_STRIDE, NUM_IMAGE_COLS * 3 / 2).ThrowOnError();
                    nv12Type.SetUINT32(MFConstants.MF_MT_INTERLACE_MODE, (uint)_MFVideoInterlaceMode.MFVideoInterlace_Progressive).ThrowOnError();
                    nv12Type.SetUINT32(MFConstants.MF_MT_ALL_SAMPLES_INDEPENDENT, 1).ThrowOnError();
                    nv12Type.SetRatio(MFConstants.MF_MT_FRAME_RATE, 30, 1);
                    bitrate = NUM_IMAGE_COLS * 3 * NUM_IMAGE_ROWS * 8 * 30 / 2;
                    nv12Type.SetUINT32(MFConstants.MF_MT_AVG_BITRATE, (uint)bitrate).ThrowOnError();
                    nv12Type.SetRatio(MFConstants.MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
                    mediaTypes[1] = nv12Type;
                }

                Functions.MFCreateStreamDescriptor(index, mediaTypes.Length, mediaTypes, out var descriptor).ThrowOnError();
                descriptor.GetMediaTypeHandler(out var handler).ThrowOnError();
                handler.SetCurrentMediaType(mediaTypes[0]).ThrowOnError();
                _descriptor = new ComObject<IMFStreamDescriptor>(descriptor);
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT Start(IMFMediaType? type)
        {
            if (_queue == null || _allocator == null)
            {
                EventProvider.LogInfo($"MF_E_SHUTDOWN");
                return HRESULTS.MF_E_SHUTDOWN;
            }

            if (type != null)
            {
                type.GetGUID(MFConstants.MF_MT_SUBTYPE, out _format).ThrowOnError();
                EventProvider.LogInfo("Format: " + _format.GetMFName());
            }

            // at this point, set D3D manager may have not been called
            // so we want to create a D2D1 renter target anyway
            _generator.EnsureRenderTarget(NUM_IMAGE_COLS, NUM_IMAGE_ROWS).ThrowOnError();

            // note the 200 here vs 10 with native version
            _allocator.Object.InitializeSampleAllocator(200, type).ThrowOnError();
            _queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MEStreamStarted, Guid.Empty, HRESULTS.S_OK, null).ThrowOnError();
            _state = _MF_STREAM_STATE.MF_STREAM_STATE_RUNNING;
            EventProvider.LogInfo("init ok");
            return HRESULTS.S_OK;
        }

        public HRESULT Stop()
        {
            if (_queue == null || _allocator == null)
            {
                EventProvider.LogInfo($"MF_E_SHUTDOWN");
                return HRESULTS.MF_E_SHUTDOWN;
            }

            _allocator.Object.UninitializeSampleAllocator();
            _queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MEStreamStopped, Guid.Empty, HRESULTS.S_OK, null).ThrowOnError();
            _state = _MF_STREAM_STATE.MF_STREAM_STATE_STOPPED;
            return HRESULTS.S_OK;
        }

        public MFSampleAllocatorUsage GetAllocatorUsage() => MFSampleAllocatorUsage.MFSampleAllocatorUsage_UsesProvidedAllocator;
        public HRESULT SetAllocator(object allocator)
        {
            if (allocator == null)
            {
                EventProvider.LogInfo($"E_POINTER");
                return HRESULTS.E_POINTER;
            }

            if (allocator is not IMFVideoSampleAllocatorEx aex)
            {
                EventProvider.LogInfo($"E_NOINTERFACE");
                return HRESULTS.E_NOINTERFACE;
            }

            _allocator = new ComObject<IMFVideoSampleAllocatorEx>(aex);
            return HRESULTS.S_OK;
        }

        public HRESULT Set3DManager(object manager)
        {
            if (_allocator == null)
            {
                EventProvider.LogInfo($"E_POINTER");
                return HRESULTS.E_POINTER;
            }

            _allocator.Object.SetDirectXManager(manager).ThrowOnError();
            var hr = _generator.SetD3DManager(manager, NUM_IMAGE_COLS, NUM_IMAGE_ROWS);
            return hr;
        }

        public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
        {
            EventProvider.LogInfo($"iid{iid:B}");
            ppv = 0;
            return CustomQueryInterfaceResult.NotHandled;
        }

        public HRESULT GetEvent(uint flags, out IMFMediaEvent evt)
        {
            EventProvider.LogInfo($"flags:{flags}");
            try
            {
                lock (_lock)
                {
                    if (_queue == null)
                    {
                        evt = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = _queue.Object.GetEvent(flags, out evt);
                    EventProvider.LogInfo($" => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT BeginGetEvent(IMFAsyncCallback callback, object state)
        {
            //EventProvider.LogInfo($"pCallback:{pCallback} punkState:{punkState}");
            try
            {
                lock (_lock)
                {
                    if (_queue == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = _queue.Object.BeginGetEvent(callback, state);
                    //EventProvider.LogInfo($" => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT EndGetEvent(IMFAsyncResult result, out IMFMediaEvent evt)
        {
            //EventProvider.LogInfo($"pResult:{pResult}");
            try
            {
                lock (_lock)
                {
                    if (_queue == null)
                    {
                        evt = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = _queue.Object.EndGetEvent(result, out evt);
                    //EventProvider.LogInfo($" => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT QueueEvent(uint type, Guid extendedType, HRESULT hrStatus, PropVariant value)
        {
            EventProvider.LogInfo($"type:{type}");
            try
            {
                lock (_lock)
                {
                    if (_queue == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = _queue.Object.QueueEventParamVar(type, extendedType, hrStatus, value);
                    EventProvider.LogInfo($" => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT GetMediaSource(out IMFMediaSource ppMediaSource)
        {
            EventProvider.LogInfo();
            lock (_lock)
            {
                if (_source == null)
                {
                    ppMediaSource = null!;
                    return HRESULTS.MF_E_SHUTDOWN;
                }

                ppMediaSource = _source;
                var hr = HRESULTS.S_OK;
                EventProvider.LogInfo($" => {hr}");
                return hr;
            }
        }

        public HRESULT GetStreamDescriptor(out IMFStreamDescriptor streamDescriptor)
        {
            EventProvider.LogInfo();
            try
            {
                lock (_lock)
                {
                    if (_descriptor == null)
                    {
                        streamDescriptor = null!;
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    streamDescriptor = _descriptor.Object;
                    var hr = HRESULTS.S_OK;
                    EventProvider.LogInfo($" => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT RequestSample(object token)
        {
            EventProvider.LogInfo($"token:{token}");
            try
            {
                lock (_lock)
                {
                    if (_allocator == null || _queue == null)
                        return HRESULTS.MF_E_SHUTDOWN;

                    var h = _allocator.Object.AllocateSample(out var sample);
                    if (h == HRESULTS.MF_E_SAMPLEALLOCATOR_EMPTY)
                    {
                        EventProvider.LogInfo("MF_E_SAMPLEALLOCATOR_EMPTY");
                        return h;
                    }

                    h.ThrowOnError();
                    using (var input = new ComObject<IMFSample>(sample))
                    {
                        sample.SetSampleTime(Functions.MFGetSystemTime()).ThrowOnError();
                        sample.SetSampleDuration(333333).ThrowOnError();

                        using (var output = _generator.Generate(input, _format))
                        {
                            if (token != null)
                            {
                                output.Object.SetUnknown(MFConstants.MFSampleExtension_Token, token).ThrowOnError();
                            }

                            _queue.Object.QueueEventParamUnk((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MEMediaSample, Guid.Empty, HRESULTS.S_OK, output.Object).ThrowOnError();

                            //Marshal.GetIUnknownForObject(osm.Object);
                            //Marshal.GetIUnknownForObject(osm.Object);

                            //_queue.Object.QueueEventParamUnk((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MEMediaSample, Guid.Empty, HRESULTS.S_OK, sample).ThrowOnError();
                            //EventProvider.LogInfo($"refI: {ComObject.GetRefCount(sample)}");
                            //EventProvider.LogInfo($"refO: {ComObject.GetRefCount(output)}");
                            //if (token != null)
                            //{
                            //    sample.SetUnknown(MFConstants.MFSampleExtension_Token, token).ThrowOnError();
                            //}
                        }
                    }

                    var hr = HRESULTS.S_OK;
                    EventProvider.LogInfo($" tok {token} => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT SetStreamState(_MF_STREAM_STATE value)
        {
            EventProvider.LogInfo($"value:{value}");
            try
            {
                if (_state != value)
                {
                    switch (value)
                    {
                        case _MF_STREAM_STATE.MF_STREAM_STATE_STOPPED:
                            return Stop();

                        case _MF_STREAM_STATE.MF_STREAM_STATE_PAUSED:
                            if (_state != _MF_STREAM_STATE.MF_STREAM_STATE_RUNNING)
                            {
                                EventProvider.LogInfo($"MF_E_INVALID_STATE_TRANSITION");
                                return HRESULTS.MF_E_INVALID_STATE_TRANSITION;
                            }

                            _state = value;
                            break;

                        case _MF_STREAM_STATE.MF_STREAM_STATE_RUNNING:
                            return Start(null);

                        default:
                            EventProvider.LogInfo($"MF_E_INVALID_STATE_TRANSITION");
                            return HRESULTS.MF_E_INVALID_STATE_TRANSITION;
                    }
                }
                return HRESULTS.S_OK;
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT GetStreamState(out _MF_STREAM_STATE value)
        {
            value = _state;
            EventProvider.LogInfo($"value:{value}");
            return HRESULTS.S_OK;
        }

        public HRESULT KsProperty(ref KSIDENTIFIER roperty, uint propertyLength, nint propertyData, uint dataLength, out uint bytesReturned)
        {
            EventProvider.LogInfo($"Property:{roperty.GetMFName()} PropertyLength:{propertyLength} DataLength:{dataLength}");
            bytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        public HRESULT KsMethod(ref KSIDENTIFIER method, uint methodLength, nint methodData, uint dataLength, out uint bytesReturned)
        {
            EventProvider.LogInfo($"Method:{method.GetMFName()} PropertyLength:{methodLength} DataLength:{dataLength}");
            bytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        public HRESULT KsEvent(ref KSIDENTIFIER evt, uint eventLength, nint eventData, uint dataLength, out uint bytesReturned)
        {
            EventProvider.LogInfo($"Event:{evt.GetMFName()} PropertyLength:{eventLength} DataLength:{dataLength}");
            bytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        protected override void Dispose(bool disposing)
        {
            EventProvider.LogInfo();
            Interlocked.Exchange(ref _descriptor!, null)?.Dispose();
            Interlocked.Exchange(ref _queue!, null)?.Dispose();
            Interlocked.Exchange(ref _allocator!, null)?.Dispose();
            Interlocked.Exchange(ref _generator!, null)?.Dispose();
            base.Dispose(disposing);
        }
    }
}
