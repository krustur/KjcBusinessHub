using System.Security.Cryptography;
using System.Text.RegularExpressions;
using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace KjcBusinessHub.Application.Services;

public partial class SourceDocumentImportService(
    ISourceDocumentRepository repository,
    ILogger<SourceDocumentImportService> logger)
{
    [GeneratedRegex(@"^\d{4}-\d{2}$", RegexOptions.Compiled)]
    private static partial Regex MonthFolderPattern();

    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2}) (.+)$", RegexOptions.Compiled)]
    private static partial Regex FileNamePattern();

    public async Task ImportAsync(string sourceDocumentFolder, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceDocumentFolder))
        {
            logger.LogError("SourceDocumentFolder not found: {Folder}.", sourceDocumentFolder);
            return;
        }

        var scannedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var subDir in Directory.GetDirectories(sourceDocumentFolder))
        {
            var dirName = Path.GetFileName(subDir);

            if (!MonthFolderPattern().IsMatch(dirName))
            {
                logger.LogError("Incorrectly named subfolder ignored: {DirName}.", dirName);
                continue;
            }

            // Recursively collect all files in this month folder (any depth)
            foreach (var filePath in Directory.EnumerateFiles(subDir, "*", SearchOption.AllDirectories))
            {
                var fileSubPath = Path.GetRelativePath(sourceDocumentFolder, filePath)
                    .Replace('\\', '/');

                await ProcessFileAsync(sourceDocumentFolder, filePath, fileSubPath, cancellationToken);
                scannedPaths.Add(fileSubPath);
            }
        }

        // Handle files directly in root (only the Transactions file is expected here; everything else is warned)
        foreach (var filePath in Directory.GetFiles(sourceDocumentFolder))
        {
            var fileName = Path.GetFileName(filePath);
            if (string.Equals(fileName, TransactionImportService.TransactionsFileName, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Ignoring Transactions file: {FileName}.", fileName);
                continue;
            }
            logger.LogWarning("File outside correct subfolder path ignored: {FilePath}.", filePath);
        }

        // Mark removed
        var allDocs = await repository.GetAllAsync(cancellationToken);
        foreach (var doc in allDocs)
        {
            if (doc.Status is SourceDocumentStatus.RemovedFromDisk or SourceDocumentStatus.Removed)
            {
                continue;
            }

            if (!scannedPaths.Contains(doc.FileSubPath))
            {
                doc.Status = SourceDocumentStatus.RemovedFromDisk;
                doc.UpdatedAt = DateTimeOffset.UtcNow;
                await repository.UpdateAsync(doc, cancellationToken);
                logger.LogWarning("SourceDocument removed from disk: {FileSubPath}.", doc.FileSubPath);
            }
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessFileAsync(
        string sourceDocumentFolder,
        string filePath,
        string fileSubPath,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileNameWithoutExtension(Path.GetFileName(filePath));
        var nameMatch = FileNamePattern().Match(fileName);
        if (!nameMatch.Success)
        {
            logger.LogWarning("File outside correct path (bad name format): {FilePath}.", filePath);
            return;
        }

        var fileNameDate = DateOnly.ParseExact(nameMatch.Groups[1].Value, "yyyy-MM-dd");
        var description = nameMatch.Groups[2].Value;

        var fileInfo = new FileInfo(filePath);
        var fileCreatedDate = new DateTimeOffset(fileInfo.CreationTimeUtc, TimeSpan.Zero);
        var fileModifiedDate = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero);

        var hash = await ComputeHashAsync(filePath, cancellationToken);

        var existing = await repository.FindByFileSubPathAsync(fileSubPath, cancellationToken);
        if (existing is not null)
        {
            // Name match found
            bool changed = false;
            if (existing.FileHash != hash)
            {
                existing.FileHash = hash;
                existing.Status = SourceDocumentStatus.Changed;
                changed = true;
                logger.LogWarning("SourceDocument changed on disk: {FileSubPath}.", fileSubPath);
            }
            if (existing.FileCreatedDate != fileCreatedDate || existing.FileModifiedDate != fileModifiedDate)
            {
                existing.FileCreatedDate = fileCreatedDate;
                existing.FileModifiedDate = fileModifiedDate;
                changed = true;
            }
            if (changed)
            {
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await repository.UpdateAsync(existing, cancellationToken);
            }
        }
        else
        {
            // Check if it can be 'revived' by hash — use the first removed match
            var byHash = await repository.FindByFileHashAsync(hash, cancellationToken);
            var toRevive = byHash.FirstOrDefault(d =>
                d.Status is SourceDocumentStatus.RemovedFromDisk or SourceDocumentStatus.Removed);

            if (toRevive is not null)
            {
                toRevive.FileSubPath = fileSubPath;
                toRevive.FileNameDate = fileNameDate;
                toRevive.Description = description;
                toRevive.FileCreatedDate = fileCreatedDate;
                toRevive.FileModifiedDate = fileModifiedDate;
                toRevive.Status = SourceDocumentStatus.Revived;
                toRevive.UpdatedAt = DateTimeOffset.UtcNow;
                await repository.UpdateAsync(toRevive, cancellationToken);
                logger.LogInformation("SourceDocument revived by hash: {FileSubPath}.", fileSubPath);
                return;
            }

            // New document
            var doc = new SourceDocument
            {
                Id = Guid.NewGuid(),
                FileSubPath = fileSubPath,
                FileHash = hash,
                FileNameDate = fileNameDate,
                Description = description,
                Amount = null,
                Status = SourceDocumentStatus.New,
                FileCreatedDate = fileCreatedDate,
                FileModifiedDate = fileModifiedDate,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            await repository.AddAsync(doc, cancellationToken);
            logger.LogInformation("New SourceDocument added: {FileSubPath}.", fileSubPath);
        }
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hashBytes);
    }
}
