# VCamNetSample
This solution contains a Media Foundation Virtual Camera Sample developed using .NET. It works only on Windows 11 thanks to the [MFCreateVirtualCamera](https://learn.microsoft.com/en-us/windows/win32/api/mfvirtualcamera/nf-mfvirtualcamera-mfcreatevirtualcamera) API.

It's a port of the same project written in C++ **VCamSample**: [https://github.com/smourier/VCamSample](https://github.com/smourier/VCamSample)

There are two projects in the solution:

* **VCamNetSampleSource**: the Media Source that provides RGB32 and NV12 streaming samples.
* **VCamNetSample**: the "driver" application that does very little but calls `MFCreateVirtualCamera`.



To test the .NET virtual cam:

* Build in debug or release
* Go to the build output and register the media source (a COM object) with a command similar to this: `regsvr32 VCamNetSampleSource.dll` (you *must* run this as administrator, it' not possible to register a Virtual Camera media source in `HKCU`, only in `HKLM` since it will be loaded by multiple processes)
* Run the VCamNetSample app.
* Run for example the Windows Camera app or using a Web Browser ImageCapture API

You should now see something like this in the Windows Camera App

![Screenshot 2024-01-22 131726](https://github.com/smourier/VCamSample/assets/5328574/50b27acb-3cf7-4d41-9298-84f7c1358148)

Something like this in Windows' Edge Web Browser, using this testing page: https://googlechrome.github.io/samples/image-capture/grab-frame-take-photo.html

![Screenshot 2024-01-22 133220](https://github.com/smourier/VCamSample/assets/5328574/1f7d34e9-5646-4f26-bc9a-534e3bc9d625)

Something like this in OBS (Video Capture Device):

![image](https://github.com/smourier/VCamSample/assets/5328574/47768c63-2979-40ab-ae70-fca632b97d81)

Something like this in Teams, this is your avatar :-)


## Notes

* The media source uses `Direct2D` and `DirectWrite` to create images. It will then create Media Foundation samples from these. To create MF samples, it can use:
  * The GPU, if a Direct3D manager has been provided by the environment. This is the case of the Windows 11 camera app.
  * The CPU, if no Direct3D environment has been provided. In this case, the media source uses a WIC bitmap as a render target and it then copies the bits over to an MF sample. The ImageCapture API code embedded in Chrome or Edge, Teams, etc. is an example of such a D3D-less environment.
  * If you want to force CPU usage at all times, you can change the code in `MediaStream::SetD3DManager` and put the lines there in comment.
* The media source provides RGB32 and NV12 formats as most setups prefer the NV12 format. Samples are initially created as RGB32 (Direct2D) and converted to NV12. To convert the samples, the media source uses two ways:
  * The GPU, if a Direct3D manager has been provided, using Media Foundation's [Video Processor MFT](https://learn.microsoft.com/en-us/windows/win32/medfound/video-processor-mft).
  * The CPU, if no Direct3D environment has been provided. In this case, the RGB to NV12 conversion is done using Media Foundation's [Color Converter DSP](https://learn.microsoft.com/en-us/windows/win32/medfound/colorconverter).
  * If you want to force RGB32 mode, you can change the code in `MediaStream` constructor and set the media types array size to 1 (check comments in the code).

## Tracing

The code output lots of interesting traces. It's quite important in this virtual camera environment because there's not just your process that's involved but at least 4: the VCamNetSample app, the Windows Frame Server, the Windows camera monitor, and the reader app (camera, etc.). They all load the media source COM object in-process.

The solution also contains a small **DebuggerAttach** project that can be ran to attach to the Windows Frame Server (hosted in svchost.exe). For it to run you must run Visual Studio and DebuggerAttach as administrator. You can also attach to the Frame Server using Visual Studio attach to process once it's been started.

Tracing here  doesn't use `OutputDebugString` because it's 100% old, crappy, truncating text, slow, etc. Instead it uses Event Tracing for Windows ("ETW") in "string-only" mode (the mode where it's very simple and you don't have to register painfull traces records and use complex readers...).

So to read these ETW traces, use WpfTraceSpy you can download here https://github.com/smourier/TraceSpy. Configure an ETW Provider with the GUID set to `964d4572-adb9-4f3a-8170-fcbecec27467`
