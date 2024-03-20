using System;
using System.Runtime.InteropServices;
using System.Threading;
using DirectN;
using VCamNetSampleSource.Utilities;
using Windows.ApplicationModel;
using Constants = VCamNetSampleSource.Utilities.Constants;

namespace VCamNetSampleSource
{
    public class MediaSource : MFAttributes, IMFMediaSourceEx, IMFSampleAllocatorControl, IMFGetService, IKsControl, ICustomQueryInterface
    {
        private readonly object _lock = new();
        private readonly MediaStream[] _streams;
        private IComObject<IMFMediaEventQueue>? _queue;
        private IComObject<IMFPresentationDescriptor>? _presentationDescriptor;

        public MediaSource()
        {
            try
            {
                // 1 stream for now
                _streams = new MediaStream[1];
                _streams[0] = new MediaStream(this, 0);

                uint streamId = 0;
                Functions.MFCreateSensorProfile(KSMedia.KSCAMERAPROFILE_Legacy, 0, null, out var legacy).ThrowOnError();
                legacy.AddProfileFilter(streamId, "((RES==;FRT<=30,1;SUT==))").ThrowOnError();

                Functions.MFCreateSensorProfile(KSMedia.KSCAMERAPROFILE_HighFrameRate, 0, null, out var high).ThrowOnError();
                high.AddProfileFilter(streamId, "((RES==;FRT>=60,1;SUT==))").ThrowOnError();

                Functions.MFCreateSensorProfileCollection(out var collection).ThrowOnError();
                collection.AddProfile(legacy).ThrowOnError();
                collection.AddProfile(high).ThrowOnError();
                SetUnknown(MFConstants.MF_DEVICEMFT_SENSORPROFILE_COLLECTION, collection).ThrowOnError();

                try
                {
                    var current = AppInfo.Current;
                    if (current != null)
                    {
                        SetString(MFConstants.MF_VIRTUALCAMERA_CONFIGURATION_APP_PACKAGE_FAMILY_NAME, current.PackageFamilyName).ThrowOnError();
                    }
                }
                catch (Exception ex)
                {
                    EventProvider.LogInfo("Not an AppX: " + ex.Message);
                }

                var descriptors = new IMFStreamDescriptor[_streams.Length];
                for (var i = 0; i < descriptors.Length; i++)
                {
                    _streams[i].GetStreamDescriptor(out descriptors[i]).ThrowOnError();
                }
                Functions.MFCreatePresentationDescriptor(descriptors.Length, descriptors, out var descriptor).ThrowOnError();
                _presentationDescriptor = new ComObject<IMFPresentationDescriptor>(descriptor);

                Functions.MFCreateEventQueue(out var queue).ThrowOnError();
                _queue = new ComObject<IMFMediaEventQueue>(queue);
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
        {
            if (iid != typeof(IKsControl).GUID && iid != typeof(IMFAttributes).GUID &&
                iid != typeof(IMFGetService).GUID && iid != typeof(IMFMediaSourceEx).GUID &&
                iid != Constants.IID_IMFDeviceSourceInternal && iid != Constants.IID_IMFDeviceSourceStatus &&
                iid != Constants.IID_IMFDeviceController && iid != Constants.IID_IMFDeviceController2)
            {
                EventProvider.LogInfo($"iid{iid:B}");
            }
            ppv = 0;
            return CustomQueryInterfaceResult.NotHandled;
        }

        private int GetStreamIndexById(uint id)
        {
            for (var i = 0; i < _streams.Length; i++)
            {
                if (_streams[i].GetStreamDescriptor(out var desc).IsError)
                    return -1;

                if (desc.GetStreamIdentifier(out var sid).IsError)
                    return -1;

                if (sid == id)
                    return i;
            }
            return -1;
        }

        public HRESULT GetEvent(uint flags, out IMFMediaEvent evt)
        {
            EventProvider.LogInfo($"flags:{flags}");
            try
            {
                lock (_lock)
                {
                    var queue = _queue;
                    if (queue == null)
                    {
                        evt = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = queue.Object.GetEvent(flags, out evt);
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
            EventProvider.LogInfo($"callback:{callback} state:{state}");
            try
            {
                lock (_lock)
                {
                    var queue = _queue;
                    if (queue == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = queue.Object.BeginGetEvent(callback, state);
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

        public HRESULT EndGetEvent(IMFAsyncResult result, out IMFMediaEvent evt)
        {
            EventProvider.LogInfo($"pResult:{result}");
            try
            {
                lock (_lock)
                {
                    var queue = _queue;
                    if (queue == null)
                    {
                        evt = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = queue.Object.EndGetEvent(result, out evt);
                    EventProvider.LogInfo($" evt:{evt} => {hr}");
                    return hr;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT QueueEvent(uint type, Guid extendedType, HRESULT hrStatus, DetachedPropVariant value)
        {
            EventProvider.LogInfo($"type:{type} value:{value}");
            lock (_lock)
            {
                var queue = _queue;
                if (queue == null)
                {
                    EventProvider.LogInfo($"MF_E_SHUTDOWN");
                    return HRESULTS.MF_E_SHUTDOWN;
                }

                var hr = queue.Object.QueueEventParamVar(type, extendedType, hrStatus, value);
                EventProvider.LogInfo($" => {hr}");
                return hr;
            }
        }

        public HRESULT GetCharacteristics(out uint characteristics)
        {
            EventProvider.LogInfo();
            characteristics = (uint)_MFMEDIASOURCE_CHARACTERISTICS.MFMEDIASOURCE_IS_LIVE;
            return HRESULTS.S_OK;
        }

        public HRESULT CreatePresentationDescriptor(out IMFPresentationDescriptor presentationDescriptor)
        {
            EventProvider.LogInfo();
            try
            {
                lock (_lock)
                {
                    if (_presentationDescriptor == null)
                    {
                        presentationDescriptor = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = _presentationDescriptor.Object.Clone(out presentationDescriptor);
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

        public HRESULT Start(IMFPresentationDescriptor presentationDescriptor, nint guidTimeFormat, DetachedPropVariant startPosition)
        {
            try
            {
                EventProvider.LogInfo($"presentationDescriptor:{presentationDescriptor} guidTimeFormat:{guidTimeFormat} startPosition:{startPosition}");
                if (guidTimeFormat != IntPtr.Zero)
                {
                    var guid = Marshal.PtrToStructure<Guid>(guidTimeFormat);
                    if (guid != Guid.Empty)
                    {
                        EventProvider.LogInfo($"E_INVALIDARG");
                        return HRESULTS.E_INVALIDARG;
                    }
                }

                lock (_lock)
                {
                    var queue = _queue;
                    var ps = _presentationDescriptor;
                    if (queue == null || ps == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    presentationDescriptor.GetStreamDescriptorCount(out var count);
                    EventProvider.LogInfo($"descriptors count:{count}");
                    if (count != _streams.Length)
                    {
                        EventProvider.LogInfo($"E_INVALIDARG");
                        return HRESULTS.E_INVALIDARG;
                    }

                    var time = Functions.MFGetSystemTime();
                    for (var i = 0; i < count; i++)
                    {
                        presentationDescriptor.GetStreamDescriptorByIndex((uint)i, out var selected, out var descriptor).ThrowOnError();
                        descriptor.GetStreamIdentifier(out var id).ThrowOnError();

                        var index = GetStreamIndexById(id);
                        if (index < 0)
                            return HRESULTS.E_INVALIDARG;

                        ps.Object.GetStreamDescriptorByIndex((uint)index, out var thisSelected, out var thisDescriptor).ThrowOnError();
                        _streams[i].GetStreamState(out var state).ThrowOnError();

                        if (thisSelected && state == _MF_STREAM_STATE.MF_STREAM_STATE_STOPPED)
                        {
                            thisSelected = false;
                        }
                        else if (!thisSelected && state != _MF_STREAM_STATE.MF_STREAM_STATE_STOPPED)
                        {
                            thisSelected = true;
                        }

                        if (selected != thisSelected)
                        {
                            if (selected)
                            {
                                ps.Object.SelectStream((uint)index).ThrowOnError();
                                queue.Object.QueueEventParamUnk((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MENewStream, Guid.Empty, HRESULTS.S_OK, _streams[index]).ThrowOnError();
                                descriptor.GetMediaTypeHandler(out var handler).ThrowOnError();
                                handler.GetCurrentMediaType(out var type).ThrowOnError();
                                _streams[index].Start(type).ThrowOnError();
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
                    queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MESourceStopped, Guid.Empty, HRESULTS.S_OK, detached).ThrowOnError();
                    return HRESULTS.S_OK;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT Stop()
        {
            try
            {
                EventProvider.LogInfo();
                lock (_lock)
                {
                    var queue = _queue;
                    var presentationDescriptor = _presentationDescriptor;
                    if (queue == null || presentationDescriptor == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var time = Functions.MFGetSystemTime();
                    for (var i = 0; i < _streams.Length; i++)
                    {
                        _streams[i].Stop().ThrowOnError();
                        presentationDescriptor.Object.DeselectStream((uint)i).ThrowOnError();
                    }

                    using var pv = new PropVariant(time);
                    var detached = pv.Detached;
                    queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MESourceStopped, Guid.Empty, HRESULTS.S_OK, detached).ThrowOnError();
                    return HRESULTS.S_OK;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT Pause()
        {
            EventProvider.LogInfo();
            return HRESULTS.MF_E_INVALID_STATE_TRANSITION;
        }

        public HRESULT Shutdown()
        {
            EventProvider.LogInfo();
            try
            {
                lock (_lock)
                {
                    var queue = _queue;
                    if (queue == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    queue.Object.Shutdown().ThrowOnError();
                    Attributes.DeleteAllItems();
                    return HRESULTS.S_OK;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT GetSourceAttributes(out IMFAttributes attributes)
        {
            EventProvider.LogInfo();
            attributes = this;
            var hr = HRESULTS.S_OK;
            EventProvider.LogInfo($" => {hr}");
            return hr;
        }

        public HRESULT GetStreamAttributes(uint streamIdentifier, out IMFAttributes attributes)
        {
            EventProvider.LogInfo($"streamIdentifier:{streamIdentifier}");
            try
            {
                lock (_lock)
                {
                    if (streamIdentifier >= _streams.Length)
                    {
                        attributes = null!;
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    var index = GetStreamIndexById(streamIdentifier);
                    if (index < 0)
                    {
                        attributes = null!;
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    attributes = _streams[index]; ;
                    var hr = HRESULTS.S_OK;
                    EventProvider.LogInfo($" => {hr}");
                    return HRESULTS.S_OK;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT SetD3DManager(object manager)
        {
            EventProvider.LogInfo($"manager:{manager}");
            try
            {
                lock (_lock)
                {
                    foreach (var stream in _streams)
                    {
                        var hr = stream.Set3DManager(manager);
                        if (!hr.IsSuccess)
                        {
                            EventProvider.LogInfo($"=> {hr}");
                            return hr;
                        }
                    }
                    return HRESULTS.S_OK;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT SetDefaultAllocator(uint outputStreamID, object allocator)
        {
            EventProvider.LogInfo($"outputStreamID:{outputStreamID} allocator:{allocator}");
            try
            {
                lock (_lock)
                {
                    if (outputStreamID >= _streams.Length)
                    {
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    var index = GetStreamIndexById(outputStreamID);
                    if (index < 0)
                    {
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    var hr = _streams[index].SetAllocator(allocator);
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

        public HRESULT GetAllocatorUsage(uint dwOutputStreamID, out uint pdwInputStreamID, out MFSampleAllocatorUsage peUsage)
        {
            EventProvider.LogInfo($"dwOutputStreamID:{dwOutputStreamID}");
            try
            {
                lock (_lock)
                {
                    if (dwOutputStreamID >= _streams.Length)
                    {
                        pdwInputStreamID = 0;
                        peUsage = 0;
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    var index = GetStreamIndexById(dwOutputStreamID);
                    if (index < 0)
                    {
                        pdwInputStreamID = 0;
                        peUsage = 0;
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    pdwInputStreamID = dwOutputStreamID;
                    peUsage = _streams[index].GetAllocatorUsage();
                    return HRESULTS.S_OK;
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT GetService(Guid guidService, Guid riid, out object ppv)
        {
            //EventProvider.LogInfo($"guidService:{guidService} riid:{riid}");
            ppv = null!;
            return HRESULTS.E_NOINTERFACE;
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
            Shutdown();
            Interlocked.Exchange(ref _presentationDescriptor!, null)?.Dispose();
            Interlocked.Exchange(ref _queue!, null)?.Dispose();
            _streams.Dispose();
            base.Dispose(disposing);
        }
    }
}
