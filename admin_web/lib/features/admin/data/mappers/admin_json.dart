DateTime? readDateTime(dynamic value) {
  if (value is! String || value.trim().isEmpty) return null;
  return DateTime.tryParse(value);
}

DateTime readRequiredDateTime(dynamic value) =>
    readDateTime(value) ?? DateTime.fromMillisecondsSinceEpoch(0, isUtc: true);

String readString(dynamic value) {
  if (value == null) return '';
  return value.toString().trim();
}

String? readOptionalString(dynamic value) {
  final text = readString(value);
  return text.isEmpty ? null : text;
}

int readInt(dynamic value) {
  if (value is num) return value.toInt();
  return int.tryParse('$value') ?? 0;
}

bool readBool(dynamic value) => value == true;

Map<String, dynamic> readMap(dynamic json) {
  if (json is Map<String, dynamic>) return json;
  if (json is Map) return Map<String, dynamic>.from(json);
  throw const FormatException('Expected object');
}

List<Map<String, dynamic>> readMapList(dynamic json) {
  if (json is! List) throw const FormatException('Expected list');
  return json.whereType<Map>().map(Map<String, dynamic>.from).toList();
}
