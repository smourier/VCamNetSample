using System;
using System.Runtime.InteropServices;
using DirectN;
using VCamNetSampleSource.Utilities;

namespace VCamNetSampleSource
{
    [ComVisible(true), Guid(Shared.CLSID_VCamNet)]
    public class Activator : MFAttributes, Activator.IMFActivate, ICustomQueryInterface
    {
        public Activator()
        {
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
                    var unk = Marshal.GetIUnknownForObject(source);
                    var hr = Marshal.QueryInterface(unk, ref riid, out ppv);
                    Marshal.Release(unk);
                    return hr;
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

        // we redefine IMFActivate because of ActivateObject
        // .NET doesn't know how to return something else than IUnknown so we must use an IntPtr
        [ComImport, Guid("7fee9e9a-4a89-47a6-899c-b6a53a70fb67"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMFActivate : IMFAttributes
        {
            // IMFAttributes
            [PreserveSig]
            new HRESULT GetItem(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [full][out][in] __RPC__inout_opt */ [In, Out] PropVariant pValue);

            [PreserveSig]
            new HRESULT GetItemType(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [out] __RPC__out */ out _MF_ATTRIBUTE_TYPE pType);

            [PreserveSig]
            new HRESULT CompareItem(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* __RPC__in */ [In, Out] PropVariant Value, /* [out] __RPC__out */ out bool pbResult);

            [PreserveSig]
            new HRESULT Compare(/* __RPC__in_opt */ IMFAttributes pTheirs, _MF_ATTRIBUTES_MATCH_TYPE MatchType, /* [out] __RPC__out */ out bool pbResult);

            [PreserveSig]
            new HRESULT GetUINT32(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [out] __RPC__out */ out uint punValue);

            [PreserveSig]
            new HRESULT GetUINT64(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [out] __RPC__out */ out ulong punValue);

            [PreserveSig]
            new HRESULT GetDouble(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [out] __RPC__out */ out double pfValue);

            [PreserveSig]
            new HRESULT GetGUID(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [out] __RPC__out */ out Guid pguidValue);

            [PreserveSig]
            new HRESULT GetStringLength(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [out] __RPC__out */ out uint pcchLength);

            [PreserveSig]
            new HRESULT GetString(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [size_is][out] __RPC__out_ecount_full(cchBufSize) */ [MarshalAs(UnmanagedType.LPWStr)] string pwszValue, uint cchBufSize, /* optional(UINT32) */ IntPtr pcchLength);

            [PreserveSig]
            new HRESULT GetAllocatedString(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* optional(LPWSTR) */ IntPtr ppwszValue, /* [out] __RPC__out */ out uint pcchLength);

            [PreserveSig]
            new HRESULT GetBlobSize(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [out] __RPC__out */ out uint pcbBlobSize);

            [PreserveSig]
            new HRESULT GetBlob(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [size_is][out] __RPC__out_ecount_full(cbBufSize) */ [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, int cbBufSize, /* optional(UINT32) */ IntPtr pcbBlobSize);

            [PreserveSig]
            new HRESULT GetAllocatedBlob(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* optional(UINT8) */ out IntPtr ppBuf, /* [out] __RPC__out */ out uint pcbSize);

            [PreserveSig]
            new HRESULT GetUnknown(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid riid, /* [iid_is][out] __RPC__deref_out_opt */ [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

            [PreserveSig]
            new HRESULT SetItem(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* __RPC__in */ [In, Out] PropVariant Value);

            [PreserveSig]
            new HRESULT DeleteItem(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey);

            [PreserveSig]
            new HRESULT DeleteAllItems();

            [PreserveSig]
            new HRESULT SetUINT32(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, uint unValue);

            [PreserveSig]
            new HRESULT SetUINT64(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, ulong unValue);

            [PreserveSig]
            new HRESULT SetDouble(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, double fValue);

            [PreserveSig]
            new HRESULT SetGUID(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);

            [PreserveSig]
            new HRESULT SetString(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [string][in] __RPC__in_string */ [MarshalAs(UnmanagedType.LPWStr)] string wszValue);

            [PreserveSig]
            new HRESULT SetBlob(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [size_is][in] __RPC__in_ecount_full(cbBufSize) */ [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, int cbBufSize);

            [PreserveSig]
            new HRESULT SetUnknown(/* __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, /* [in] __RPC__in_opt */ [MarshalAs(UnmanagedType.IUnknown)] object pUnknown);

            [PreserveSig]
            new HRESULT LockStore();

            [PreserveSig]
            new HRESULT UnlockStore();

            [PreserveSig]
            new HRESULT GetCount(/* [out] __RPC__out */ out uint pcItems);

            [PreserveSig]
            new HRESULT GetItemByIndex(uint unIndex, /* [out] __RPC__out */ out Guid pguidKey, /* [full][out][in] __RPC__inout_opt */ [In, Out] PropVariant pValue);

            [PreserveSig]
            new HRESULT CopyAllItems(/* [in] __RPC__in_opt */ IMFAttributes pDest);

            // IMFActivate
            [PreserveSig]
            HRESULT ActivateObject(/* [in] __RPC__in */ [MarshalAs(UnmanagedType.LPStruct)] Guid riid, /* [retval][iid_is][out] __RPC__deref_out_opt */ out IntPtr ppv);

            [PreserveSig]
            HRESULT ShutdownObject();

            [PreserveSig]
            HRESULT DetachObject();
        }
    }
}
