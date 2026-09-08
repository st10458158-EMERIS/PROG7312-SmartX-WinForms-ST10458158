namespace SmartX.WinForms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        // The Windows Forms message loop owns MainForm for the lifetime of the desktop UI.
        // Layout controls are used so the interface can adapt when the form is resized
        // (Microsoft, 2025a).
        Application.Run(new Forms.MainForm());
    }
}
