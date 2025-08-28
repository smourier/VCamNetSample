namespace VCamNetSampleSourceAOT;

[Guid(Shared.CLSID_VCamNetAOT)]
[GeneratedComClass]
public partial class Activator : IMFActivate, IMFAttributes
{
    private readonly MFAttributes _attributes; // if we derive from it, C#/WinRT doesn't see it for some reason

    public Activator()
    {
        ComHosting.Trace();
        _attributes = new MFAttributes(nameof(Activator));
        SetDefaultAttributes(_attributes.NativeObject);
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
            ComHosting.Trace($"{riid.GetName()}");
            if (riid == typeof(IMFMediaSourceEx).GUID || riid == typeof(IMFMediaSource).GUID)
            {
                var source = new MediaSource();
                SetDefaultAttributes(source);
                ppv = DirectN.Extensions.Com.ComObject.GetOrCreateComInstance(source, riid);
                if (ppv != 0)
                    return Constants.S_OK;
            }

            ComHosting.Trace($"{riid.GetName()} => E_NOINTERFACE");
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
}
