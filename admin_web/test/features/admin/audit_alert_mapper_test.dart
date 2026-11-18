import 'package:admin_web/features/admin/data/mappers/audit_alert_mapper.dart';
import 'package:admin_web/features/admin/domain/admin_alert_outcome.dart';
import 'package:admin_web/features/admin/domain/audit_log_query.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('audit page keeps transfer and archive identifiers', () {
    final page = AuditAlertMapper.auditPage({
      'items': [
        {
          'id': 112,
          'actorUserId': 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          'action': 'OWNERSHIP_TRANSFERRED',
          'targetType': 'OWNERSHIP',
          'targetId': 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          'deviceId': 'cccccccc-cccc-cccc-cccc-cccccccccccc',
          'result': 'SUCCESS',
          'occurredAt': '2026-11-18T08:20:00Z',
          'details': '{"toOwnerId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"}',
        },
        {
          'id': '90',
          'action': 'DEVICE_ARCHIVED',
          'targetType': 'DEVICE',
          'targetId': 'dddddddd-dddd-dddd-dddd-dddddddddddd',
          'deviceId': 'dddddddd-dddd-dddd-dddd-dddddddddddd',
          'result': 'SUCCESS',
          'occurredAt': '2026-11-18T09:00:00Z',
        },
      ],
      'page': 2,
      'pageSize': 20,
      'totalCount': 40,
    });

    expect(page.page, 2);
    expect(page.totalCount, 40);
    expect(page.items.first.action, 'OWNERSHIP_TRANSFERRED');
    expect(page.items.first.targetId, 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
    expect(page.items.first.deviceId, 'cccccccc-cccc-cccc-cccc-cccccccccccc');
    expect(page.items.last.id, '90');
    expect(page.items.last.targetId, 'dddddddd-dddd-dddd-dddd-dddddddddddd');
    expect(page.hasNext, isFalse);
    expect(page.hasPrevious, isTrue);
  });

  test('query parameters follow the audit API and omit blanks', () {
    final params = AuditLogQuery(
      q: '  ',
      action: 'OWNERSHIP_TRANSFERRED',
      deviceId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
      page: 3,
      sortBy: 'not-allowed',
      sortDir: 'asc',
      from: DateTime.utc(2026, 11, 18, 8),
    ).toQueryParameters();

    expect(params.containsKey('q'), isFalse);
    expect(params['action'], 'OWNERSHIP_TRANSFERRED');
    expect(params['deviceId'], 'cccccccc-cccc-cccc-cccc-cccccccccccc');
    expect(params['page'], 3);
    expect(params['sortBy'], 'occurredAt');
    expect(params['sortDir'], 'asc');
    expect(params['from'], '2026-11-18T08:00:00.000Z');
  });

  test('accepted without an attempt is not delivery success or read', () {
    final alert = AuditAlertMapper.adminAlert({
      'adminAlertId': '11111111-1111-1111-1111-111111111111',
      'senderUserId': '22222222-2222-2222-2222-222222222222',
      'recipientUserId': '33333333-3333-3333-3333-333333333333',
      'title': 'تنبيه',
      'body': 'يبقى للمراجعة',
      'createdAt': '2026-11-18T01:00:00Z',
      'notificationStatus': 'ACCEPTED',
      'deliveryAttemptStatus': 'ACCEPTED',
      'deliveryAttempted': false,
    });

    expect(AdminAlertOutcome.requestAccepted(alert), isTrue);
    expect(AdminAlertOutcome.attemptRecorded(alert), isFalse);
    expect(AdminAlertOutcome.deliverySucceeded(alert), isFalse);
    expect(alert.readClaimed, isFalse);
    expect(alert.hasReadEvidence, isFalse);
  });

  test('failed attempt stays reviewable and does not claim read', () {
    final alert = AuditAlertMapper.adminAlert({
      'adminAlertId': '11111111-1111-1111-1111-111111111111',
      'senderUserId': '22222222-2222-2222-2222-222222222222',
      'recipientUserId': '33333333-3333-3333-3333-333333333333',
      'title': 'فشل التسليم',
      'body': 'النص الأصلي',
      'createdAt': '2026-11-18T01:00:00Z',
      'notificationStatus': 'FAILED',
      'deliveryAttemptStatus': 'FAILED',
      'deliveryAttempted': true,
      'lastAttemptAt': '2026-11-18T01:05:00Z',
    });

    expect(AdminAlertOutcome.deliveryFailed(alert), isTrue);
    expect(AdminAlertOutcome.deliverySucceeded(alert), isFalse);
    expect(alert.body, 'النص الأصلي');
    expect(alert.readClaimed, isFalse);
  });

  test('explicit server read evidence is the only read claim', () {
    final unread = AuditAlertMapper.adminAlert({
      'adminAlertId': '11111111-1111-1111-1111-111111111111',
      'senderUserId': '22222222-2222-2222-2222-222222222222',
      'recipientUserId': '33333333-3333-3333-3333-333333333333',
      'title': 'تنبيه',
      'body': 'نص',
      'createdAt': '2026-11-18T01:00:00Z',
      'deliveryAttemptStatus': 'PENDING',
      'deliveryAttempted': false,
      'isRead': false,
    });
    final read = AuditAlertMapper.adminAlert({
      'adminAlertId': '11111111-1111-1111-1111-111111111111',
      'senderUserId': '22222222-2222-2222-2222-222222222222',
      'recipientUserId': '33333333-3333-3333-3333-333333333333',
      'title': 'تنبيه',
      'body': 'نص',
      'createdAt': '2026-11-18T01:00:00Z',
      'deliveryAttemptStatus': 'PENDING',
      'deliveryAttempted': false,
      'readAt': '2026-11-18T02:00:00Z',
    });

    expect(unread.readClaimed, isFalse);
    expect(unread.isRead, isFalse);
    expect(read.readClaimed, isTrue);
    expect(read.readAt, isNotNull);
  });
}
