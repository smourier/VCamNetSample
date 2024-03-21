using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DirectN;
using VCamNetSampleSource.Utilities;

namespace VCamNetSampleSource
{
    public class FrameGenerator : IDisposable
    {
        public const float DIVISOR = 20;

        private bool _disposedValue;
        private uint _width;
        private uint _height;
        private ulong _frameCount;
        private long _prevTime;
        private uint _fps;
        private IntPtr _deviceHandle;
        private IComObject<ID3D11Texture2D>? _texture;
        private IComObject<ID2D1RenderTarget>? _renderTarget;
        private IComObject<ID2D1SolidColorBrush>? _whiteBrush;
        private IComObject<ID2D1SolidColorBrush>[]? _blockBrushes;
        private IComObject<IDWriteTextFormat>? _textFormat;
        private IComObject<IDWriteFactory>? _dwrite;
        private IComObject<IMFTransform>? _converter;
        private IComObject<IWICBitmap>? _bitmap;
        private IComObject<IMFDXGIDeviceManager>? _dxgiManager;

        public bool HasD3DManager => _texture != null;
        public ulong FrameCount => _frameCount;

        // common to CPU & GPU
        private HRESULT CreateRenderTargetResources(uint width, uint height)
        {
            if (_renderTarget == null)
                return HRESULTS.E_FAIL;

            _whiteBrush = _renderTarget.CreateSolidColorBrush(new _D3DCOLORVALUE(1, 1, 1, 1));
            _dwrite = DWriteFunctions.DWriteCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED);
            _textFormat = _dwrite.CreateTextFormat("Segoe UI", 40);
            _textFormat.Object.SetParagraphAlignment(DWRITE_PARAGRAPH_ALIGNMENT.DWRITE_PARAGRAPH_ALIGNMENT_CENTER);
            _textFormat.Object.SetTextAlignment(DWRITE_TEXT_ALIGNMENT.DWRITE_TEXT_ALIGNMENT_CENTER);

            // colors for HSL blocks
            var brushes = new List<IComObject<ID2D1SolidColorBrush>>();
            for (var i = 0; i < width / DIVISOR; i++)
            {
                for (var j = 0; j < height / DIVISOR; j++)
                {
                    var color = Utilities.Extensions.HSL2RGB(i / (height / DIVISOR), 1, (j / (width / DIVISOR)));
                    brushes.Add(_renderTarget.CreateSolidColorBrush(color));
                }
            }

            _blockBrushes = brushes.ToArray();
            _width = width;
            _height = height;
            return HRESULTS.S_OK;
        }

        private void SetConverterTypes(uint width, uint height)
        {
            Functions.MFCreateMediaType(out var inputType).ThrowOnError();
            inputType.SetGUID(MFConstants.MF_MT_MAJOR_TYPE, MFConstants.MFMediaType_Video).ThrowOnError();
            inputType.SetGUID(MFConstants.MF_MT_SUBTYPE, MFConstants.MFVideoFormat_RGB32).ThrowOnError();
            inputType.SetSize(MFConstants.MF_MT_FRAME_SIZE, width, height);
            _converter!.Object.SetInputType(0, inputType, 0).ThrowOnError();

            Functions.MFCreateMediaType(out var outputType).ThrowOnError();
            outputType.SetGUID(MFConstants.MF_MT_MAJOR_TYPE, MFConstants.MFMediaType_Video).ThrowOnError();
            outputType.SetGUID(MFConstants.MF_MT_SUBTYPE, MFConstants.MFVideoFormat_NV12).ThrowOnError();
            outputType.SetSize(MFConstants.MF_MT_FRAME_SIZE, width, height);
            _converter!.Object.SetOutputType(0, outputType, 0).ThrowOnError();
        }

