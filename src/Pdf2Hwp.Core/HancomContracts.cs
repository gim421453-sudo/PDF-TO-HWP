namespace Pdf2Hwp.Core;

public sealed record HancomInstallation(string DisplayName, string? Version, string? InstallLocation);
public interface IHancomInstallationDetector { HancomInstallation? Detect(); }
public interface IHwpxOpenIntegrationTest { Task<ValidationReport> VerifyOpenAsync(string hwpxPath, CancellationToken cancellationToken); }
