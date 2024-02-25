using System;
using System.Runtime.InteropServices;
using DirectN;

namespace VCamNetSampleSource.Utilities
{
    internal static class Extensions
    {
        public static string GetMFName(this Guid guid) => typeof(MFConstants).GetGuidName(guid);
        public static string GetMFName(this KSIDENTIFIER id) => typeof(MFConstants).GetGuidName(id.__union_0.__field_0.Set) + " " + id.__union_0.__field_0.Id;
        public static HRESULT QueryInterface<T>(this object instance, out IntPtr ppv)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (instance is ComObject)
            {
                instance = ((ComObject)instance).Object;
            }
            var unk = Marshal.GetIUnknownForObject(instance);
            var iid = typeof(T).GUID;
            var hr = Marshal.QueryInterface(unk, ref iid, out ppv);
            Marshal.Release(unk);
            return hr;
        }
    }
}
