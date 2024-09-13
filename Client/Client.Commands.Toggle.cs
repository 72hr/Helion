using Helion.Util.Configs.Components;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles.Commands;
using Helion.Util.Consoles;
using Helion.Util.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Client;

public partial class Client
{
    /// <summary>
    /// Cycles a config item's value through the provided values (optional if boolean)
    /// </summary>
    [ConsoleCommand("toggle", "Toggles a config item")]
    private void Toggle(ConsoleCommandEventArgs args)
    {
        string? configItem = args.Args.FirstOrDefault();
        if (configItem == null)
        {
            HelionLog.Error("Config item not provided");
            return;
        }
        if (!m_config.TryGetComponent(configItem, out ConfigComponent? component))
        {
            HelionLog.Error($"Config item {configItem} not found");
            return;
        }

        // if boolean, allow toggling without providing values
        if (args.Args.Count == 1)
        {
            if (component.Value is ConfigValue<bool> boolConfigVal)
                TryHandleConfigVariableCommand(new ConsoleCommandEventArgs($"{configItem} {!boolConfigVal.Value}"));
            else
            {
                HelionLog.Error($"Must provide values for {configItem}, since it is not a True/False config item");
                return;
            }
        }
        else
        {
            List<object> parsedValues = [];
            foreach (string arg in args.Args[1..])
            {
                if (!component.Value.TryParseNewObjectValue(arg, out object? parsedVal) || parsedVal == null)
                {
                    HelionLog.Error($"Invalid value {arg} provided for {configItem}");
                    return;
                }
                parsedValues.Add(parsedVal);
            }
            int nextIndex = (parsedValues.IndexOf(component.Value.ObjectValue) + 1) % parsedValues.Count;
            TryHandleConfigVariableCommand(new ConsoleCommandEventArgs($"{configItem} {parsedValues[nextIndex]}"));
        }
    }

    [ConsoleCommand("mouselook", "Toggle mouselook")]
    private void ToggleMouselook(ConsoleCommandEventArgs args)
    {
        m_config.Mouse.Look.Set(!m_config.Mouse.Look.Value);
    }

    [ConsoleCommand("autoaim", "Toggle auto aim")]
    private void ToggleAutoaim(ConsoleCommandEventArgs args)
    {
        m_config.Game.AutoAim.Set(!m_config.Game.AutoAim.Value);
    }


    [ConsoleCommand("screenshot", "Capture a screenshot")]
    private void Screenshot(ConsoleCommandEventArgs args)
    {
        m_takeScreenshot = true;
    }

    [ConsoleCommand("chasecam", "Toggles chase camera mode")]
    private void ToggleChaseCam(ConsoleCommandEventArgs args)
    {
        if (m_layerManager.WorldLayer == null)
            return;

        m_layerManager.WorldLayer.World.ToggleChaseCameraMode();
    }

    [ConsoleCommand("markspecials", "Toggles mark specials")]
    private void ToggleMarkSpecials(ConsoleCommandEventArgs args)
    {
        m_config.Game.MarkSpecials.Set(!m_config.Game.MarkSpecials.Value);
    }

    [ConsoleCommand("marksecrets", "Toggles mark secrets")]
    private void ToggleMarkSecrets(ConsoleCommandEventArgs args)
    {
        m_config.Game.MarkSecrets.Set(!m_config.Game.MarkSecrets.Value);
    }
}
