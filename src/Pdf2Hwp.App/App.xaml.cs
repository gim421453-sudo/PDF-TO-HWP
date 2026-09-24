using Pdf2Hwp.Infrastructure;

namespace Pdf2Hwp.App;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Only marked PDF2HWP workspaces below the user's TEMP directory are eligible.
        _ = new ConversionWorkspaceFactory().CleanupStale(TimeSpan.FromDays(7));
        base.OnStartup(e);
    }
}
