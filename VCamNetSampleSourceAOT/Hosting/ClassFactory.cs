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
        if (pUnkOuter != 0)
        {
            ppvObject = 0;
            return Constants.CLASS_E_NOAGGREGATION;
        }

        object? obj = null;
        if (Type == typeof(Activator))
        {
            obj = new Activator();
        }

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
