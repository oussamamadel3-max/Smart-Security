/// Owner account as returned by `api/admin/owners` (Backend T-02).
class OwnerAccountEntity {
  const OwnerAccountEntity({
    required this.id,
    required this.fullName,
    required this.isActive,
    required this.createdAt,
    this.userName,
    this.email,
    this.deletedAt,
    this.updatedAt,
  });

  final String id;
  final String? userName;
  final String fullName;
  final String? email;
  final bool isActive;
  final DateTime? deletedAt;
  final DateTime createdAt;
  final DateTime? updatedAt;

  bool get isArchived => deletedAt != null;

  bool get isDisabled => !isActive && deletedAt == null;

  String get statusCode {
    if (isArchived) return 'ARCHIVED';
    if (!isActive) return 'DISABLED';
    return 'ACTIVE';
  }
}
