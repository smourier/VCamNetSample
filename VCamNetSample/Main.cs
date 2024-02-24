using System;
using System.Reflection;
using System.Windows.Forms;
using DirectN;
using VCamNetSampleSource;

namespace VCamNetSample
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            Icon = Resources.MainIcon;
            Text = AssemblyUtilities.GetTitle(Assembly.GetExecutingAssembly());
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            MFFunctions.MFStartup();
            Functions.MFCreateVirtualCamera(
                __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0001.MFVirtualCameraType_SoftwareCameraSource,
                __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0002.MFVirtualCameraLifetime_Session,
                __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0003.MFVirtualCameraAccess_CurrentUser,
                Text,
                "{" + Shared.CLSID_VCamNet + "}",
                null,
                0,
                out var camera).ThrowOnError();

            camera.Start(null).ThrowOnError();
        }
    }
}
