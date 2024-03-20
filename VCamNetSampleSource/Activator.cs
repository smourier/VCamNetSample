using System;
using System.Runtime.InteropServices;
using DirectN;
using VCamNetSampleSource.Utilities;

namespace VCamNetSampleSource
{
    [ComVisible(true), Guid(Shared.CLSID_VCamNet)]
    public class Activator : MFAttributes, IMFActivateImpl, ICustomQueryInterface
    {
        public Activator()
        {
            ComObject.Logger = EventProvider.Current;
            EventProvider.LogInfo($"ctor commandLine:{Environment.CommandLine}");
            SetDefaultAttributes(this);
        }

        private static void SetDefaultAttributes(IMFAttributes attributes)
        {
            attributes.SetUINT32(MFConstants.MF_VIRTUALCAMERA_PROVIDE_ASSOCIATED_CAMERA_SOURCES, 1).ThrowOnError();
            attributes.SetGUID(MFConstants.MFT_TRANSFORM_CLSID_Attribute, typeof(Activator).GUID).ThrowOnError();
        }

        public HRESULT ActivateObject(Guid riid, out nint ppv)
        {
            try
            {
                EventProvider.LogInfo($"{riid}");
                if (riid == typeof(IMFMediaSourceEx).GUID || riid == typeof(IMFMediaSource).GUID)
                {
                    var source = new MediaSource();
                    SetDefaultAttributes(source);
                    ppv = ComObject.QueryObjectInterface(source, riid, false);
                    if (ppv != IntPtr.Zero)
                        return HRESULTS.S_OK;
                }

                ppv = 0;
                EventProvider.LogInfo($"{riid} => E_NOINTERFACE");
                return HRESULTS.E_NOINTERFACE;
            }
            catch (Exception e)
            {
                EventProvider.LogError(e.ToString());
                throw;
            }
        }

        public HRESULT ShutdownObject()
        {
            EventProvider.LogInfo();
            return HRESULTS.S_OK;
        }

        public HRESULT DetachObject()
        {
            EventProvider.LogInfo();
            return HRESULTS.S_OK;
        }

        public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
        {
            EventProvider.LogInfo($"iid{iid:B}");
            ppv = 0;
            return CustomQueryInterfaceResult.NotHandled;
        }

        [ComRegisterFunction]
        public static void RegisterFunction(Type type)
        {
            EventProvider.LogInfo("type:" + type);
        }

        [ComUnregisterFunction]
        public static void UnregisterFunction(Type type)
        {
            EventProvider.LogInfo("type:" + type);
        }
    }
}
