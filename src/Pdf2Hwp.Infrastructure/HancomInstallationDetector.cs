using Microsoft.Win32;
using Pdf2Hwp.Core;
using System.Runtime.Versioning;

namespace Pdf2Hwp.Infrastructure;

[SupportedOSPlatform("windows")]
public sealed class HancomInstallationDetector : IHancomInstallationDetector
{
    public HancomInstallation? Detect()
    {
        foreach (var root in new[] { Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"), Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"), Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall") })
        {
            using (root)
            {
                if (root is null) continue;
                foreach (var name in root.GetSubKeyNames())
                {
                    using var entry = root.OpenSubKey(name);
                    var displayName = entry?.GetValue("DisplayName") as string;
                    if (displayName is not null && (displayName.Contains("한컴", StringComparison.OrdinalIgnoreCase) || displayName.Contains("Hancom", StringComparison.OrdinalIgnoreCase)))
                        return new HancomInstallation(displayName, entry?.GetValue("DisplayVersion") as string, entry?.GetValue("InstallLocation") as string);
                }
            }
        }
        return null;
    }
}
