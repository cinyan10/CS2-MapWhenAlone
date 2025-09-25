using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class SoloMapChange : BasePlugin
{
    public override string ModuleName => "Solo Map Changer";
    public override string ModuleVersion => "1.3";
    public override string ModuleAuthor => "Cinyan10";

    private Dictionary<string, string> _workshopIds = new();

    public override void Load(bool hotReload)
    {
        LoadMaps();
        AddCommand("map", "Change map if only one player online", OnMapCommand);
    }

    private void LoadMaps()
    {
        try
        {
            string path = Path.Combine(Server.GameDirectory, "csgo", "cfg", "GGMCmaps.json");
            if (!File.Exists(path))
            {
                Console.WriteLine($"[SoloMapChange] JSON not found: {path}");
                return;
            }

            string json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                string mapName = prop.Name.ToLower();
                string workshopId = prop.Value.GetProperty("mapid").GetRawText();
                _workshopIds[mapName] = workshopId;
            }

            Console.WriteLine($"[SoloMapChange] Loaded {_workshopIds.Count} workshop maps.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SoloMapChange] Error loading JSON: {ex.Message}");
        }
    }

    private void OnMapCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid)
            return;

        if (info.ArgCount < 2)
        {
            player.PrintToChat("Usage: !map <mapname>");
            return;
        }

        int playerCount = Utilities.GetPlayers().Count(p => p != null && p.IsValid && !p.IsBot);
        if (playerCount != 1)
        {
            player.PrintToChat("⚠️ You can only change the map when you are the only player online.");
            return;
        }

        string input = info.ArgByIndex(1).ToLower();

        // exact match
        if (_workshopIds.ContainsKey(input))
        {
            StartDelayedChange(player, input, _workshopIds[input]);
            return;
        }

        // partial matches
        var matches = _workshopIds.Keys.Where(m => m.Contains(input)).ToList();

        if (matches.Count == 0)
        {
            player.PrintToChat($"❌ No maps found for '{input}'.");
            return;
        }
        if (matches.Count == 1)
        {
            StartDelayedChange(player, matches[0], _workshopIds[matches[0]]);
            return;
        }
        if (matches.Count <= 7)
        {
            player.PrintToChat($"⚠️ Multiple matches for '{input}': {string.Join(", ", matches)}");
            return;
        }

        player.PrintToChat($"❌ Too many matches for '{input}', please be more specific.");
    }

    private void StartDelayedChange(CCSPlayerController player, string mapName, string workshopId)
    {
        player.PrintToChat($"⏳ Changing to {mapName} in 5 seconds...");

        AddTimer(5.0f, () =>
        {
            if (!string.IsNullOrEmpty(workshopId))
            {
                Server.PrintToChatAll($"✅ Changing map to workshop {mapName} (ID {workshopId})...");
                Server.ExecuteCommand($"host_workshop_map {workshopId}");
            }
            else
            {
                Server.PrintToChatAll($"✅ Changing map to {mapName}...");
                Server.ExecuteCommand($"changelevel {mapName}");
            }
        });
    }
}
