using Microsoft.AspNetCore.DataProtection;
using SmartX.Api.Data;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

public sealed class AttachmentService(
    IWebHostEnvironment environment,
    IDataProtectionProvider protectionProvider,
    SmartXDbContext db)
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("SmartX.DiagnosticAttachments.v1");

    public async Task<AttachmentRecord> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(environment.ContentRootPath, "App_Data", "ProtectedUploads");
        Directory.CreateDirectory(root);

        await using var source = file.OpenReadStream();
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken);

        // Protect() adds cryptographic protection to the diagnostic payload before local
        // persistence. This is a simulation-friendly use of ASP.NET Core Data Protection
        // (Microsoft, 2026c).
        var protectedText = _protector.Protect(Convert.ToBase64String(memory.ToArray()));
        var protectedName = $"{Guid.NewGuid():N}.protected";
        var protectedPath = Path.Combine(root, protectedName);
        await File.WriteAllTextAsync(protectedPath, protectedText, cancellationToken);

        var record = new AttachmentRecord
        {
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType ?? "application/octet-stream",
            ProtectedPath = protectedName,
            SizeBytes = file.Length
        };

        db.Attachments.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return record;
    }
}
