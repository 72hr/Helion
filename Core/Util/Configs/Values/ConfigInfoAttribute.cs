using System;

namespace Helion.Util.Configs.Values;

/// <summary>
/// Metadata for a config component or value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ConfigInfoAttribute(string description, bool save = true, bool serialize = false, bool demo = false, bool mapRestartRequired = false,
    bool restartRequired = false, bool legacy = false, string? consoleAlias = null) : Attribute
{
    /// <summary>
    /// A high level description of the attribute.
    /// </summary>
    public readonly string Description = description;

    /// <summary>
    /// If true, saves to the config. If false, never saves.
    /// </summary>
    /// <remarks>
    /// If this is false, it means it is a transient field whereby it can
    /// be toggled via the console, but will never be saved. Upon loading
    /// the game again, it will always have the default definition.
    /// </remarks>
    public readonly bool Save = save;

    /// <summary>
    /// If true, serializes to the world state (save games).
    /// </summary>
    public readonly bool Serialize = serialize;

    // If true this option is serialized for demos.
    public readonly bool Demo = demo;

    // If the map needs to be restarted to take effect.
    public readonly bool MapRestartRequired = mapRestartRequired;

    // If the application needs to be restarted to take effect.
    public readonly bool RestartRequired = restartRequired;

    public readonly bool Legacy = legacy;

    public readonly string? ConsoleAlias = consoleAlias;

    public bool GetSetWarningString(out string message)
    {
        message = string.Empty;
        if (MapRestartRequired)
            message = "Map restart required for this change to take effect.";
        if (RestartRequired)
            message = "App restart required for this change to take effect.";
        return message.Length > 0;
    }
}
