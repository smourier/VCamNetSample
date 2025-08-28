namespace VCamNetSampleSourceAOT;

[GeneratedComClass]
public partial class MediaStream : IMFAttributes, IMFMediaStream2, IMFMediaStream, IMFMediaEventGenerator, IKsControl, IDisposable
{
    public const int NUM_IMAGE_COLS = 1280;
    public const int NUM_IMAGE_ROWS = 960;
    public const int NUM_ALLOCATOR_SAMPLES = 10;

    internal MFAttributes _attributes; // if we derive from it, C#/WinRT doesn't see it for some reason
    private readonly Lock _lock = new();
    private readonly MediaSource _source;
    private IComObject<IMFMediaEventQueue>? _queue;
    private IComObject<IMFStreamDescriptor>? _descriptor;
    private IComObject<IMFVideoSampleAllocatorEx>? _allocator;
    private MF_STREAM_STATE _state;
    private Guid _format;
    private FrameGenerator _generator = new();

    public MediaStream(MediaSource source, uint index)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(source);
            _attributes = new MFAttributes(nameof(MediaStream));
            _source = source;

            _attributes.Set(Constants.MF_DEVICESTREAM_STREAM_CATEGORY, Constants.PINNAME_VIDEO_CAPTURE);
            _attributes.Set(Constants.MF_DEVICESTREAM_STREAM_ID, index);
            _attributes.Set(Constants.MF_DEVICESTREAM_FRAMESERVER_SHARED, 1u);
            _attributes.Set(Constants.MF_DEVICESTREAM_ATTRIBUTE_FRAMESOURCE_TYPES, (uint)MFFrameSourceTypes.MFFrameSourceTypes_Color);

            Functions.MFCreateEventQueue(out var queue).ThrowOnError();
            _queue = new ComObject<IMFMediaEventQueue>(queue);

            // set 1 here to force RGB32 only
            var mediaTypes = new IMFMediaType[2];

