import 'package:flutter/material.dart';

import '../../domain/admin_alert_outcome.dart';
import '../../domain/entities/admin_alert_entity.dart';
import '../admin_alert_facts.dart';
import '../admin_copy.dart';
import 'admin_widgets.dart';

class AdminAlertOutcomePanel extends StatelessWidget {
  const AdminAlertOutcomePanel({super.key, required this.alert});

  final AdminAlertEntity alert;

  @override
  Widget build(BuildContext context) {
    final failed = AdminAlertOutcome.deliveryFailed(alert);
    return AdminPanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(alert.title, style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          Text(alert.body),
          const SizedBox(height: 12),
          if (failed)
            const AdminNotice(
              text: AdminAlertFacts.deliveryFailed,
              danger: true,
            ),
          AdminKvGrid(
            entries: [
              (AdminCopy.requestLabel, AdminAlertFacts.requestLine(alert)),
              (AdminCopy.attemptLabel, AdminAlertFacts.attemptLine(alert)),
              (AdminCopy.deliveryLabel, AdminAlertFacts.deliveryLine(alert)),
              (AdminCopy.readLabel, AdminAlertFacts.readLine(alert)),
              (AdminCopy.alertId, _dash(alert.adminAlertId)),
              (AdminCopy.recipient, _dash(alert.recipientUserId)),
              (AdminCopy.notificationStatus, alert.notificationStatus ?? '—'),
              (AdminCopy.createdAt, formatAdminTime(alert.createdAt)),
            ],
          ),
        ],
      ),
    );
  }

  String _dash(String value) => value.isEmpty ? '—' : value;
}
