using System;
using System.Runtime.InteropServices;
using System.Threading;
using DirectN;
using VCamNetSampleSource.Utilities;

namespace VCamNetSampleSource
{
    public class MediaStream : MFAttributes, IMFMediaStream, IDisposable
    {
        public const int NUM_IMAGE_COLS = 1280;
        public const int NUM_IMAGE_ROWS = 960;

        private readonly object _lock = new();
        private readonly uint _index;
        private bool _disposedValue;
        private IComObject<IMFMediaEventQueue>? _queue;
        private IComObject<IMFStreamDescriptor>? _descriptor;
        private IComObject<IMFVideoSampleAllocatorEx>? _allocator;
        private readonly MediaSource _source;

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

        public HRESULT SetAllocator(object allocator)
        {
            if (allocator == null)
                return HRESULTS.E_POINTER;

            if (allocator is not IMFVideoSampleAllocatorEx aex)
                return HRESULTS.E_NOINTERFACE;

            _allocator = new ComObject<IMFVideoSampleAllocatorEx>(aex);
            return HRESULTS.S_OK;
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
            EventProvider.LogInfo($"met:{met} guidExtendedType:{guidExtendedType} hrStatus:{hrStatus} pvValue:{pvValue}");
            throw new NotImplementedException();
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    Interlocked.Exchange(ref _descriptor!, null)?.Dispose();
                    Interlocked.Exchange(ref _queue!, null)?.Dispose();
                    Interlocked.Exchange(ref _allocator!, null)?.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MediaStream()
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
    }
}