        public HRESULT SetD3DManager(object manager, uint width, uint height)
        {
            if (manager == null)
                return HRESULTS.E_POINTER;

            if (width == 0 || height == 0)
                return HRESULTS.E_INVALIDARG;

            if (manager is not IMFDXGIDeviceManager dxgiManager)
                return HRESULTS.E_NOTIMPL;

            _dxgiManager = new ComObject<IMFDXGIDeviceManager>(dxgiManager);
            _dxgiManager.Object.OpenDeviceHandle(out _deviceHandle).ThrowOnError();
            _dxgiManager.Object.GetVideoService(_deviceHandle, typeof(ID3D11Device).GUID, out var obj).ThrowOnError();

            // create a texture/surface to write
            using var device = new ComObject<ID3D11Device>((ID3D11Device)obj);
            _texture = device.CreateTexture2D(new D3D11_TEXTURE2D_DESC
            {
                Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                Width = width,
                Height = height,
                ArraySize = 1,
                MipLevels = 1,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
                BindFlags = (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET)
            });

            // create a D2D1 render target from 2D GPU surface
            var surface = new ComObject<IDXGISurface>((IDXGISurface)_texture.Object, false);
            using var factory = D2D1Functions.D2D1CreateFactory(D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_MULTI_THREADED);
            _renderTarget = factory.CreateDxgiSurfaceRenderTarget(surface, new D2D1_RENDER_TARGET_PROPERTIES { pixelFormat = new D2D1_PIXEL_FORMAT { alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_PREMULTIPLIED } });

            CreateRenderTargetResources(width, height).ThrowOnError();

            // create GPU RGB => NV12 converter
            _converter = new ComObject<IMFTransform>((IMFTransform)System.Activator.CreateInstance(Type.GetTypeFromCLSID(MFConstants.CLSID_VideoProcessorMFT)!)!);
            SetConverterTypes(width, height);

            // make sure the video processor works on GPU
            ComObject.WithComPointer(manager, unk => _converter!.Object.ProcessMessage(_MFT_MESSAGE_TYPE.MFT_MESSAGE_SET_D3D_MANAGER, unk));
            EventProvider.LogInfo("OK");
            return HRESULTS.S_OK;
        }

