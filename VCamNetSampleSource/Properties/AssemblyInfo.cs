using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif
[assembly: AssemblyTitle("VCamNetSampleSource")]
[assembly: AssemblyDescription("Virtual Web Cam Sample")]
[assembly: AssemblyCompany("Simon Mourier")]
[assembly: AssemblyProduct("VCamNetSample")]
[assembly: AssemblyCopyright("Copyright (C) 2023-2025 Simon Mourier. All rights reserved.")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("8079cbe0-b9f7-45bc-88f3-2bd01831cd94")]
[assembly: SupportedOSPlatform("windows10.0.22621.0")]
