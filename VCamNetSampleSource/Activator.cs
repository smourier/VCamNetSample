using System;
using System.Runtime.InteropServices;
using DirectN;
using VCamNetSampleSource.Utilities;

namespace VCamNetSampleSource
{
    [Guid(Shared.CLSID_VCamNet)]
    [ComVisible(true)]
    public class Activator : MFAttributes, IMFActivate, ICustomQueryInterface
    {
        public Activator()
        {
            EventProvider.LogInfo("ctor");
            SetUINT32(MFConstants.MF_VIRTUALCAMERA_PROVIDE_ASSOCIATED_CAMERA_SOURCES, 1).ThrowOnError();
            SetGUID(MFConstants.MFT_TRANSFORM_CLSID_Attribute, GetType().GUID).ThrowOnError();
        }

        public HRESULT ActivateObject(Guid riid, out object ppv)
        {
            EventProvider.LogInfo($"{riid}");
            if (riid == typeof(IMFMediaSourceEx).GUID)
            {
                ppv = new MediaSource(this);
                return HRESULTS.S_OK;
            }

            ppv = null!;
            EventProvider.LogInfo($"{riid} => E_NOINTERFACE");
            return HRESULTS.E_NOINTERFACE;
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
