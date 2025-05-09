namespace VCamNetSampleSourceAOT;

[GeneratedComClass]
public partial class MediaSource :
    IMFAttributes,
    IMFMediaSourceEx,
    IMFMediaSource,
    IMFMediaEventGenerator,
    IMFSampleAllocatorControl,
    IMFGetService,
    IKsControl,
    IDisposable
{
    private readonly Lock _lock = new();
    private MFAttributes _attributes; // if we derive from it, C#/WinRT doesn't see it for some reason
    private MediaStream[] _streams;
    private ComObject<IMFMediaEventQueue>? _queue;
    private ComObject<IMFPresentationDescriptor>? _presentationDescriptor;

    public MediaSource()
    {
        ComHosting.Trace();
        _attributes = new MFAttributes(nameof(MediaSource));
        _streams = new MediaStream[1];
        _streams[0] = new MediaStream(this, 0);

        uint streamId = 0;
        Functions.MFCreateSensorProfile(Constants.KSCAMERAPROFILE_Legacy, 0, PWSTR.Null, out var legacy).ThrowOnError();
        legacy.AddProfileFilter(streamId, PWSTR.From("((RES==;FRT<=30,1;SUT==))")).ThrowOnError();

        Functions.MFCreateSensorProfile(Constants.KSCAMERAPROFILE_HighFrameRate, 0, PWSTR.Null, out var high).ThrowOnError();
        high.AddProfileFilter(streamId, PWSTR.From("((RES==;FRT>=60,1;SUT==))")).ThrowOnError();

        Functions.MFCreateSensorProfileCollection(out var obj).ThrowOnError();
        using var collection = new ComObject<IMFSensorProfileCollection>(obj);
        collection.Object.AddProfile(legacy).ThrowOnError();
        collection.Object.AddProfile(high).ThrowOnError();

        _attributes.Set(Constants.MF_DEVICEMFT_SENSORPROFILE_COLLECTION, collection);

        try
        {
            var current = AppInfo.Current;
            if (current != null)
            {
                _attributes.Set(Constants.MF_VIRTUALCAMERA_CONFIGURATION_APP_PACKAGE_FAMILY_NAME, current.PackageFamilyName);
            }
        }
        catch (Exception ex)
        {
            ComHosting.Trace("Not an AppX: " + ex.Message);
        }

        using var descriptors = new ComObjectArray<IMFStreamDescriptor>(_streams.Length, false);
        for (var i = 0; i < descriptors.Length; i++)
        {
            _streams[i].GetStreamDescriptor(out var obj2).ThrowOnError();
            descriptors[i] = new ComObject<IMFStreamDescriptor>(obj2, false);
        }

        Functions.MFCreatePresentationDescriptor(descriptors.Length, descriptors.Pointer, out var obj3).ThrowOnError();
        _presentationDescriptor = new ComObject<IMFPresentationDescriptor>(obj3);

        Functions.MFCreateEventQueue(out var queue).ThrowOnError();
        _queue = new ComObject<IMFMediaEventQueue>(queue);
    }

    private int GetStreamIndexById(uint id)
    {
        for (var i = 0; i < _streams.Length; i++)
        {
            if (_streams[i].GetStreamDescriptor(out var descriptor).IsError)
                return -1;

            if (descriptor.GetStreamIdentifier(out var sid).IsError)
                return -1;

            if (sid == id)
                return i;
        }
        return -1;
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

    public HRESULT CreatePresentationDescriptor(out IMFPresentationDescriptor presentationDescriptor)
    {
        ComHosting.Trace();
        try
        {
            lock (_lock)
            {
                if (_presentationDescriptor == null)
                {
                    presentationDescriptor = null!;
                    ComHosting.Trace($"MF_E_SHUTDOWN");
                    return Constants.MF_E_SHUTDOWN;
                }

                var hr = _presentationDescriptor.Object.Clone(out presentationDescriptor);
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

    public HRESULT EndGetEvent(IMFAsyncResult result, out IMFMediaEvent evt)
    {
        //ComHosting.Trace($"pResult:{result}");
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
                //ComHosting.Trace($" evt:{evt} => {hr}");
                return hr;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT GetAllocatorUsage(uint outputStreamID, out uint inputStreamID, out MFSampleAllocatorUsage usage)
    {
        ComHosting.Trace($"dwOutputStreamID:{outputStreamID}");
        try
        {
            lock (_lock)
            {
                if (outputStreamID >= _streams.Length)
                {
                    inputStreamID = 0;
                    usage = 0;
                    ComHosting.Trace($"E_FAIL");
                    return Constants.E_FAIL;
                }

                var index = GetStreamIndexById(outputStreamID);
                if (index < 0)
                {
                    inputStreamID = 0;
                    usage = 0;
                    ComHosting.Trace($"E_FAIL");
                    return Constants.E_FAIL;
                }

                inputStreamID = outputStreamID;
                usage = MediaStream.GetAllocatorUsage();
                return Constants.S_OK;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT GetCharacteristics(out uint characteristics)
    {
        ComHosting.Trace();
        characteristics = (uint)MFMEDIASOURCE_CHARACTERISTICS.MFMEDIASOURCE_IS_LIVE;
        return Constants.S_OK;
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

    public HRESULT GetSourceAttributes(out IMFAttributes attributes)
    {
        ComHosting.Trace();
        attributes = _attributes;
        var hr = Constants.S_OK;
        ComHosting.Trace($"{Environment.NewLine}{_attributes.NativeObject.Trace(Environment.NewLine)} => {hr}");
        return hr;
    }

    public HRESULT GetStreamAttributes(uint streamIdentifier, out IMFAttributes attributes)
    {
        ComHosting.Trace($"streamIdentifier:{streamIdentifier}");
        try
        {
            lock (_lock)
            {
                if (streamIdentifier >= _streams.Length)
                {
                    attributes = null!;
                    ComHosting.Trace($"E_FAIL");
                    return Constants.E_FAIL;
                }

                var index = GetStreamIndexById(streamIdentifier);
                if (index < 0)
                {
                    attributes = null!;
                    ComHosting.Trace($"E_FAIL");
                    return Constants.E_FAIL;
                }

                attributes = _streams[index]._attributes;
                return Constants.S_OK;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT Pause()
    {
        ComHosting.Trace();
        return Constants.MF_E_INVALID_STATE_TRANSITION;
    }

    public HRESULT QueueEvent(uint type, in Guid extendedType, HRESULT hrStatus, nint value)
    {
        ComHosting.Trace($"type:{type} extendedType:{extendedType.GetConstantName()} value:{value}");
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

    public HRESULT SetD3DManager(nint manager)
    {
        ComHosting.Trace($"manager:{manager}");
        try
        {
            lock (_lock)
            {
                foreach (var stream in _streams)
                {
                    var hr = stream.Set3DManager(manager);
                    if (!hr.IsSuccess)
                    {
                        ComHosting.Trace($"=> {hr}");
                        return hr;
                    }
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

    public HRESULT SetDefaultAllocator(uint outputStreamID, nint allocator)
    {
        ComHosting.Trace($"outputStreamID:{outputStreamID} allocator:{allocator}");
        try
        {
            lock (_lock)
            {
                if (outputStreamID >= _streams.Length)
                {
                    ComHosting.Trace($"E_FAIL");
                    return Constants.E_FAIL;
                }

                var index = GetStreamIndexById(outputStreamID);
                if (index < 0)
                {
                    ComHosting.Trace($"E_FAIL");
                    return Constants.E_FAIL;
                }

                var hr = _streams[index].SetAllocator(allocator);
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

    public HRESULT Shutdown()
    {
        ComHosting.Trace();
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

                queue.Object.Shutdown().ThrowOnError();
                _attributes.NativeObject.DeleteAllItems();
                return Constants.S_OK;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public unsafe HRESULT Start(IMFPresentationDescriptor? presentationDescriptor, nint guidTimeFormat, nint startPosition)
    {
        try
        {
            ComHosting.Trace($"presentationDescriptor:{presentationDescriptor} guidTimeFormat:{guidTimeFormat} startPosition:{startPosition}");
            ArgumentNullException.ThrowIfNull(presentationDescriptor);

            if (guidTimeFormat != 0)
            {
                var format = *(Guid*)guidTimeFormat;
                if (format != Guid.Empty)
                {
                    ComHosting.Trace($"E_INVALIDARG");
                    return Constants.E_INVALIDARG;
                }
            }

            lock (_lock)
            {
                var queue = _queue;
                var ps = _presentationDescriptor;
                if (queue == null || ps == null)
                {
                    ComHosting.Trace($"MF_E_SHUTDOWN");
                    return Constants.MF_E_SHUTDOWN;
                }

                presentationDescriptor.GetStreamDescriptorCount(out var count);
                ComHosting.Trace($"descriptors count:{count}");
                if (count != _streams.Length)
                {
                    ComHosting.Trace($"E_INVALIDARG");
                    return Constants.E_INVALIDARG;
                }

                var time = Functions.MFGetSystemTime();
                for (var i = 0; i < count; i++)
                {
                    presentationDescriptor.GetStreamDescriptorByIndex((uint)i, out var selected, out var obj).ThrowOnError();
                    var descriptor = new ComObject<IMFStreamDescriptor>(obj, false);
                    descriptor.Object.GetStreamIdentifier(out var id).ThrowOnError();

                    var index = GetStreamIndexById(id);
                    if (index < 0)
                        return Constants.E_INVALIDARG;

                    ps.Object.GetStreamDescriptorByIndex((uint)index, out var thisSelected, out var obj2).ThrowOnError();
                    var thisDescriptor = new ComObject<IMFStreamDescriptor>(obj2, false);
                    _streams[i].GetStreamState(out var state).ThrowOnError();

                    if (thisSelected && state == MF_STREAM_STATE.MF_STREAM_STATE_STOPPED)
                    {
                        thisSelected = false;
                    }
                    else if (!thisSelected && state != MF_STREAM_STATE.MF_STREAM_STATE_STOPPED)
                    {
                        thisSelected = true;
                    }

                    if (selected != thisSelected)
                    {
                        if (selected)
                        {
                            ps.Object.SelectStream((uint)index).ThrowOnError();
                            DirectN.Extensions.Com.ComObject.WithComInstanceOfType<IMFMediaStream>(_streams[index], unk =>
                            {
                                queue.Object.QueueEventParamUnk((uint)MF_EVENT_TYPE.MENewStream, Guid.Empty, Constants.S_OK, unk).ThrowOnError();
                            }, createIfNeeded: true);

                            descriptor.Object.GetMediaTypeHandler(out var objHandler).ThrowOnError();
                            using var handler = new ComObject<IMFMediaTypeHandler>(objHandler);
                            handler.Object.GetCurrentMediaType(out var objType).ThrowOnError();
                            using var type = new ComObject<IMFMediaType>(objType);
                            _streams[index].Start(type.Object).ThrowOnError();
                        }
                        else
                        {
                            ps.Object.DeselectStream((uint)index).ThrowOnError();
                            _streams[index].Stop().ThrowOnError();
                        }
                    }
                }

                using var pv = new PropVariant(time);
                var detached = pv.Detached;
                queue.Object.QueueEventParamVar((uint)MF_EVENT_TYPE.MESourceStopped, Guid.Empty, Constants.S_OK, (nint)(&detached)).ThrowOnError();
                return Constants.S_OK;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public unsafe HRESULT Stop()
    {
        try
        {
            ComHosting.Trace();
            lock (_lock)
            {
                var queue = _queue;
                var presentationDescriptor = _presentationDescriptor;
                if (queue == null || presentationDescriptor == null)
                {
                    ComHosting.Trace($"MF_E_SHUTDOWN");
                    return Constants.MF_E_SHUTDOWN;
                }

                var time = Functions.MFGetSystemTime();
                for (var i = 0; i < _streams.Length; i++)
                {
                    _streams[i].Stop().ThrowOnError();
                    presentationDescriptor.Object.DeselectStream((uint)i).ThrowOnError();
                }

                using var pv = new PropVariant(time);
                var detached = pv.Detached;
                queue.Object.QueueEventParamVar((uint)MF_EVENT_TYPE.MESourceStopped, Guid.Empty, Constants.S_OK, (nint)(&detached)).ThrowOnError();
                return Constants.S_OK;
            }
        }
        catch (Exception e)
        {
            ComHosting.Trace(e.ToString());
            throw;
        }
    }

    public HRESULT GetService(in Guid guidService, in Guid riid, out nint ppvObject)
    {
        ComHosting.Trace($"guidService:{guidService.GetConstantName()} riid:{riid.GetConstantName()}");
        ppvObject = 0;
        return Constants.E_NOINTERFACE;
    }

    public HRESULT KsEvent(in KSIDENTIFIER Event, uint EventLength, nint EventData, uint DataLength, out uint BytesReturned)
    {
        ComHosting.Trace($"Source Event:{Event.GetDebugName()} Length:{EventLength} DataLength:{DataLength}");
        BytesReturned = 0;
        return HRESULT.FromWin32(WIN32_ERROR.ERROR_SET_NOT_FOUND);
    }

    public HRESULT KsMethod(in KSIDENTIFIER Method, uint MethodLength, nint MethodData, uint DataLength, out uint BytesReturned)
    {
        ComHosting.Trace($"Source Method:{Method.GetDebugName()} Length:{MethodLength} DataLength:{DataLength}");
        BytesReturned = 0;
        return HRESULT.FromWin32(WIN32_ERROR.ERROR_SET_NOT_FOUND);
    }

    public HRESULT KsProperty(in KSIDENTIFIER Property, uint PropertyLength, nint PropertyData, uint DataLength, out uint BytesReturned)
    {
        //ComHosting.Trace($"Source Property:{Property.GetDebugName()} Length:{PropertyLength} DataLength:{DataLength}");
        BytesReturned = 0;
        return HRESULT.FromWin32(WIN32_ERROR.ERROR_SET_NOT_FOUND);
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

    public void Dispose()
    {
        ComHosting.Trace();
        _streams?.Dispose();
        _attributes?.Dispose();
        Interlocked.Exchange(ref _queue, null)?.Dispose();
        Interlocked.Exchange(ref _presentationDescriptor, null)?.Dispose();
        GC.SuppressFinalize(this);
    }
}
