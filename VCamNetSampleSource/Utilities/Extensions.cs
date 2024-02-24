using System;
using DirectN;

namespace VCamNetSampleSource.Utilities
{
    internal static class Extensions
    {
        public static string GetMFName(this Guid guid) => typeof(MFConstants).GetGuidName(guid);
    }
}
