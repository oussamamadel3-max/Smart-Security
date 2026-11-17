using System.Text.Json;
using Domain.Constants;

namespace Application.Services.MqttServices;

/// <summary>
/// EVENT_PERSISTED JSON for one alarm phase. <c>queuedPhases</c> stays in the outbox row
/// so a second phase is not dropped, and is removed from the bytes published to the device.
/// A stored payload with no <c>alarmPhase</c> is treated as <see cref="AlarmEventDraft.AlarmPhase.Active"/>
/// and does not count as an acknowledgement of <see cref="AlarmEventDraft.AlarmPhase.Ended"/>.
/// </summary>
public static class EventPersistedPayload
{
    public const string QueuedPhasesProperty = "queuedPhases";

    public static string Build(Guid deviceId, Guid eventUid, string phase, IReadOnlyList<string> queued)
    {
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["schemaVersion"] = EventPersistedDraft.SchemaVersion,
            ["deviceId"] = deviceId,
            ["messageId"] = Guid.NewGuid(),
            ["messageType"] = EventPersistedDraft.MqttMessageType,
            ["eventUid"] = eventUid,
            ["stored"] = true,
            ["alarmPhase"] = phase,
            [QueuedPhasesProperty] = queued
        });
    }

    public static string Wire(string storedPayload)
    {
        using var doc = JsonDocument.Parse(storedPayload);
        var cleaned = new Dictionary<string, object?>();
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.NameEquals(QueuedPhasesProperty))
                continue;
            cleaned[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => property.Value.TryGetInt64(out var n) ? n : property.Value.GetDouble(),
                _ => property.Value.GetRawText()
            };
        }

        return JsonSerializer.Serialize(cleaned);
    }

    public static List<string> ReadQueue(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.TryGetProperty(QueuedPhasesProperty, out var queued)
                && queued.ValueKind == JsonValueKind.Array)
            {
                var phases = new List<string>();
                foreach (var item in queued.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && IsPhase(item.GetString()))
                        phases.Add(item.GetString()!);
                }

                if (phases.Count > 0)
                    return phases;
            }

            if (root.TryGetProperty("alarmPhase", out var phaseEl)
                && phaseEl.ValueKind == JsonValueKind.String
                && IsPhase(phaseEl.GetString()))
            {
                return [phaseEl.GetString()!];
            }
        }
        catch (JsonException)
        {
        }

        return [AlarmEventDraft.AlarmPhase.Active];
    }

    public static bool TryReadExplicitPhase(string payload, out string phase)
    {
        phase = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("alarmPhase", out var phaseEl)
                && phaseEl.ValueKind == JsonValueKind.String
                && IsPhase(phaseEl.GetString()))
            {
                phase = phaseEl.GetString()!;
                return true;
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }

    public static bool IsPhase(string? value)
        => value is AlarmEventDraft.AlarmPhase.Active or AlarmEventDraft.AlarmPhase.Ended;
}
