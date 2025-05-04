namespace VCamNetSampleSourceAOT;

[Guid(Shared.CLSID_VCamNet)]
[GeneratedComClass]
public partial class Activator : MFAttributes, IMFActivate
{
    public Activator()
    {
        ComHosting.Trace();
        SetDefaultAttributes(NativeObject);
    }

    private static void SetDefaultAttributes(IMFAttributes attributes)
    {
        attributes.SetUINT32(Constants.MF_VIRTUALCAMERA_PROVIDE_ASSOCIATED_CAMERA_SOURCES, 1).ThrowOnError();
        attributes.SetGUID(Constants.MFT_TRANSFORM_CLSID_Attribute, typeof(Activator).GUID).ThrowOnError();
    }

    public HRESULT ActivateObject(in Guid riid, out nint ppv)
    {
        try
        {
            ppv = 0;
            ComHosting.Trace($"{riid}");
            if (riid == typeof(IMFMediaSourceEx).GUID || riid == typeof(IMFMediaSource).GUID)
            {
                var source = new MediaSource();
                SetDefaultAttributes(source);
                ppv = DirectN.Extensions.Com.ComObject.GetOrCreateComInstance(source, riid);
                if (ppv != 0)
                    return Constants.S_OK;
            }

            ComHosting.Trace($"{riid} => E_NOINTERFACE");
            return Constants.E_NOINTERFACE;
        }
        catch (Exception e)
        {
            ComHosting.Trace($"Error:{e}");
            ppv = 0;
            return e.HResult;
        }
    }

    public HRESULT DetachObject()
    {
        ComHosting.Trace();
        return Constants.S_OK;
    }

    public HRESULT ShutdownObject()
    {
        ComHosting.Trace();
        return Constants.S_OK;
    }
}
