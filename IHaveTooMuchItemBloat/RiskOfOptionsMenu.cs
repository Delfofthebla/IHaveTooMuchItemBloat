using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using UnityEngine;

namespace IHaveTooMuchItemBloat;

internal static class RiskOfOptionsMenu
{
    public const string GUID = "com.rune580.riskofoptions";

    public static void Register(BaseUnityPlugin plugin, ManualLogSource log, Func<ConfigEntryBase, bool> isHidden = null)
    {
        if (!Chainloader.PluginInfos.ContainsKey(GUID))
            return;

        try
        {
            AddToMenu(plugin, isHidden ?? (_ => false));
        }
        catch (Exception exception)
        {
            log.LogWarning($"Failed to add settings to Risk of Options: {exception}");
        }
    }

    // Risk of Options' types are resolved when this method is compiled, which only happens when Risk of Options is loaded.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddToMenu(BaseUnityPlugin plugin, Func<ConfigEntryBase, bool> isHidden)
    {
        var guid = plugin.Info.Metadata.GUID;
        var name = plugin.Info.Metadata.Name;

        foreach (var entry in plugin.Config.Keys.Select(definition => plugin.Config[definition]).Where(entry => !isHidden(entry)))
        {
            var option = OptionFor(entry);
            if (option != null)
                ModSettingsManager.AddOption(option, guid, name);
        }

        var packageDirectory = Path.GetDirectoryName(plugin.Info.Location) ?? "";

        var description = ManifestDescription(packageDirectory);
        if (!string.IsNullOrEmpty(description))
            ModSettingsManager.SetModDescription(description, guid, name);

        var icon = LoadIcon(packageDirectory);
        if (icon)
            ModSettingsManager.SetModIcon(icon, guid, name);
    }

    private static BaseOption OptionFor(ConfigEntryBase entry) => entry switch
    {
        ConfigEntry<bool> toggle => new CheckBoxOption(toggle),
        ConfigEntry<int> number => IntOption(number),
        ConfigEntry<float> number => FloatOption(number),
        ConfigEntry<string> text => new StringInputFieldOption(text),
        ConfigEntry<KeyboardShortcut> shortcut => new KeyBindOption(shortcut),
        ConfigEntry<Color> color => new ColorOption(color),
        _ when IsDropdownEnum(entry.SettingType) => new ChoiceOption(entry),
        _ => null,
    };

    private static BaseOption IntOption(ConfigEntry<int> entry)
    {
        if (entry.Description.AcceptableValues is AcceptableValueRange<int> range)
            return new IntSliderOption(entry, new IntSliderConfig { min = range.MinValue, max = range.MaxValue });

        return new IntFieldOption(entry);
    }

    private static BaseOption FloatOption(ConfigEntry<float> entry)
    {
        if (entry.Description.AcceptableValues is AcceptableValueRange<float> range)
            return new SliderOption(entry, new SliderConfig { min = range.MinValue, max = range.MaxValue, FormatString = "{0:0.##}" });

        return new FloatFieldOption(entry);
    }

    // Risk of Options' dropdown treats an enum's numeric value as its position in the list.
    private static bool IsDropdownEnum(Type type)
    {
        if (!type.IsEnum || type.IsDefined(typeof(FlagsAttribute), false))
            return false;

        var values = Enum.GetValues(type).Cast<object>().Select(value => Convert.ToInt64(value, CultureInfo.InvariantCulture)).OrderBy(value => value);

        return values.SequenceEqual(Enumerable.Range(0, Enum.GetValues(type).Length).Select(position => (long)position));
    }

    private static string ManifestDescription(string packageDirectory)
    {
        var path = Path.Combine(packageDirectory, "manifest.json");
        if (!File.Exists(path))
            return null;

        return JsonUtility.FromJson<Manifest>(File.ReadAllText(path))?.description;
    }

    private static Sprite LoadIcon(string packageDirectory)
    {
        var path = Path.Combine(packageDirectory, "icon.png");
        if (!File.Exists(path))
            return null;

        var texture = new Texture2D(2, 2);
        if (!texture.LoadImage(File.ReadAllBytes(path)))
            return null;

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    [Serializable]
    private sealed class Manifest
    {
        public string description = "";
    }
}
