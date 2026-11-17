// =============================================================================
// result.dart
// =============================================================================
// نمط Result لتوحيد مخرجات العمليات في التطبيق.
//
// المبدأ:
// كل عملية تنتهي بإحدى حالتين:
// - Success<T> : تحتوي على البيانات المطلوبة.
// - Failure   : تحتوي على وصف الخطأ.
//
// الهدف:
// - منع انتشار Exceptions إلى الطبقات العليا.
// - جعل Repository و UseCase يعيدان نتيجة واضحة وقابلة للمعالجة.
// - تسهيل التعامل مع الأخطاء في UI.
//
// مثال:
// Result<User>
//      ↓
// Success(user)  أو  Failure(error)
// =============================================================================

import 'package:freezed_annotation/freezed_annotation.dart';
import '../failures/failure.dart';

part 'result.freezed.dart';

/// نتيجة عملية قد تنجح أو تفشل.
///
/// T يمثل نوع البيانات عند نجاح العملية.
@freezed
sealed class Result<T> with _$Result<T> {
  /// نتيجة ناجحة تحتوي على البيانات المطلوبة.
  const factory Result.success(T data) = Success<T>;

  /// نتيجة فاشلة تحتوي على سبب الخطأ.
  const factory Result.failure(Failure failure) = Failure_<T>;
}

// =============================================================================
// Result Extensions
// =============================================================================
// أدوات مساعدة لتسهيل التعامل مع Result بدون تكرار فحص الحالات.
// =============================================================================

extension ResultExtension<T> on Result<T> {
  /// هل تحتوي النتيجة على بيانات ناجحة؟
  bool get isSuccess => this is Success<T>;

  /// هل تحتوي النتيجة على Failure؟
  bool get isFailure => this is Failure_<T>;

  /// يعيد البيانات عند النجاح وإلا يعيد null.
  T? get dataOrNull => switch (this) {
    Success(:final data) => data,
    _ => null,
  };

  /// يعيد الخطأ عند الفشل وإلا يعيد null.
  Failure? get failureOrNull => switch (this) {
    Failure_(:final failure) => failure,
    _ => null,
  };

  /// تنفيذ منطق مختلف حسب حالة النتيجة.
  ///
  /// يستخدم لفصل مسار النجاح عن مسار الخطأ.
  ///
  /// مثال:
  /// ```dart
  /// result.fold(
  ///   onFailure: (failure) => showError(failure),
  ///   onSuccess: (data) => showData(data),
  /// );
  /// ```
  R fold<R>({
    required R Function(Failure failure) onFailure,
    required R Function(T data) onSuccess,
  }) {
    return switch (this) {
      Success(:final data) => onSuccess(data),
      Failure_(:final failure) => onFailure(failure),
    };
  }

  /// تحويل البيانات عند النجاح فقط.
  ///
  /// لا يغير حالة Failure.
  Result<R> map<R>(R Function(T data) transform) {
    return switch (this) {
      Success(:final data) => Result.success(transform(data)),

      Failure_(:final failure) => Result.failure(failure),
    };
  }

  /// يعيد البيانات عند النجاح،
  /// وإلا يعيد القيمة الافتراضية.
  T getOrElse(T defaultValue) => dataOrNull ?? defaultValue;
}
