import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/features/wifi_provisioning/domain/local_provisioning_client.dart';
import 'package:security_mobile/features/wifi_provisioning/presentation/viewmodels/wifi_provisioning_view_model.dart';

void main() {
  test('default client never reports connected and clears password', () async {
    final client = UnavailableLocalProvisioningClient();
    final outcome = await client.submitWifi(
      const LocalWifiCredentials(ssid: 'home', password: 'secret-pass'),
    );
    expect(outcome, LocalProvisioningOutcome.unavailable);
    expect(client.retainsPasswordForTest, isFalse);
  });

  test('view model clears password before result and does not invent success',
      () async {
    final client = UnavailableLocalProvisioningClient();
    final vm = WifiProvisioningViewModel(client);
    vm.acknowledgePossession();
    vm.setSsid('home');
    vm.setPassword('secret-pass');
    await vm.submit();

    expect(vm.state.password, isEmpty);
    expect(vm.state.outcome, LocalProvisioningOutcome.unavailable);
    expect(vm.state.phase, WifiProvisioningPhase.result);
    vm.dispose();
  });
}
