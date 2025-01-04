using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif
[assembly: AssemblyTitle("VCamNetSample")]
[assembly: AssemblyDescription("Virtual Web Cam Sample")]
[assembly: AssemblyCompany("Simon Mourier")]
[assembly: AssemblyProduct("VCamNetSample")]
[assembly: AssemblyCopyright("Copyright (C) 2023-2025 Simon Mourier. All rights reserved.")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("f5720613-b59e-4228-b67e-b71727dc7fba")]
[assembly: SupportedOSPlatform("windows10.0.22621.0")]
