namespace VCamNetSampleSourceAOT.Utilities;

internal static class LocalExtensions
{
    public static string GetDebugName(this KSIDENTIFIER id)
    {
        var flags = (KSPROPERTY_TYPE)id.Anonymous.Anonymous.Flags;
        var str = $"{id.Anonymous.Anonymous.Set.GetName()} ({flags}) ";

        if (id.Anonymous.Anonymous.Set == Constants.KSPROPERTYSETID_ExtendedCameraControl)
            return str + (KSPROPERTY_CAMERACONTROL_EXTENDED_PROPERTY)id.Anonymous.Anonymous.Id;

        if (id.Anonymous.Anonymous.Set == Constants.PROPSETID_VIDCAP_CAMERACONTROL)
            return str + (KSPROPERTY_VIDCAP_CAMERACONTROL)id.Anonymous.Anonymous.Id;

        if (id.Anonymous.Anonymous.Set == Constants.PROPSETID_VIDCAP_VIDEOPROCAMP)
            return str + (KSPROPERTY_VIDCAP_VIDEOPROCAMP)id.Anonymous.Anonymous.Id;

        if (id.Anonymous.Anonymous.Set == Constants.KSPROPERTYSETID_PerFrameSettingControl)
            return str + (KSPROPERTY_CAMERACONTROL_PERFRAMESETTING_PROPERTY)id.Anonymous.Anonymous.Id;

        if (id.Anonymous.Anonymous.Set == Constants.PROPSETID_VIDCAP_CAMERACONTROL_REGION_OF_INTEREST)
            return str + (KSPROPERTY_CAMERACONTROL_REGION_OF_INTEREST)id.Anonymous.Anonymous.Id;

        if (id.Anonymous.Anonymous.Set == Constants.PROPSETID_VIDCAP_CAMERACONTROL_IMAGE_PIN_CAPABILITY)
            return str + (KSPROPERTY_CAMERACONTROL_IMAGE_PIN_CAPABILITY)id.Anonymous.Anonymous.Id;

        if (id.Anonymous.Anonymous.Set == Constants.KSPROPSETID_Topology)
            return str + (KSPROPERTY_TOPOLOGY)id.Anonymous.Anonymous.Id;

        if (id.Anonymous.Anonymous.Set == Constants.KSPROPSETID_Pin)
            return str + (KSPROPERTY_PIN)id.Anonymous.Anonymous.Id;

        if (id.Anonymous.Anonymous.Set == Constants.KSPROPSETID_Connection)
            return str + (KSPROPERTY_CONNECTION)id.Anonymous.Anonymous.Id;

        return str + id.Anonymous.Anonymous.Id;
    }

    private enum KSPROPERTY_TYPE : uint
    {
        KSPROPERTY_TYPE_GET = Constants.KSPROPERTY_TYPE_GET,
        KSPROPERTY_TYPE_SET = Constants.KSPROPERTY_TYPE_SET,
        KSPROPERTY_TYPE_GETPAYLOADSIZE = Constants.KSPROPERTY_TYPE_GETPAYLOADSIZE,
        KSPROPERTY_TYPE_SETSUPPORT = Constants.KSPROPERTY_TYPE_SETSUPPORT,
        KSPROPERTY_TYPE_BASICSUPPORT = Constants.KSPROPERTY_TYPE_BASICSUPPORT,
        KSPROPERTY_TYPE_RELATIONS = Constants.KSPROPERTY_TYPE_RELATIONS,
        KSPROPERTY_TYPE_SERIALIZESET = Constants.KSPROPERTY_TYPE_SERIALIZESET,
        KSPROPERTY_TYPE_UNSERIALIZESET = Constants.KSPROPERTY_TYPE_UNSERIALIZESET,
        KSPROPERTY_TYPE_SERIALIZERAW = Constants.KSPROPERTY_TYPE_SERIALIZERAW,
        KSPROPERTY_TYPE_UNSERIALIZERAW = Constants.KSPROPERTY_TYPE_UNSERIALIZERAW,
        KSPROPERTY_TYPE_SERIALIZESIZE = Constants.KSPROPERTY_TYPE_SERIALIZESIZE,
        KSPROPERTY_TYPE_DEFAULTVALUES = Constants.KSPROPERTY_TYPE_DEFAULTVALUES,
        KSPROPERTY_TYPE_TOPOLOGY = Constants.KSPROPERTY_TYPE_TOPOLOGY,
        KSPROPERTY_TYPE_HIGHPRIORITY = Constants.KSPROPERTY_TYPE_HIGHPRIORITY,
        KSPROPERTY_TYPE_FSFILTERSCOPE = Constants.KSPROPERTY_TYPE_FSFILTERSCOPE,
        KSPROPERTY_TYPE_COPYPAYLOAD = Constants.KSPROPERTY_TYPE_COPYPAYLOAD,
    }

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
