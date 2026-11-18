import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/features/devices/presentation/viewmodels/devices_list_view_model.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  test('loading/empty/error distinctions for list UI', () {
    expect(const DevicesListState(isLoading: true).isEmpty, isFalse);
    expect(const DevicesListState().isEmpty, isTrue);
    expect(
      DevicesListState(
        error: ErrorState(message: 'net', displayType: ErrorDisplayType.inline),
      ).isEmpty,
      isFalse,
    );
  });
}
