import '../domain/admin_alert_outcome.dart';
import '../domain/entities/admin_alert_entity.dart';
import 'widgets/admin_widgets.dart';

/// Arabic lines that keep save, attempt, provider result, and read apart.
abstract final class AdminAlertFacts {
  static const requestAccepted =
      'قُبل حفظ التنبيه على الخادم. هذا ليس نجاح تسليم.';
  static const requestMissing = 'لا يوجد معرف تنبيه في استجابة الخادم.';
  static const noAttempt = 'لا محاولة تسليم مسجّلة (لا وقت محاولة من الخادم).';
  static const deliveryAccepted =
      'قبل المزوّد محاولة التسليم (ACCEPTED). هذا ليس دليل قراءة.';
  static const deliveryFailed =
      'فشلت محاولة التسليم (FAILED). التنبيه يبقى للمراجعة.';
  static const deliveryPending = 'محاولة التسليم لم تكتمل (PENDING).';
  static const deliveryCancelled = 'أُلغي طلب الإشعار (CANCELLED).';
  static const noReadEvidence = 'لا دليل قراءة من الخادم.';
  static const serverRead = 'الخادم أعاد دليل قراءة.';
  static const serverUnread = 'الخادم أعاد أن القراءة لم تُسجَّل.';
  static const shortAccepted = 'قبول المزوّد';
  static const shortFailed = 'فشل التسليم';
  static const shortPending = 'معلّق';
  static const shortCancelled = 'ملغى';

  static String requestLine(AdminAlertEntity alert) {
    return AdminAlertOutcome.requestAccepted(alert)
        ? requestAccepted
        : requestMissing;
  }

  static String attemptLine(AdminAlertEntity alert) {
    if (!AdminAlertOutcome.attemptRecorded(alert)) return noAttempt;
    return 'سُجّلت محاولة تسليم في ${formatAdminTime(alert.lastAttemptAt)}.';
  }

  static String deliveryLine(AdminAlertEntity alert) {
    if (AdminAlertOutcome.deliverySucceeded(alert)) return deliveryAccepted;
    if (AdminAlertOutcome.deliveryFailed(alert)) return deliveryFailed;
    if (AdminAlertOutcome.deliveryCancelled(alert)) return deliveryCancelled;
    final status = alert.deliveryAttemptStatus;
    if (status == AdminAlertOutcome.accepted ||
        status == AdminAlertOutcome.failed) {
      return 'وصل الرمز $status دون دليل محاولة. لا يُحتسب نتيجة تسليم.';
    }
    return deliveryPending;
  }

  static String readLine(AdminAlertEntity alert) {
    if (alert.readClaimed) return serverRead;
    if (alert.isRead == false) return serverUnread;
    return noReadEvidence;
  }

  static String shortStatus(AdminAlertEntity alert) {
    if (AdminAlertOutcome.deliverySucceeded(alert)) return shortAccepted;
    if (AdminAlertOutcome.deliveryFailed(alert)) return shortFailed;
    if (AdminAlertOutcome.deliveryCancelled(alert)) return shortCancelled;
    return shortPending;
  }
}
