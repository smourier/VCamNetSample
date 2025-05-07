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

        // we should only instantiate classes that are declared in ComHosting.ComTypes
#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
        var obj = System.Activator.CreateInstance(Type, true);
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.

        var unk = DirectN.Extensions.Com.ComObject.GetOrCreateComInstance(obj, riid);
        ppvObject = unk;
        return unk == 0 ? Constants.E_NOINTERFACE : Constants.S_OK;
    }

    HRESULT IClassFactory.LockServer(BOOL fLock)
    {
        ComHosting.Trace($"lock:{fLock}");
        return Constants.S_OK;
    }
}
