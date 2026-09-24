using Pdf2Hwp.Core;

namespace Pdf2Hwp.Infrastructure;

// Deliberately non-automating: product-specific COM/UI automation requires an explicit, licensed policy decision.
public sealed class HancomOpenIntegrationTest : IHwpxOpenIntegrationTest
{
    public Task<ValidationReport> VerifyOpenAsync(string hwpxPath, CancellationToken cancellationToken) => Task.FromResult(new ValidationReport(ValidationStatus.Warning,
        [new ValidationIssue(ValidationStatus.Warning, null, "HANCOM_MANUAL_VERIFICATION_REQUIRED", "Hancom is detected but no undocumented UI/COM automation was attempted.")]));
}