        public HRESULT EnsureRenderTarget(uint width, uint height)
        {
            try
            {
                if (!HasD3DManager)
                {
                    // create a D2D1 render target from WIC bitmap
                    using var factory = D2D1Functions.D2D1CreateFactory(D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_MULTI_THREADED);
                    _bitmap = WICImagingFactory.CreateBitmap((int)width, (int)height, WICConstants.GUID_WICPixelFormat32bppBGR, WICBitmapCreateCacheOption.WICBitmapCacheOnDemand);
                    _renderTarget = factory.CreateWicBitmapRenderTarget(_bitmap, new D2D1_RENDER_TARGET_PROPERTIES
                    {
                        pixelFormat = new D2D1_PIXEL_FORMAT
                        {
                            alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_IGNORE,
                            format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM
                        }
                    });

                    CreateRenderTargetResources(width, height).ThrowOnError();

                    // create CPU RGB => NV12 converter
                    _converter = new ComObject<IMFTransform>((IMFTransform)System.Activator.CreateInstance(Type.GetTypeFromCLSID(MFConstants.CLSID_CColorConvertDMO)!)!);
                    SetConverterTypes(width, height);
                }

                _prevTime = Functions.MFGetSystemTime();
                _frameCount = 0;
                return HRESULTS.S_OK;
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public IComObject<IMFSample> Generate(IComObject<IMFSample> sample, Guid format)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(sample);
                IComObject<IMFSample>? outSample;

                // render something on image common to CPU & GPU
                if (_renderTarget != null && _textFormat != null && _dwrite != null && _whiteBrush != null && _blockBrushes != null)
                {
                    _renderTarget.BeginDraw();
                    _renderTarget.Clear(new _D3DCOLORVALUE(1, 0, 0, 1));

                    // draw some HSL blocks
                    var k = 0;
                    for (uint i = 0; i < _width / DIVISOR; i++)
                    {
                        for (uint j = 0; j < _height / DIVISOR; j++)
                        {
                            var brush = _blockBrushes[k++];
                            _renderTarget.FillRectangle(new D2D_RECT_F(i * DIVISOR, j * DIVISOR, (i + 1) * DIVISOR, (j + 1) * DIVISOR), brush);
                        }
                    }

                    var radius = DIVISOR * 2;
                    const float padding = 1;
                    _renderTarget.DrawEllipse(new D2D1_ELLIPSE(new D2D_POINT_2F(radius + padding, radius + padding), radius, radius), _whiteBrush);
                    _renderTarget.DrawEllipse(new D2D1_ELLIPSE(new D2D_POINT_2F(radius + padding, _height - radius - padding), radius, radius), _whiteBrush);
                    _renderTarget.DrawEllipse(new D2D1_ELLIPSE(new D2D_POINT_2F(_width - radius - padding, radius + padding), radius, radius), _whiteBrush);
                    _renderTarget.DrawEllipse(new D2D1_ELLIPSE(new D2D_POINT_2F(_width - radius - padding, _height - radius - padding), radius, radius), _whiteBrush);
                    _renderTarget.DrawRectangle(new D2D_RECT_F(radius, radius, _width - radius, _height - radius), _whiteBrush);

                    // draw resolution at center
                    // note: we could optimize here and compute layout only once if text doesn't change (depending on the font, etc.)
                    string fmt;
                    if (format == MFConstants.MFVideoFormat_NV12)
                    {
                        if (HasD3DManager)
                        {
                            fmt = "NV12 (GPU)";
                        }
                        else
                        {
                            fmt = "NV12 (CPU)";
                        }
                    }
                    else
                    {
                        if (HasD3DManager)
                        {
                            fmt = "RGB32 (GPU)";
                        }
                        else
                        {
                            fmt = "RGB32 (GPU)";
                        }
                    }

                    const int FRAMES_FOR_FPS = 60; // number of frames to wait to compute fps from last measure
                    const int NS_PER_MS = 10000;
                    const int MS_PER_S = 1000;

                    if (_fps == 0 || (_frameCount % FRAMES_FOR_FPS) == 0)
                    {
                        var time = Functions.MFGetSystemTime();
                        _fps = (uint)(MS_PER_S * NS_PER_MS * FRAMES_FOR_FPS / (time - _prevTime));
                        _prevTime = time;
                    }

                    var text = $"Format: {fmt}\n.NET Frame#: {_frameCount}\nFps: {_fps}\nResolution: {_width} x {_height}";

                    using var layout = _dwrite.CreateTextLayout(_textFormat, text, text.Length, _width, _height);
                    _renderTarget.DrawTextLayout(new D2D_POINT_2F(0, 0), layout, _whiteBrush);
                    _renderTarget.EndDraw();
                }

                //EventProvider.LogInfo("format: " + format + " generated 3D:" + HasD3DManager);

                if (HasD3DManager)
                {
                    sample.RemoveAllBuffers();

                    // create a buffer from this and add to sample
                    using var mediaBuffer = MFFunctions.MFCreateDXGISurfaceBuffer(_texture);
                    sample.Object.AddBuffer(mediaBuffer.Object).ThrowOnError();

                    // if we're on GPU & format is not RGB, convert using GPU (VideoProcessorMFT)
                    if (format == MFConstants.MFVideoFormat_NV12)
                    {
                        _converter!.Object.ProcessInput(0, sample.Object, 0).ThrowOnError();

                        // let converter build the sample for us, note it works because we gave it the D3DManager
                        var buffers = new _MFT_OUTPUT_DATA_BUFFER[1];
                        _converter.Object.ProcessOutput(0, (uint)buffers.Length, buffers, out var status).ThrowOnError();
                        outSample = ComObject.From<IMFSample>(buffers[0].pSample);
                        Marshal.Release(buffers[0].pSample);
                    }
                    else
                    {
                        outSample = sample; // nothing to do
                    }

                    _frameCount++;
                    return outSample;
                }

                // write WIC bitmap to output sample
                using var locked = _bitmap.Lock(WICBitmapLockFlags.WICBitmapLockRead);
                locked.Object.GetSize(out var w, out var h).ThrowOnError();
                locked.Object.GetStride(out var wicStride).ThrowOnError();
                locked.Object.GetDataPointer(out var wicSize, out var wicPointer).ThrowOnError();

                using var buffer = sample.GetBufferByIndex(0);
                buffer.WithLock((scanline, length, _) => wicPointer.CopyTo(scanline, length));
                buffer.SetCurrentLength(wicSize);

                // if we're on CPU & format is NOT RGB, convert using CPU (CColorConvertDMO)
                if (format == MFConstants.MFVideoFormat_NV12)
                {
                    _converter!.Object.ProcessInput(0, sample.Object, 0).ThrowOnError();

                    // convert RGB sample to NV12 sample
                    _converter.Object.GetOutputStreamInfo(0, out var info).ThrowOnError();
                    outSample = MFFunctions.MFCreateSample();
                    using var outBuffer = MFFunctions.MFCreateMemoryBuffer(info.cbSize);
                    outSample.AddBuffer(outBuffer);
                    outSample.WithComPointer(outSamplePtr =>
                    {
                        var buffers = new _MFT_OUTPUT_DATA_BUFFER[1];
                        buffers[0].pSample = outSamplePtr;
                        _converter.Object.ProcessOutput(0, 1, buffers, out var status).ThrowOnError();
                    });
                }
                else
                {
                    outSample = sample; // nothing to do
                }

                _frameCount++;
                return outSample;
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            EventProvider.LogInfo();
            try
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        // dispose managed state (managed objects)
                        var dxgiManager = _dxgiManager;
                        if (_deviceHandle != IntPtr.Zero && dxgiManager != null)
                        {
                            dxgiManager.Object.CloseDeviceHandle(_deviceHandle);
                        }

                        _whiteBrush.SafeDispose();
                        _blockBrushes.Dispose();
                        _bitmap.SafeDispose();
                        _texture.SafeDispose();
                        _textFormat.SafeDispose();
                        _dwrite.SafeDispose();
                        _renderTarget.SafeDispose();
                        _converter.SafeDispose();
                    }

                    // free unmanaged resources (unmanaged objects) and override finalizer
                    // set large fields to null
                    _disposedValue = true;
                    EventProvider.LogInfo("Disposed");
                }
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FrameGenerator()
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
