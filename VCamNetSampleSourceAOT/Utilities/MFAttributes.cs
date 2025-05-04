
namespace VCamNetSampleSourceAOT.Utilities;

public class MFAttributes : InterlockedComObject<IMFAttributes>, IMFAttributes
{
    public MFAttributes()
        : base(CreateAttributes())
    {
    }

    private static ComObject<IMFAttributes> CreateAttributes()
    {
        Functions.MFCreateAttributes(out var obj, 0).ThrowOnError();
        return new ComObject<IMFAttributes>(obj);
    }

    HRESULT IMFAttributes.Compare(IMFAttributes? pTheirs, MF_ATTRIBUTES_MATCH_TYPE MatchType, out BOOL pbResult)
    {
        var hr = NativeObject.Compare(pTheirs, MatchType, out pbResult);
        ComHosting.Trace($"{pTheirs} {MatchType} {pbResult} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.CompareItem(in Guid guidKey, in PROPVARIANT Value, out BOOL pbResult)
    {
        var hr = NativeObject.CompareItem(guidKey, Value, out pbResult);
        ComHosting.Trace($"{guidKey} {Value} {pbResult} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.CopyAllItems(IMFAttributes pDest)
    {
        var hr = NativeObject.CopyAllItems(pDest);
        ComHosting.Trace($"{pDest} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.DeleteAllItems()
    {
        var hr = NativeObject.DeleteAllItems();
        ComHosting.Trace($" => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.DeleteItem(in Guid guidKey)
    {
        var hr = NativeObject.DeleteItem(guidKey);
        ComHosting.Trace($"{guidKey} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetAllocatedBlob(in Guid guidKey, out nint ppBuf, out uint pcbSize)
    {
        var hr = NativeObject.GetAllocatedBlob(guidKey, out ppBuf, out pcbSize);
        ComHosting.Trace($"{guidKey} {ppBuf} {pcbSize} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetAllocatedString(in Guid guidKey, out PWSTR ppwszValue, out uint pcchLength)
    {
        var hr = NativeObject.GetAllocatedString(guidKey, out ppwszValue, out pcchLength);
        ComHosting.Trace($"{guidKey} {ppwszValue} {pcchLength} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetBlob(in Guid guidKey, nint pBuf, uint cbBufSize, nint pcbBlobSize)
    {
        var hr = NativeObject.GetBlob(guidKey, pBuf, cbBufSize, pcbBlobSize);
        ComHosting.Trace($"{guidKey} {pBuf} {cbBufSize} {pcbBlobSize} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetBlobSize(in Guid guidKey, out uint pcbBlobSize)
    {
        var hr = NativeObject.GetBlobSize(guidKey, out pcbBlobSize);
        ComHosting.Trace($"{guidKey} {pcbBlobSize} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetCount(out uint pcItems)
    {
        var hr = NativeObject.GetCount(out pcItems);
        ComHosting.Trace($"{pcItems} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetDouble(in Guid guidKey, out double pfValue)
    {
        var hr = NativeObject.GetDouble(guidKey, out pfValue);
        ComHosting.Trace($"{guidKey} {pfValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetGUID(in Guid guidKey, out Guid pguidValue)
    {
        var hr = NativeObject.GetGUID(guidKey, out pguidValue);
        ComHosting.Trace($"{guidKey} {pguidValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetItem(in Guid guidKey, nint pValue)
    {
        var hr = NativeObject.GetItem(guidKey, pValue);
        ComHosting.Trace($"{guidKey} {pValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetItemByIndex(uint unIndex, out Guid pguidKey, nint pValue)
    {
        var hr = NativeObject.GetItemByIndex(unIndex, out pguidKey, pValue);
        ComHosting.Trace($"{unIndex} {pguidKey} {pValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetItemType(in Guid guidKey, out MF_ATTRIBUTE_TYPE pType)
    {
        var hr = NativeObject.GetItemType(guidKey, out pType);
        ComHosting.Trace($"{guidKey} {pType} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetString(in Guid guidKey, PWSTR pwszValue, uint cchBufSize, nint pcchLength)
    {
        var hr = NativeObject.GetString(guidKey, pwszValue, cchBufSize, pcchLength);
        ComHosting.Trace($"{guidKey} {pwszValue} {cchBufSize} {pcchLength} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetStringLength(in Guid guidKey, out uint pcchLength)
    {
        var hr = NativeObject.GetStringLength(guidKey, out pcchLength);
        ComHosting.Trace($"{guidKey} {pcchLength} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetUINT32(in Guid guidKey, out uint punValue)
    {
        var hr = NativeObject.GetUINT32(guidKey, out punValue);
        ComHosting.Trace($"{guidKey} {punValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetUINT64(in Guid guidKey, out ulong punValue)
    {
        var hr = NativeObject.GetUINT64(guidKey, out punValue);
        ComHosting.Trace($"{guidKey} {punValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.GetUnknown(in Guid guidKey, in Guid riid, out nint ppv)
    {
        var hr = NativeObject.GetUnknown(guidKey, riid, out ppv);
        ComHosting.Trace($"{guidKey} {riid} {ppv} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.LockStore()
    {
        var hr = NativeObject.LockStore();
        ComHosting.Trace($" => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.SetBlob(in Guid guidKey, nint pBuf, uint cbBufSize)
    {
        var hr = NativeObject.SetBlob(guidKey, pBuf, cbBufSize);
        ComHosting.Trace($"{guidKey} {pBuf} {cbBufSize} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.SetDouble(in Guid guidKey, double fValue)
    {
        var hr = NativeObject.SetDouble(guidKey, fValue);
        ComHosting.Trace($"{guidKey} {fValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.SetGUID(in Guid guidKey, in Guid guidValue)
    {
        var hr = NativeObject.SetGUID(guidKey, guidValue);
        ComHosting.Trace($"{guidKey} {guidValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.SetItem(in Guid guidKey, in PROPVARIANT Value)
    {
        var hr = NativeObject.SetItem(guidKey, Value);
        ComHosting.Trace($"{guidKey} {Value} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.SetString(in Guid guidKey, PWSTR wszValue)
    {
        var hr = NativeObject.SetString(guidKey, wszValue);
        ComHosting.Trace($"{guidKey} {wszValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.SetUINT32(in Guid guidKey, uint unValue)
    {
        var hr = NativeObject.SetUINT32(guidKey, unValue);
        ComHosting.Trace($"{guidKey} {unValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.SetUINT64(in Guid guidKey, ulong unValue)
    {
        var hr = NativeObject.SetUINT64(guidKey, unValue);
        ComHosting.Trace($"{guidKey} {unValue} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.SetUnknown(in Guid guidKey, nint pUnknown)
    {
        var hr = NativeObject.SetUnknown(guidKey, pUnknown);
        ComHosting.Trace($"{guidKey} {pUnknown} => {hr}");
        return hr;
    }

    HRESULT IMFAttributes.UnlockStore()
    {
        var hr = NativeObject.UnlockStore();
        ComHosting.Trace($" => {hr}");
        return hr;
    }
}
