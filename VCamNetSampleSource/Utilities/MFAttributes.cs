using System;
using DirectN;

namespace VCamNetSampleSource.Utilities
{
    public class MFAttributes : IMFAttributes, IDisposable
    {
        private bool _disposedValue;

        public MFAttributes()
        {
            Attributes = MFFunctions.MFCreateAttributes();
        }

        public IComObject<IMFAttributes> Attributes { get; }

        public virtual HRESULT GetItem(Guid guidKey, DetachedPropVariant pValue)
        {
            var hr = Attributes.Object.GetItem(guidKey, pValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pValue} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetItemType(Guid guidKey, out _MF_ATTRIBUTE_TYPE pType)
        {
            var hr = Attributes.Object.GetItemType(guidKey, out pType);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pType} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT CompareItem(Guid guidKey, DetachedPropVariant pValue, out bool pbResult)
        {
            var hr = Attributes.Object.CompareItem(guidKey, pValue, out pbResult);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pValue} {pbResult} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT Compare(IMFAttributes pTheirs, _MF_ATTRIBUTES_MATCH_TYPE type, out bool pbResult)
        {
            var hr = Attributes.Object.Compare(pTheirs, type, out pbResult);
            EventProvider.LogInfo($" {pTheirs} {type} {pbResult} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetUINT32(Guid guidKey, out uint punValue)
        {
            var hr = Attributes.Object.GetUINT32(guidKey, out punValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {punValue} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetUINT64(Guid guidKey, out ulong punValue)
        {
            var hr = Attributes.Object.GetUINT64(guidKey, out punValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {punValue} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetDouble(Guid guidKey, out double pfValue)
        {
            var hr = Attributes.Object.GetDouble(guidKey, out pfValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pfValue} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetGUID(Guid guidKey, out Guid pguidValue)
        {
            var hr = Attributes.Object.GetGUID(guidKey, out pguidValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pguidValue:B} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetStringLength(Guid guidKey, out uint pcchLength)
        {
            var hr = Attributes.Object.GetStringLength(guidKey, out pcchLength);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pcchLength} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetString(Guid guidKey, string pwszValue, uint cchBufSize, nint pcchLength)
        {
            var hr = Attributes.Object.GetString(guidKey, pwszValue, cchBufSize, pcchLength);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pwszValue} {cchBufSize} {pcchLength} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetAllocatedString(Guid guidKey, nint ppwszValue, out uint pcchLength)
        {
            var hr = Attributes.Object.GetAllocatedString(guidKey, ppwszValue, out pcchLength);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {ppwszValue} {pcchLength} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetBlobSize(Guid guidKey, out uint pcbBlobSize)
        {
            var hr = Attributes.Object.GetBlobSize(guidKey, out pcbBlobSize);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pcbBlobSize} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetBlob(Guid guidKey, byte[] pBuf, int cbBufSize, nint pcbBlobSize)
        {
            var hr = Attributes.Object.GetBlob(guidKey, pBuf, cbBufSize, pcbBlobSize);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pBuf} {cbBufSize} {pcbBlobSize} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetAllocatedBlob(Guid guidKey, out nint ppBuf, out uint pcbSize)
        {
            var hr = Attributes.Object.GetAllocatedBlob(guidKey, out ppBuf, out pcbSize);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pcbSize} {pcbSize} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetUnknown(Guid guidKey, Guid riid, out object ppv)
        {
            var hr = Attributes.Object.GetUnknown(guidKey, riid, out ppv);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {riid} {ppv} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT SetItem(Guid guidKey, DetachedPropVariant value)
        {
            var hr = Attributes.Object.SetItem(guidKey, value);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {value} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT DeleteItem(Guid guidKey)
        {
            var hr = Attributes.Object.DeleteItem(guidKey);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT DeleteAllItems()
        {
            var hr = Attributes.Object.DeleteAllItems();
            EventProvider.LogInfo($" => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT SetUINT32(Guid guidKey, uint unValue)
        {
            var hr = Attributes.Object.SetUINT32(guidKey, unValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {unValue} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT SetUINT64(Guid guidKey, ulong unValue)
        {
            var hr = Attributes.Object.SetUINT64(guidKey, unValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {unValue} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT SetDouble(Guid guidKey, double fValue)
        {
            var hr = Attributes.Object.SetDouble(guidKey, fValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {fValue} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT SetGUID(Guid guidKey, Guid guidValue)
        {
            var hr = Attributes.Object.SetGUID(guidKey, guidValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {guidValue:B} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT SetString(Guid guidKey, string wszValue)
        {
            var hr = Attributes.Object.SetString(guidKey, wszValue);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {wszValue} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT SetBlob(Guid guidKey, byte[] pBuf, int cbBufSize)
        {
            var hr = Attributes.Object.SetBlob(guidKey, pBuf, cbBufSize);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pBuf} {cbBufSize} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT SetUnknown(Guid guidKey, object pUnknown)
        {
            var hr = Attributes.Object.SetUnknown(guidKey, pUnknown);
            EventProvider.LogInfo($"({guidKey.GetMFName()}) {pUnknown} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT LockStore()
        {
            var hr = Attributes.Object.LockStore();
            EventProvider.LogInfo($" => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT UnlockStore()
        {
            var hr = Attributes.Object.UnlockStore();
            EventProvider.LogInfo($" => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetCount(out uint pcItems)
        {
            var hr = Attributes.Object.GetCount(out pcItems);
            EventProvider.LogInfo($"{pcItems} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT GetItemByIndex(uint unIndex, out Guid pguidKey, DetachedPropVariant pValue)
        {
            var hr = Attributes.Object.GetItemByIndex(unIndex, out pguidKey, pValue);
            EventProvider.LogInfo($"{unIndex} {pguidKey} {pValue} => {hr}", filePath: GetType().Name);
            return hr;
        }

        public virtual HRESULT CopyAllItems(IMFAttributes pDest)
        {
            var hr = Attributes.Object.CopyAllItems(pDest);
            //EventProvider.LogInfo($"{this.Trace()} > {pDest.Trace()} => {hr}", filePath: GetType().Name);
            EventProvider.LogInfo($"{this.Count()} => {hr}", filePath: GetType().Name);
            return hr;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    Attributes?.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MFAttributes()
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
