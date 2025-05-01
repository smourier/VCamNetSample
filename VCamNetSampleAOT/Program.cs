namespace VCamNetSampleAOT;

internal static class Program
{
    static void Main()
    {
        using var app = new Application();
        using var win = new SampleWindow("VCamNetSample AOT");
        win.ResizeClient(600, 400);
        win.Center();
        win.Show();
        win.SetForeground();
        app.Run();
    }
}