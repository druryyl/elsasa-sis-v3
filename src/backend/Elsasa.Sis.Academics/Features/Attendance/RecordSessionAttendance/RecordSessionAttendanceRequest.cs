namespace Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Student attendance entry payload for roll-call recording (P4-S08, UC-ACD-03).
/// </summary>
/// <param name="StudentId">Unique identifier of the participating student.</param>
/// <param name="Status">Attendance status ('present', 'absent', 'late', 'excused_absence', 'partial').</param>
/// <param name="Evidence">Optional supporting evidence (e.g., medical note reference, doctor certificate link).</param>
/// <param name="Notes">Optional attendance remark or context.</param>
public sealed record StudentAttendanceEntry(
    Guid StudentId,
    string Status,
    string? Evidence = null,
    string? Notes = null);

/// <summary>
/// Request payload for recording session roll-call attendance (P4-S08, UC-ACD-03).
/// Supports both JSON object with an <c>entries</c> array and a top-level JSON array of entries.
/// </summary>
/// <param name="Entries">Collection of student attendance entries.</param>
[JsonConverter(typeof(RecordSessionAttendanceRequestJsonConverter))]
public sealed record RecordSessionAttendanceRequest(
    IReadOnlyList<StudentAttendanceEntry> Entries)
{
    /// <summary>
    /// Default constructor initializing an empty entries collection.
    /// </summary>
    public RecordSessionAttendanceRequest()
        : this([])
    {
    }
}

/// <summary>
/// JSON converter enabling flexible deserialization from either an object with <c>entries</c> or a root array.
/// </summary>
public sealed class RecordSessionAttendanceRequestJsonConverter : JsonConverter<RecordSessionAttendanceRequest>
{
    public override RecordSessionAttendanceRequest? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var entries = JsonSerializer.Deserialize<List<StudentAttendanceEntry>>(ref reader, options);
            return new RecordSessionAttendanceRequest(entries ?? (IReadOnlyList<StudentAttendanceEntry>)[]);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.TryGetProperty("entries", out var entriesProp) ||
                doc.RootElement.TryGetProperty("Entries", out entriesProp))
            {
                var entries = entriesProp.Deserialize<List<StudentAttendanceEntry>>(options);
                return new RecordSessionAttendanceRequest(entries ?? (IReadOnlyList<StudentAttendanceEntry>)[]);
            }

            return new RecordSessionAttendanceRequest([]);
        }

        throw new JsonException("Expected JSON array or JSON object containing an 'entries' property.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        RecordSessionAttendanceRequest value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WritePropertyName("entries");
        JsonSerializer.Serialize(writer, value.Entries, options);
        writer.WriteEndObject();
    }
}
