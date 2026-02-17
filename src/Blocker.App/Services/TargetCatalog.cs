using Blocker.App.Models;

namespace Blocker.App.Services;

public sealed class TargetCatalog
{
    public BlockTargets GetDefaultTargets()
    {
        return new BlockTargets
        {
            Domains = new[]
            {
                "discord.com",
                "www.discord.com",
                "discordapp.com",
                "www.discordapp.com",
                "cdn.discordapp.com",
                "media.discordapp.net",
                "gateway.discord.gg",
                "discord.gg",
                "messenger.com",
                "www.messenger.com",
                "facebook.com",
                "www.facebook.com",
                "m.facebook.com",
                "mbasic.facebook.com",
                "web.facebook.com",
                "fbcdn.net",
                "www.fbcdn.net",
                "static.xx.fbcdn.net",
                "graph.facebook.com",
                "edge-mqtt.facebook.com"
            },
            ProcessNames = new[]
            {
                "Discord",
                "DiscordCanary",
                "DiscordPTB",
                "Update",
                "Messenger",
                "MessengerDesktop"
            },
            ExecutableHints = BuildExecutableHints()
        };
    }

    private static IReadOnlyList<string> BuildExecutableHints()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        return new[]
        {
            Path.Combine(localAppData, "Discord", "Update.exe"),
            Path.Combine(localAppData, "Discord", "app-*", "Discord.exe"),
            Path.Combine(localAppData, "DiscordCanary", "app-*", "DiscordCanary.exe"),
            Path.Combine(localAppData, "DiscordPTB", "app-*", "DiscordPTB.exe"),
            Path.Combine(localAppData, "Programs", "Discord", "Discord.exe"),
            Path.Combine(localAppData, "Programs", "Messenger", "Messenger.exe"),
            Path.Combine(localAppData, "Programs", "Facebook Messenger", "Messenger.exe"),
            Path.Combine(programFiles, "Messenger", "Messenger.exe"),
            Path.Combine(programFilesX86, "Messenger", "Messenger.exe")
        };
    }
}
