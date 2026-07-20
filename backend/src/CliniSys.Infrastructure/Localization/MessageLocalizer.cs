using System.Globalization;
using System.Reflection;
using System.Text.Json;
using CliniSys.Application.Common.Interfaces;

namespace CliniSys.Infrastructure.Localization;

internal class MessageLocalizer : IMessageLocalizer
{
    private static readonly string[] SupportedLocales = ["en-US", "pt-BR", "es-ES"];
    private readonly Dictionary<string, JsonElement> _messages;

    public MessageLocalizer()
    {
        var locale = CultureInfo.CurrentUICulture.Name;
        if (!SupportedLocales.Contains(locale)) locale = "en-US";

        var appAssembly = Assembly.Load("CliniSys.Application");
        var resourceName = $"CliniSys.Application.Locales.{locale}.json";

        using var stream = appAssembly.GetManifestResourceStream(resourceName)
            ?? appAssembly.GetManifestResourceStream("CliniSys.Application.Locales.en-US.json")!;
        using var reader = new StreamReader(stream);
        var doc = JsonDocument.Parse(reader.ReadToEnd());
        _messages = FlattenJson(doc.RootElement, string.Empty);
    }

    public string this[string key] =>
        _messages.TryGetValue(key, out var v) ? v.GetString() ?? key : key;

    private static Dictionary<string, JsonElement> FlattenJson(JsonElement element, string prefix)
    {
        var result = new Dictionary<string, JsonElement>();
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var prop in element.EnumerateObject())
            {
                var fullKey = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                foreach (var kv in FlattenJson(prop.Value, fullKey))
                    result[kv.Key] = kv.Value;
            }
        else
            result[prefix] = element;
        return result;
    }
}
