using System;
using System.Runtime.InteropServices;
using DirectN;
using VCamNetSampleSource.Utilities;

namespace VCamNetSampleSource
{
    public class MediaSource : MFAttributes, IMFMediaSourceEx, IMFMediaSource, IMFSampleAllocatorControl, ICustomQueryInterface, IDisposable
    {
        private readonly MediaStream[] _streams;
        private bool _disposedValue;

        public MediaSource(Activator activator)
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

                _streams[streamId].GetStreamDescriptor(out var desc).ThrowOnError();
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
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
            throw new NotImplementedException();
        }

        public HRESULT BeginGetEvent(IMFAsyncCallback pCallback, object punkState)
        {
            EventProvider.LogInfo($"pCallback:{pCallback} punkState:{punkState}");
            throw new NotImplementedException();
        }

        public HRESULT EndGetEvent(IMFAsyncResult pResult, out IMFMediaEvent ppEvent)
        {
            EventProvider.LogInfo($"pResult:{pResult}");
            throw new NotImplementedException();
        }

        public HRESULT QueueEvent(uint met, Guid guidExtendedType, HRESULT hrStatus, PropVariant pvValue)
        {
            EventProvider.LogInfo($"met:{met}");
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public HRESULT Start(IMFPresentationDescriptor pPresentationDescriptor, nint pguidTimeFormat, PropVariant pvarStartPosition)
        {
            EventProvider.LogInfo($"pPresentationDescriptor:{pPresentationDescriptor} pguidTimeFormat:{pguidTimeFormat} pvarStartPosition:{pvarStartPosition}");
            throw new NotImplementedException();
        }

        public HRESULT Stop()
        {
            EventProvider.LogInfo();
            throw new NotImplementedException();
        }

        public HRESULT Pause()
        {
            EventProvider.LogInfo();
            return HRESULTS.MF_E_INVALID_STATE_TRANSITION;
        }

        public HRESULT Shutdown()
        {
            EventProvider.LogInfo();
            Attributes.DeleteAllItems();
            return HRESULTS.S_OK;
        }

        public HRESULT GetSourceAttributes(out IMFAttributes ppAttributes)
        {
            EventProvider.LogInfo();
            throw new NotImplementedException();
        }

        public HRESULT GetStreamAttributes(uint dwStreamIdentifier, out IMFAttributes ppAttributes)
        {
            EventProvider.LogInfo($"dwStreamIdentifier:{dwStreamIdentifier}");
            throw new NotImplementedException();
        }

        public HRESULT SetD3DManager(object pManager)
        {
            EventProvider.LogInfo($"pManager:{pManager}");
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    _streams.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MediaSource()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public HRESULT SetDefaultAllocator(uint dwOutputStreamID, object pAllocator)
        {
            EventProvider.LogInfo($"dwOutputStreamID:{dwOutputStreamID} pAllocator:{pAllocator}");
            if (dwOutputStreamID >= _streams.Length)
                return HRESULTS.E_FAIL;

            var hr = _streams[dwOutputStreamID].SetAllocator(pAllocator);
            EventProvider.LogInfo($" => {hr}");
            return hr;
        }

        public HRESULT GetAllocatorUsage(uint dwOutputStreamID, out uint pdwInputStreamID, out MFSampleAllocatorUsage peUsage)
        {
            EventProvider.LogInfo($"dwOutputStreamID:{dwOutputStreamID}");
            throw new NotImplementedException();
        }
    }
}