            // RGB
            Functions.MFCreateMediaType(out var rgbObj).ThrowOnError();
            using var rgbType = new ComObject<IMFMediaType>(rgbObj);
            rgbType.Set(Constants.MF_MT_MAJOR_TYPE, Constants.MFMediaType_Video);
            rgbType.Set(Constants.MF_MT_SUBTYPE, Constants.MFVideoFormat_RGB32);
            rgbType.SetSize(Constants.MF_MT_FRAME_SIZE, NUM_IMAGE_COLS, NUM_IMAGE_ROWS);
            rgbType.Set(Constants.MF_MT_DEFAULT_STRIDE, NUM_IMAGE_COLS * 4u);
            rgbType.Set(Constants.MF_MT_INTERLACE_MODE, (uint)MFVideoInterlaceMode.MFVideoInterlace_Progressive);
            rgbType.Set(Constants.MF_MT_ALL_SAMPLES_INDEPENDENT, 1u);
            rgbType.SetRatio(Constants.MF_MT_FRAME_RATE, 30, 1);
            var bitrate = NUM_IMAGE_COLS * 4 * NUM_IMAGE_ROWS * 8 * 30;
            rgbType.Set(Constants.MF_MT_AVG_BITRATE, (uint)bitrate);
            rgbType.SetRatio(Constants.MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
            mediaTypes[0] = rgbType.Object;

            // NV12
            ComObject<IMFMediaType>? nv12Type = null!;
            if (mediaTypes.Length > 1)
            {
                Functions.MFCreateMediaType(out var nv12Obj).ThrowOnError();
                nv12Type = new ComObject<IMFMediaType>(nv12Obj);
                nv12Type.Set(Constants.MF_MT_MAJOR_TYPE, Constants.MFMediaType_Video);
                nv12Type.Set(Constants.MF_MT_SUBTYPE, Constants.MFVideoFormat_NV12);
                nv12Type.SetSize(Constants.MF_MT_FRAME_SIZE, NUM_IMAGE_COLS, NUM_IMAGE_ROWS);
                nv12Type.Set(Constants.MF_MT_DEFAULT_STRIDE, (uint)(NUM_IMAGE_COLS * 3 / 2));
                nv12Type.Set(Constants.MF_MT_INTERLACE_MODE, (uint)MFVideoInterlaceMode.MFVideoInterlace_Progressive);
                nv12Type.Set(Constants.MF_MT_ALL_SAMPLES_INDEPENDENT, 1u);
                nv12Type.SetRatio(Constants.MF_MT_FRAME_RATE, 30, 1);
                bitrate = NUM_IMAGE_COLS * 3 * NUM_IMAGE_ROWS * 8 * 30 / 2;
                nv12Type.Set(Constants.MF_MT_AVG_BITRATE, (uint)bitrate);
                nv12Type.SetRatio(Constants.MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
                mediaTypes[1] = nv12Type.Object;
            }

            Functions.MFCreateStreamDescriptor(index, mediaTypes.Length(), mediaTypes, out var descriptor).ThrowOnError();
            descriptor.GetMediaTypeHandler(out var obj).ThrowOnError();
            using var handler = new ComObject<IMFMediaTypeHandler>(obj);
            handler.Object.SetCurrentMediaType(mediaTypes[0]).ThrowOnError();
            _descriptor = new ComObject<IMFStreamDescriptor>(descriptor);

            mediaTypes.Dispose();
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT Start(IMFMediaType? type)
    {
        var queue = _queue;
        var allocator = _allocator;
        if (queue == null || allocator == null)
        {
            ComHosting.Trace($"MF_E_SHUTDOWN");
            return Constants.MF_E_SHUTDOWN;
        }

        if (type != null)
        {
            allocator.Object.InitializeSampleAllocator(NUM_ALLOCATOR_SAMPLES, type).ThrowOnError();

            type.GetGUID(Constants.MF_MT_SUBTYPE, out _format).ThrowOnError();
            ComHosting.Trace("Format: " + _format.GetName());
        }

        // at this point, set D3D manager may have not been called
        // so we want to create a D2D1 renter target anyway
        _generator.EnsureRenderTarget(NUM_IMAGE_COLS, NUM_IMAGE_ROWS).ThrowOnError();

        queue.Object.QueueEventParamVar((uint)MF_EVENT_TYPE.MEStreamStarted, Guid.Empty, Constants.S_OK, 0).ThrowOnError();
        _state = MF_STREAM_STATE.MF_STREAM_STATE_RUNNING;
        ComHosting.Trace("Started");
        return Constants.S_OK;
    }

    public unsafe HRESULT Stop()
    {
        var queue = _queue;
        var allocator = _allocator;
        if (queue == null || allocator == null)
        {
            ComHosting.Trace($"MF_E_SHUTDOWN");
            return Constants.MF_E_SHUTDOWN;
        }

        allocator.Object.UninitializeSampleAllocator();
        queue.Object.QueueEventParamVar((uint)MF_EVENT_TYPE.MEStreamStopped, Guid.Empty, Constants.S_OK, 0).ThrowOnError();
        _state = MF_STREAM_STATE.MF_STREAM_STATE_STOPPED;
        return Constants.S_OK;
    }

    public static MFSampleAllocatorUsage GetAllocatorUsage() => MFSampleAllocatorUsage.MFSampleAllocatorUsage_UsesProvidedAllocator;
    public HRESULT SetAllocator(nint allocator)
    {
        if (allocator == 0)
        {
            ComHosting.Trace($"E_POINTER");
            return Constants.E_POINTER;
        }

        _allocator = DirectN.Extensions.Com.ComObject.FromPointer<IMFVideoSampleAllocatorEx>(allocator);
        if (_allocator == null)
        {
            ComHosting.Trace($"E_NOINTERFACE");
            return Constants.E_NOINTERFACE;
        }
        return Constants.S_OK;
    }

    public HRESULT Set3DManager(nint manager)
    {
        var allocator = _allocator;
        if (allocator == null)
        {
            ComHosting.Trace($"E_POINTER");
            return Constants.E_POINTER;
        }

        allocator.Object.SetDirectXManager(manager).ThrowOnError();
        return _generator.SetD3DManager(manager, NUM_IMAGE_COLS, NUM_IMAGE_ROWS);
    }

    public HRESULT BeginGetEvent(IMFAsyncCallback callback, nint state)
    {
        //ComHosting.Trace($"callback:{callback} state:{state}");
        try
        {
            lock (_lock)
            {
                var queue = _queue;
                if (queue == null)
                {
                    ComHosting.Trace($"MF_E_SHUTDOWN");
                    return Constants.MF_E_SHUTDOWN;
                }

                var hr = queue.Object.BeginGetEvent(callback, state);
                //ComHosting.Trace($" => {hr}");
                return hr;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT EndGetEvent(IMFAsyncResult result, out IMFMediaEvent evt)
    {
        //ComHosting.Trace($"result:{result}");
        try
        {
            lock (_lock)
            {
                var queue = _queue;
                if (queue == null)
                {
                    evt = null!;
                    ComHosting.Trace($"MF_E_SHUTDOWN");
                    return Constants.MF_E_SHUTDOWN;
                }

                var hr = queue.Object.EndGetEvent(result, out evt);
                //ComHosting.Trace($" evt {evt}=> {hr}");
                return hr;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT GetEvent(MEDIA_EVENT_GENERATOR_GET_EVENT_FLAGS flags, out IMFMediaEvent evt)
    {
        ComHosting.Trace($"flags:{flags}");
        try
        {
            lock (_lock)
            {
                var queue = _queue;
                if (queue == null)
                {
                    evt = null!;
                    ComHosting.Trace($"MF_E_SHUTDOWN");
                    return Constants.MF_E_SHUTDOWN;
                }

                var hr = queue.Object.GetEvent((uint)flags, out evt);
                ComHosting.Trace($" => {hr}");
                return hr;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT GetMediaSource(out IMFMediaSource mediaSource)
    {
        ComHosting.Trace();
        lock (_lock)
        {
            var source = _source;
            if (source == null)
            {
                mediaSource = null!;
                return Constants.MF_E_SHUTDOWN;
            }

            mediaSource = source;
            var hr = Constants.S_OK;
            ComHosting.Trace($" => {hr}");
            return hr;
        }
    }

    public HRESULT GetStreamDescriptor(out IMFStreamDescriptor streamDescriptor)
    {
        ComHosting.Trace();
        try
        {
            lock (_lock)
            {
                var descriptor = _descriptor;
                if (descriptor == null)
                {
                    streamDescriptor = null!;
                    return Constants.MF_E_SHUTDOWN;
                }

                streamDescriptor = descriptor.Object;
                var hr = Constants.S_OK;
                ComHosting.Trace($" => {hr}");
                return hr;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT GetStreamState(out MF_STREAM_STATE value)
    {
        value = _state;
        ComHosting.Trace($"value:{value}");
        return Constants.S_OK;
    }

    public HRESULT KsEvent(in KSIDENTIFIER Event, uint EventLength, nint EventData, uint DataLength, out uint BytesReturned)
    {
        ComHosting.Trace($"Stream Event:{Event.GetDebugName()} Length:{EventLength} DataLength:{DataLength}");
        BytesReturned = 0;
        return HRESULT.FromWin32(WIN32_ERROR.ERROR_SET_NOT_FOUND);
    }

    public HRESULT KsMethod(in KSIDENTIFIER Method, uint MethodLength, nint MethodData, uint DataLength, out uint BytesReturned)
    {
        ComHosting.Trace($"Stream Method:{Method.GetDebugName()} Length:{MethodLength} DataLength:{DataLength}");
        BytesReturned = 0;
        return HRESULT.FromWin32(WIN32_ERROR.ERROR_SET_NOT_FOUND);
    }

    public HRESULT KsProperty(in KSIDENTIFIER Property, uint PropertyLength, nint PropertyData, uint DataLength, out uint BytesReturned)
    {
        ComHosting.Trace($"Stream Property:{Property.GetDebugName()} Length:{PropertyLength} DataLength:{DataLength}");
        BytesReturned = 0;
        return HRESULT.FromWin32(WIN32_ERROR.ERROR_SET_NOT_FOUND);
    }

    public HRESULT QueueEvent(uint type, in Guid extendedType, HRESULT hrStatus, nint value)
    {
        ComHosting.Trace($"type:{type} extendedType:{extendedType.GetName()} value:{value}");
        try
        {
            lock (_lock)
            {
                var queue = _queue;
                if (queue == null)
                {
                    ComHosting.Trace($"MF_E_SHUTDOWN");
                    return Constants.MF_E_SHUTDOWN;
                }

                var hr = queue.Object.QueueEventParamVar(type, extendedType, hrStatus, value);
                ComHosting.Trace($" => {hr}");
                return hr;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT RequestSample(nint token)
    {
        //ComHosting.Trace($"token:{token}");
        try
        {
            lock (_lock)
            {
                var queue = _queue;
                var allocator = _allocator;
                if (allocator == null || queue == null)
                    return Constants.MF_E_SHUTDOWN;

                allocator.Object.AllocateSample(out var sample).ThrowOnError();

                using var inputSample = new ComObject<IMFSample>(sample);
                sample.SetSampleTime(Functions.MFGetSystemTime()).ThrowOnError();
                sample.SetSampleDuration(333333).ThrowOnError();

                using var outputSample = _generator.Generate(inputSample, _format);
                if (token != 0)
                {
                    outputSample.Object.SetUnknown(Constants.MFSampleExtension_Token, token).ThrowOnError();
                }

                DirectN.Extensions.Com.ComObject.WithComInstance(outputSample, unk =>
                {
                    queue.Object.QueueEventParamUnk((uint)MF_EVENT_TYPE.MEMediaSample, Guid.Empty, Constants.S_OK, unk).ThrowOnError();
                });

                // we must do this sometimes, otherwise the allocator gets full too early
                if (_generator.FrameCount % (NUM_ALLOCATOR_SAMPLES / 2) == 0)
                {
                    GC.Collect();
                }
                return Constants.S_OK;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT SetStreamState(MF_STREAM_STATE value)
    {
        ComHosting.Trace($"value:{value}");
        try
        {
            if (_state != value)
            {
                switch (value)
                {
                    case MF_STREAM_STATE.MF_STREAM_STATE_STOPPED:
                        return Stop();

                    case MF_STREAM_STATE.MF_STREAM_STATE_PAUSED:
                        if (_state != MF_STREAM_STATE.MF_STREAM_STATE_RUNNING)
                        {
                            ComHosting.Trace($"MF_E_INVALID_STATE_TRANSITION");
                            return Constants.MF_E_INVALID_STATE_TRANSITION;
                        }

                        _state = value;
                        break;

                    case MF_STREAM_STATE.MF_STREAM_STATE_RUNNING:
                        return Start(null);

                    default:
                        ComHosting.Trace($"MF_E_INVALID_STATE_TRANSITION");
                        return Constants.MF_E_INVALID_STATE_TRANSITION;
                }
            }
            return Constants.S_OK;
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public void Dispose()
    {
        ComHosting.Trace();
        Interlocked.Exchange(ref _attributes!, null)?.Dispose();
        Interlocked.Exchange(ref _descriptor!, null)?.Dispose();
        Interlocked.Exchange(ref _queue!, null)?.Dispose();
        Interlocked.Exchange(ref _allocator!, null)?.Dispose();
        Interlocked.Exchange(ref _generator!, null)?.Dispose();
        GC.SuppressFinalize(this);
    }

    HRESULT IMFAttributes.Compare(IMFAttributes? pTheirs, MF_ATTRIBUTES_MATCH_TYPE MatchType, out BOOL pbResult) => ((IMFAttributes)_attributes).Compare(pTheirs, MatchType, out pbResult);
    HRESULT IMFAttributes.CompareItem(in Guid guidKey, in PROPVARIANT Value, out BOOL pbResult) => ((IMFAttributes)_attributes).CompareItem(guidKey, Value, out pbResult);
    HRESULT IMFAttributes.CopyAllItems(IMFAttributes? pDest) => ((IMFAttributes)_attributes).CopyAllItems(pDest);
    HRESULT IMFAttributes.DeleteAllItems() => ((IMFAttributes)_attributes).DeleteAllItems();
    HRESULT IMFAttributes.DeleteItem(in Guid guidKey) => ((IMFAttributes)_attributes).DeleteItem(guidKey);
    HRESULT IMFAttributes.GetAllocatedBlob(in Guid guidKey, out nint ppBuf, out uint pcbSize) => ((IMFAttributes)_attributes).GetAllocatedBlob(guidKey, out ppBuf, out pcbSize);
    HRESULT IMFAttributes.GetAllocatedString(in Guid guidKey, out PWSTR ppwszValue, out uint pcchLength) => ((IMFAttributes)_attributes).GetAllocatedString(guidKey, out ppwszValue, out pcchLength);
    HRESULT IMFAttributes.GetBlob(in Guid guidKey, nint pBuf, uint cbBufSize, nint pcbBlobSize) => ((IMFAttributes)_attributes).GetBlob(guidKey, pBuf, cbBufSize, pcbBlobSize);
    HRESULT IMFAttributes.GetBlobSize(in Guid guidKey, out uint pcbBlobSize) => ((IMFAttributes)_attributes).GetBlobSize(guidKey, out pcbBlobSize);
    HRESULT IMFAttributes.GetCount(out uint pcItems) => ((IMFAttributes)_attributes).GetCount(out pcItems);
    HRESULT IMFAttributes.GetDouble(in Guid guidKey, out double pfValue) => ((IMFAttributes)_attributes).GetDouble(guidKey, out pfValue);
    HRESULT IMFAttributes.GetGUID(in Guid guidKey, out Guid pguidValue) => ((IMFAttributes)_attributes).GetGUID(guidKey, out pguidValue);
    HRESULT IMFAttributes.GetItem(in Guid guidKey, nint pValue) => ((IMFAttributes)_attributes).GetItem(guidKey, pValue);
    HRESULT IMFAttributes.GetItemByIndex(uint unIndex, out Guid pguidKey, nint pValue) => ((IMFAttributes)_attributes).GetItemByIndex(unIndex, out pguidKey, pValue);
    HRESULT IMFAttributes.GetItemType(in Guid guidKey, out MF_ATTRIBUTE_TYPE pType) => ((IMFAttributes)_attributes).GetItemType(guidKey, out pType);
    HRESULT IMFAttributes.GetString(in Guid guidKey, PWSTR pwszValue, uint cchBufSize, nint pcchLength) => ((IMFAttributes)_attributes).GetString(guidKey, pwszValue, cchBufSize, pcchLength);
    HRESULT IMFAttributes.GetStringLength(in Guid guidKey, out uint pcchLength) => ((IMFAttributes)_attributes).GetStringLength(guidKey, out pcchLength);
    HRESULT IMFAttributes.GetUINT32(in Guid guidKey, out uint punValue) => ((IMFAttributes)_attributes).GetUINT32(guidKey, out punValue);
    HRESULT IMFAttributes.GetUINT64(in Guid guidKey, out ulong punValue) => ((IMFAttributes)_attributes).GetUINT64(guidKey, out punValue);
    HRESULT IMFAttributes.GetUnknown(in Guid guidKey, in Guid riid, out nint ppv) => ((IMFAttributes)_attributes).GetUnknown(guidKey, riid, out ppv);
    HRESULT IMFAttributes.LockStore() => ((IMFAttributes)_attributes).LockStore();
    HRESULT IMFAttributes.SetBlob(in Guid guidKey, nint pBuf, uint cbBufSize) => ((IMFAttributes)_attributes).SetBlob(guidKey, pBuf, cbBufSize);
    HRESULT IMFAttributes.SetDouble(in Guid guidKey, double fValue) => ((IMFAttributes)_attributes).SetDouble(guidKey, fValue);
    HRESULT IMFAttributes.SetGUID(in Guid guidKey, in Guid guidValue) => ((IMFAttributes)_attributes).SetGUID(guidKey, guidValue);
    HRESULT IMFAttributes.SetItem(in Guid guidKey, in PROPVARIANT Value) => ((IMFAttributes)_attributes).SetItem(guidKey, Value);
    HRESULT IMFAttributes.SetString(in Guid guidKey, PWSTR wszValue) => ((IMFAttributes)_attributes).SetString(guidKey, wszValue);
    HRESULT IMFAttributes.SetUINT32(in Guid guidKey, uint unValue) => ((IMFAttributes)_attributes).SetUINT32(guidKey, unValue);
    HRESULT IMFAttributes.SetUINT64(in Guid guidKey, ulong unValue) => ((IMFAttributes)_attributes).SetUINT64(guidKey, unValue);
    HRESULT IMFAttributes.SetUnknown(in Guid guidKey, nint pUnknown) => ((IMFAttributes)_attributes).SetUnknown(guidKey, pUnknown);
    HRESULT IMFAttributes.UnlockStore() => ((IMFAttributes)_attributes).UnlockStore();
}
