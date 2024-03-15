using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using DirectN;
using VCamNetSampleSource;

namespace VCamNetSample
{
    public partial class Main : Form
    {
        private IComObject<IMFVirtualCamera>? _camera;

        public Main()
        {
            InitializeComponent();
            Icon = Resources.MainIcon;
            Text = AssemblyUtilities.GetTitle(Assembly.GetExecutingAssembly());
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            var td = new DirectN.TaskDialog
            {
                Title = Text,
                CommonButtonFlags = TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_CLOSE_BUTTON
            };

            MFFunctions.MFStartup();
            var hr = Functions.MFCreateVirtualCamera(
                __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0001.MFVirtualCameraType_SoftwareCameraSource,
                __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0002.MFVirtualCameraLifetime_Session,
                __MIDL___MIDL_itf_mfvirtualcamera_0000_0000_0003.MFVirtualCameraAccess_CurrentUser,
                Text,
                "{" + Shared.CLSID_VCamNet + "}",
                null,
                0,
                out var camera);
            if (hr.IsSuccess)
            {
                _camera = new ComObject<IMFVirtualCamera>(camera);
                hr = _camera.Object.Start(null);
            }

            if (hr.IsError)
            {
                td.MainInstruction = "VCamNet could not be started. Make sure you have registered the VCamNetSampleSource dll.\nPress Close to exit this program.";
                td.Content = $"Error {hr} {hr.Value} {new Win32Exception(hr.Value).Message}";
                td.MainIcon = DirectN.TaskDialog.TD_ERROR_ICON;
            }
            else
            {
                td.MainInstruction = "VCamNet was started, you can now run a program such as Windows Camera to visualize the output.\nPress Close to stop VCamNet and exit this program.";
                td.Content = "This may stop VCamNet access for visualizing programs too.";
                td.MainIcon = DirectN.TaskDialog.TD_INFORMATION_ICON;
            }
            td.Show(Handle);

            if (_camera != null)
            {
                hr = _camera.Object.Remove();
            }
            MFFunctions.MFShutdown();
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                MFFunctions.MFShutdown();
                components?.Dispose();
                _camera?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
