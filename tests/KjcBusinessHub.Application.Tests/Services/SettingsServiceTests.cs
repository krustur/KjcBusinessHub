using System;
using System.IO;
using KjcBusinessHub.Infrastructure.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace KjcBusinessHub.Application.Tests.Services;

public class SettingsServiceTests
{
    [Fact]
    public void Save_and_load_persists_source_document_folder_and_close_to_tray_setting()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"kjcbh-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var settingsFilePath = Path.Combine(tempDirectory, "settings.json");

        try
        {
            var sut = new SettingsService(settingsFilePath, NullLogger<SettingsService>.Instance)
            {
                SourceDocumentFolder = tempDirectory,
                CloseToSystemTray = true,
                FiscalStartMonth = 6,
            };

            sut.Save();

            var loaded = new SettingsService(settingsFilePath, NullLogger<SettingsService>.Instance);

            Assert.Equal(tempDirectory, loaded.SourceDocumentFolder);
            Assert.True(loaded.CloseToSystemTray);
            Assert.Equal(6, loaded.FiscalStartMonth);
            Assert.True(loaded.IsConfigured);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void FiscalStartMonth_defaults_to_1_when_not_set()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"kjcbh-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var settingsFilePath = Path.Combine(tempDirectory, "settings.json");

        try
        {
            var sut = new SettingsService(settingsFilePath, NullLogger<SettingsService>.Instance);
            Assert.Equal(1, sut.FiscalStartMonth);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
