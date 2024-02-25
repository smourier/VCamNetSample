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
                return HRESULTS.MF_E_SHUTDOWN;

            if (type != null)
            {
                type.GetGUID(MFConstants.MF_MT_SUBTYPE, out _format).ThrowOnError();
                EventProvider.LogInfo("Format: ", _format.GetMFName());
            }

            // at this point, set D3D manager may have not been called
            // so we want to create a D2D1 renter target anyway
            //_generator.EnsureRenderTarget(NUM_IMAGE_COLS, NUM_IMAGE_ROWS));

            _allocator.Object.InitializeSampleAllocator(10, type).ThrowOnError();
            _queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MEStreamStarted, Guid.Empty, HRESULTS.S_OK, null).ThrowOnError();
            _state = _MF_STREAM_STATE.MF_STREAM_STATE_RUNNING;
            return HRESULTS.S_OK;
        }

        public HRESULT Stop()
        {
            if (_queue == null || _allocator == null)
                return HRESULTS.MF_E_SHUTDOWN;

            _allocator.Object.UninitializeSampleAllocator();
            _queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MEStreamStopped, Guid.Empty, HRESULTS.S_OK, null).ThrowOnError();
            _state = _MF_STREAM_STATE.MF_STREAM_STATE_STOPPED;
            return HRESULTS.S_OK;
        }

        public MFSampleAllocatorUsage GetAllocatorUsage() => MFSampleAllocatorUsage.MFSampleAllocatorUsage_UsesProvidedAllocator;
        public HRESULT SetAllocator(object allocator)
        {
            if (allocator == null)
                return HRESULTS.E_POINTER;

            if (allocator is not IMFVideoSampleAllocatorEx aex)
                return HRESULTS.E_NOINTERFACE;

            _allocator = new ComObject<IMFVideoSampleAllocatorEx>(aex);
            return HRESULTS.S_OK;
        }

        public HRESULT Set3DManager(object manager)
        {
            var hr = _allocator?.Object.SetDirectXManager(manager) ?? HRESULTS.E_FAIL;
            return hr;
        }

        public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
        {
            EventProvider.LogInfo($"iid{iid:B}");
            ppv = 0;
            return CustomQueryInterfaceResult.NotHandled;
        }

        public HRESULT GetEvent(uint dwFlags, out IMFMediaEvent ppEvent)
        {
            EventProvider.LogInfo($"dwFlags:{dwFlags}");
            lock (_lock)
            {
                if (_queue == null)
                {
                    ppEvent = null!;
                    return HRESULTS.MF_E_SHUTDOWN;
                }

                var hr = _queue.Object.GetEvent(dwFlags, out ppEvent);
                EventProvider.LogInfo($" => {hr}");
                return hr;
            }
        }

        public HRESULT BeginGetEvent(IMFAsyncCallback pCallback, object punkState)
        {
            EventProvider.LogInfo($"pCallback:{pCallback} punkState:{punkState}");
            lock (_lock)
            {
                if (_queue == null)
                    return HRESULTS.MF_E_SHUTDOWN;

                var hr = _queue.Object.BeginGetEvent(pCallback, punkState);
                EventProvider.LogInfo($" => {hr}");
                return hr;
            }
        }

        public HRESULT EndGetEvent(IMFAsyncResult pResult, out IMFMediaEvent ppEvent)
        {
            EventProvider.LogInfo($"pResult:{pResult}");
            lock (_lock)
            {
                if (_queue == null)
                {
                    ppEvent = null!;
                    return HRESULTS.MF_E_SHUTDOWN;
                }

                var hr = _queue.Object.EndGetEvent(pResult, out ppEvent);
                EventProvider.LogInfo($" => {hr}");
                return hr;
            }
        }

        public HRESULT QueueEvent(uint met, Guid guidExtendedType, HRESULT hrStatus, PropVariant pvValue)
        {
            EventProvider.LogInfo($"met:{met}");
            lock (_lock)
            {
                if (_queue == null)
                    return HRESULTS.MF_E_SHUTDOWN;

                var hr = _queue.Object.QueueEventParamVar(met, guidExtendedType, hrStatus, pvValue);
                EventProvider.LogInfo($" => {hr}");
                return hr;
            }
        }

        public HRESULT GetMediaSource(out IMFMediaSource ppMediaSource)
        {
            EventProvider.LogInfo();
            lock (_lock)
            {
                ppMediaSource = _source;
                return _source != null ? HRESULTS.S_OK : HRESULTS.MF_E_SHUTDOWN;
            }
        }

        public HRESULT GetStreamDescriptor(out IMFStreamDescriptor ppStreamDescriptor)
        {
            EventProvider.LogInfo();
            lock (_lock)
            {
                ppStreamDescriptor = _descriptor?.Object!;
                return _descriptor != null ? HRESULTS.S_OK : HRESULTS.MF_E_SHUTDOWN;
            }
        }

        public HRESULT RequestSample(object pToken)
        {
            EventProvider.LogInfo($"pToken:{pToken}");
            throw new NotImplementedException();
        }

        public HRESULT SetStreamState(_MF_STREAM_STATE value)
        {
            if (_state != value)
            {
                switch (value)
                {
                    case _MF_STREAM_STATE.MF_STREAM_STATE_STOPPED:
                        return Stop();

                    case _MF_STREAM_STATE.MF_STREAM_STATE_PAUSED:
                        if (_state != _MF_STREAM_STATE.MF_STREAM_STATE_RUNNING)
                            return HRESULTS.MF_E_INVALID_STATE_TRANSITION;

                        _state = value;
                        break;

                    case _MF_STREAM_STATE.MF_STREAM_STATE_RUNNING:
                        return Start(null);

                    default:
                        return HRESULTS.MF_E_INVALID_STATE_TRANSITION;
                }
            }
            return HRESULTS.S_OK;
        }

        public HRESULT GetStreamState(out _MF_STREAM_STATE value)
        {
            value = _state;
            return HRESULTS.S_OK;
        }

        public HRESULT KsProperty(ref KSIDENTIFIER Property, uint PropertyLength, out nint PropertyData, uint DataLength, out uint BytesReturned)
        {
            PropertyData = 0;
            BytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        public HRESULT KsMethod(ref KSIDENTIFIER Method, uint MethodLength, out nint MethodData, uint DataLength, out uint BytesReturned)
        {
            MethodData = 0;
            BytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        public HRESULT KsEvent(ref KSIDENTIFIER Event, uint EventLength, out nint EventData, uint DataLength, out uint BytesReturned)
        {
            EventData = 0;
            BytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        protected override void Dispose(bool disposing)
        {
            Interlocked.Exchange(ref _descriptor!, null)?.Dispose();
            Interlocked.Exchange(ref _queue!, null)?.Dispose();
            Interlocked.Exchange(ref _allocator!, null)?.Dispose();
            base.Dispose(disposing);
        }
    }
}
