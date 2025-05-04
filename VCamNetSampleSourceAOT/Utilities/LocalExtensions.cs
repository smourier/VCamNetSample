namespace VCamNetSampleSourceAOT.Utilities;

internal static class LocalExtensions
{
    public static string GetMFName(this Guid guid) => typeof(Constants).GetGuidName(guid);
    public static string GetMFName(this KSIDENTIFIER id) => typeof(Constants).GetGuidName(id.Anonymous.Anonymous.Set) + " " + id.Anonymous.Anonymous.Id;

    public static float HUE2RGB(float p, float q, float t)
    {
        if (t < 0)
        {
            t += 1;
        }

        if (t > 1)
        {
            t -= 1;
        }

        if (t < 1 / 6.0f)
            return p + (q - p) * 6 * t;

        if (t < 1 / 2.0f)
            return q;

        if (t < 2 / 3.0f)
            return p + (q - p) * (2 / 3.0f - t) * 6;

        return p;
    }

    public static D3DCOLORVALUE HSL2RGB(float h, float s, float l)
    {
        var result = new D3DCOLORVALUE { a = 1 };
        if (s == 0)
        {
            result.r = l;
            result.g = l;
            result.b = l;
        }
        else
        {
            var q = l < 0.5f ? l * (1 + s) : l + s - l * s;
            var p = 2 * l - q;
            result.r = HUE2RGB(p, q, h + 1 / 3.0f);
            result.g = HUE2RGB(p, q, h);
            result.b = HUE2RGB(p, q, h - 1 / 3.0f);
        }
        return result;
    }
}
