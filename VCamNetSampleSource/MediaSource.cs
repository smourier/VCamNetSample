using System;
using System.Runtime.InteropServices;
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

        public HRESULT GetEvent(uint dwFlags, out IMFMediaEvent ppEvent)
        {
            EventProvider.LogInfo($"dwFlags:{dwFlags}");
            try
            {
                lock (_lock)
                {
                    if (_queue == null)
                    {
                        ppEvent = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = _queue.Object.GetEvent(dwFlags, out ppEvent);
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

        public HRESULT BeginGetEvent(IMFAsyncCallback pCallback, object punkState)
        {
            EventProvider.LogInfo($"pCallback:{pCallback} punkState:{punkState}");
            try
            {
                lock (_lock)
                {
                    if (_queue == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = _queue.Object.BeginGetEvent(pCallback, punkState);
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

        public HRESULT EndGetEvent(IMFAsyncResult pResult, out IMFMediaEvent ppEvent)
        {
            EventProvider.LogInfo($"pResult:{pResult}");
            try
            {
                lock (_lock)
                {
                    if (_queue == null)
                    {
                        ppEvent = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = _queue.Object.EndGetEvent(pResult, out ppEvent);
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

        public HRESULT QueueEvent(uint met, Guid guidExtendedType, HRESULT hrStatus, PropVariant pvValue)
        {
            EventProvider.LogInfo($"met:{met}");
            lock (_lock)
            {
                if (_queue == null)
                {
                    EventProvider.LogInfo($"MF_E_SHUTDOWN");
                    return HRESULTS.MF_E_SHUTDOWN;
                }

                var hr = _queue.Object.QueueEventParamVar(met, guidExtendedType, hrStatus, pvValue);
                EventProvider.LogInfo($" => {hr}");
                return hr;
            }
        }

        public HRESULT GetCharacteristics(out uint pdwCharacteristics)
        {
            EventProvider.LogInfo();
            pdwCharacteristics = (uint)_MFMEDIASOURCE_CHARACTERISTICS.MFMEDIASOURCE_IS_LIVE;
            return HRESULTS.S_OK;
        }

        public HRESULT CreatePresentationDescriptor(out IMFPresentationDescriptor ppPresentationDescriptor)
        {
            EventProvider.LogInfo();
            try
            {
                lock (_lock)
                {
                    if (_presentationDescriptor == null)
                    {
                        ppPresentationDescriptor = null!;
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var hr = _presentationDescriptor.Object.Clone(out ppPresentationDescriptor);
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

        public HRESULT Start(IMFPresentationDescriptor pPresentationDescriptor, nint pguidTimeFormat, PropVariant pvarStartPosition)
        {
            try
            {
                EventProvider.LogInfo($"pPresentationDescriptor:{pPresentationDescriptor} pguidTimeFormat:{pguidTimeFormat} pvarStartPosition:{pvarStartPosition}");
                if (pguidTimeFormat != IntPtr.Zero)
                {
                    var guid = Marshal.PtrToStructure<Guid>(pguidTimeFormat);
                    if (guid != Guid.Empty)
                    {
                        EventProvider.LogInfo($"E_INVALIDARG");
                        return HRESULTS.E_INVALIDARG;
                    }
                }

                lock (_lock)
                {
                    if (_queue == null || _presentationDescriptor == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    pPresentationDescriptor.GetStreamDescriptorCount(out var count);
                    EventProvider.LogInfo($"descriptors count:{count}");
                    if (count != _streams.Length)
                    {
                        EventProvider.LogInfo($"E_INVALIDARG");
                        return HRESULTS.E_INVALIDARG;
                    }

                    var time = Functions.MFGetSystemTime();
                    for (var i = 0; i < count; i++)
                    {
                        pPresentationDescriptor.GetStreamDescriptorByIndex((uint)i, out var selected, out var descriptor).ThrowOnError();
                        descriptor.GetStreamIdentifier(out var id).ThrowOnError();

                        var index = GetStreamIndexById(id);
                        if (index < 0)
                            return HRESULTS.E_INVALIDARG;

                        _presentationDescriptor.Object.GetStreamDescriptorByIndex((uint)index, out var thisSelected, out var thisDescriptor).ThrowOnError();
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
                                _presentationDescriptor.Object.SelectStream((uint)index).ThrowOnError();
                                _queue.Object.QueueEventParamUnk((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MENewStream, Guid.Empty, HRESULTS.S_OK, _streams[index]).ThrowOnError();
                                descriptor.GetMediaTypeHandler(out var handler).ThrowOnError();
                                handler.GetCurrentMediaType(out var type).ThrowOnError();
                                _streams[index].Start(type).ThrowOnError();
                            }
                            else
                            {
                                _presentationDescriptor.Object.DeselectStream((uint)index).ThrowOnError();
                                _streams[index].Stop().ThrowOnError();
                            }
                        }
                    }

                    using var pv = new PropVariant(time);
                    _queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MESourceStopped, Guid.Empty, HRESULTS.S_OK, pv).ThrowOnError();
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
                    if (_queue == null || _presentationDescriptor == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    var time = Functions.MFGetSystemTime();
                    for (var i = 0; i < _streams.Length; i++)
                    {
                        _streams[i].Stop().ThrowOnError();
                        _presentationDescriptor.Object.DeselectStream((uint)i).ThrowOnError();
                    }

                    using var pv = new PropVariant(time);
                    _queue.Object.QueueEventParamVar((uint)__MIDL___MIDL_itf_mfobjects_0000_0012_0001.MESourceStopped, Guid.Empty, HRESULTS.S_OK, pv).ThrowOnError();
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
                    if (_queue == null)
                    {
                        EventProvider.LogInfo($"MF_E_SHUTDOWN");
                        return HRESULTS.MF_E_SHUTDOWN;
                    }

                    _queue.Object.Shutdown();
                    _streams.Dispose();
                    _presentationDescriptor = null;
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

        public HRESULT GetSourceAttributes(out IMFAttributes ppAttributes)
        {
            EventProvider.LogInfo();
            ppAttributes = this;
            var hr = HRESULTS.S_OK;
            EventProvider.LogInfo($" => {hr}");
            return hr;
        }

        public HRESULT GetStreamAttributes(uint dwStreamIdentifier, out IMFAttributes ppAttributes)
        {
            EventProvider.LogInfo($"dwStreamIdentifier:{dwStreamIdentifier}");
            try
            {
                lock (_lock)
                {
                    if (dwStreamIdentifier >= _streams.Length)
                    {
                        ppAttributes = null!;
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    var index = GetStreamIndexById(dwStreamIdentifier);
                    if (index < 0)
                    {
                        ppAttributes = null!;
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    ppAttributes = _streams[index]; ;
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

        public HRESULT SetD3DManager(object pManager)
        {
            EventProvider.LogInfo($"pManager:{pManager}");
            lock (_lock)
            {
                foreach (var stream in _streams)
                {
                    var hr = stream.Set3DManager(pManager);
                    if (!hr.IsSuccess)
                    {
                        EventProvider.LogInfo($"=> {hr}");
                        return hr;
                    }
                }
                return HRESULTS.S_OK;
            }
        }

        public HRESULT SetDefaultAllocator(uint dwOutputStreamID, object pAllocator)
        {
            EventProvider.LogInfo($"dwOutputStreamID:{dwOutputStreamID} pAllocator:{pAllocator}");
            try
            {
                lock (_lock)
                {
                    if (dwOutputStreamID >= _streams.Length)
                    {
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    var index = GetStreamIndexById(dwOutputStreamID);
                    if (index < 0)
                    {
                        EventProvider.LogInfo($"E_FAIL");
                        return HRESULTS.E_FAIL;
                    }

                    var hr = _streams[index].SetAllocator(pAllocator);
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

        public HRESULT GetService(Guid guidService, Guid riid, out object ppvObject)
        {
            //EventProvider.LogInfo($"guidService:{guidService} riid:{riid}");
            ppvObject = null!;
            return HRESULTS.E_NOINTERFACE;
        }

        public HRESULT KsProperty(ref KSIDENTIFIER Property, uint PropertyLength, nint PropertyData, uint DataLength, out uint BytesReturned)
        {
            EventProvider.LogInfo($"Property:{Property.GetMFName()} PropertyLength:{PropertyLength} DataLength:{DataLength}");
            BytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        public HRESULT KsMethod(ref KSIDENTIFIER Method, uint MethodLength, nint MethodData, uint DataLength, out uint BytesReturned)
        {
            EventProvider.LogInfo($"Method:{Method.GetMFName()} PropertyLength:{MethodLength} DataLength:{DataLength}");
            BytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        public HRESULT KsEvent(ref KSIDENTIFIER Event, uint EventLength, nint EventData, uint DataLength, out uint BytesReturned)
        {
            EventProvider.LogInfo($"Event:{Event.GetMFName()} PropertyLength:{EventLength} DataLength:{DataLength}");
            BytesReturned = 0;
            return HRESULT.FromWin32(Utilities.Constants.ERROR_SET_NOT_FOUND);
        }

        protected override void Dispose(bool disposing)
        {
            Shutdown();
            base.Dispose(disposing);
        }
    }
}
