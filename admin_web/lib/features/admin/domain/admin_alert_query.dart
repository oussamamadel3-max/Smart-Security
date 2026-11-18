/// Query accepted by `GET /api/admin/alerts`.
class AdminAlertQuery {
  const AdminAlertQuery({
    this.recipientUserId,
    this.senderUserId,
    this.page = 1,
    this.pageSize = 20,
  });

  final String? recipientUserId;
  final String? senderUserId;
  final int page;
  final int pageSize;

  AdminAlertQuery copyWith({
    String? recipientUserId,
    String? senderUserId,
    int? page,
    int? pageSize,
    bool clearRecipient = false,
    bool clearSender = false,
  }) {
    return AdminAlertQuery(
      recipientUserId: clearRecipient
          ? null
          : (recipientUserId ?? this.recipientUserId),
      senderUserId: clearSender ? null : (senderUserId ?? this.senderUserId),
      page: page ?? this.page,
      pageSize: pageSize ?? this.pageSize,
    );
  }

  Map<String, dynamic> toQueryParameters() {
    final params = <String, dynamic>{
      'page': page < 1 ? 1 : page,
      'pageSize': pageSize < 1 ? 20 : pageSize,
    };
    final recipient = recipientUserId?.trim();
    final sender = senderUserId?.trim();
    if (recipient != null && recipient.isNotEmpty) {
      params['recipientUserId'] = recipient;
    }
    if (sender != null && sender.isNotEmpty) params['senderUserId'] = sender;
    return params;
  }
}
