namespace VCamNetSampleAOT;

public class SampleWindow : Window
{
    private ComObject<IMFVirtualCamera>? _camera;

    public SampleWindow(string? title = null)
        : base(title, WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_SYSMENU)
    {
        RunTaskOnUIThread(() =>
        {
            var td = new TaskDialog
            {
                Title = title,
                CommonButtonFlags = TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_CLOSE_BUTTON
            };

            Functions.MFStartup(Constants.MF_VERSION, 0).ThrowOnError();

            var hr = Functions.MFCreateVirtualCamera(MFVirtualCameraType.MFVirtualCameraType_SoftwareCameraSource,
                MFVirtualCameraLifetime.MFVirtualCameraLifetime_Session,
                MFVirtualCameraAccess.MFVirtualCameraAccess_CurrentUser,
                PWSTR.From(title),
                PWSTR.From($"{{{Shared.CLSID_VCamNet}}}"),
                0,
                0,
                out var camera);
            if (hr.IsSuccess)
            {
                _camera = new ComObject<IMFVirtualCamera>(camera);
                hr = _camera.Object.Start(null);
            }

            if (hr.IsError)
            {
                td.MainInstruction = "VCamNet AOT could not be started. Make sure you have registered the VCamNetSampleSourceAOT dll.\nPress Close to exit this program.";
                td.Content = $"Error {hr} {hr.Value} {new Win32Exception(hr.Value).Message}";
                td.MainIcon = TaskDialog.TD_ERROR_ICON;
            }
            else
            {
                td.MainInstruction = "VCamNet AOT was started, you can now run a program such as Windows Camera to visualize the output.\nPress Close to stop VCamNet AOT and exit this program.";
                td.Content = "This may stop VCamNet AOT access for visualizing programs too.";
                td.MainIcon = TaskDialog.TD_INFORMATION_ICON;
            }

            if (_camera != null)
            {
                hr = _camera.Object.Remove();
            }

            td.Show(Handle);
            Functions.MFShutdown().ThrowOnError();
            Dispose();
        }, true);
    }
}
