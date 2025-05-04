namespace VCamNetSampleSourceAOT.Hosting;

[GeneratedComClass]
public partial class ClassFactory : IClassFactory
{
    public ClassFactory(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
    }

    public Type Type { get; }

    HRESULT IClassFactory.CreateInstance(nint pUnkOuter, in Guid riid, out nint ppvObject)
    {
        ComHosting.Trace($"pUnkOuter:{pUnkOuter} riid:{riid} Type:{Type.FullName}");
        ppvObject = 0;
        // we should only instantiate classes that are declared in ComHosting.ComTypes
        var obj = System.Activator.CreateInstance(Type, true);
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
        if (obj is null)
            return Constants.E_NOINTERFACE;

        var unk = ComHosting._wrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
        if (riid != typeof(IUnknown).GUID)
        {
            var hr = Marshal.QueryInterface(unk, riid, out ppvObject);
            Marshal.Release(unk);
            if (hr < 0)
                return (uint)hr;
        }
        else
        {
            ppvObject = unk;
        }
        return Constants.S_OK;
    }

    HRESULT IClassFactory.LockServer(BOOL fLock)
    {
        ComHosting.Trace($"lock:{fLock}");
        return Constants.S_OK;
    }
}
