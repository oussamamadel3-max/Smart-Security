import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/local_provisioning_client.dart';
import '../providers/wifi_provisioning_providers.dart';

enum WifiProvisioningPhase {
  instructions,
  form,
  submitting,
  result,
}

class WifiProvisioningState {
  const WifiProvisioningState({
    this.phase = WifiProvisioningPhase.instructions,
    this.ssid = '',
    this.password = '',
    this.outcome,
    this.errorMessage,
  });

  final WifiProvisioningPhase phase;
  final String ssid;
  final String password;
  final LocalProvisioningOutcome? outcome;
  final String? errorMessage;

  WifiProvisioningState copyWith({
    WifiProvisioningPhase? phase,
    String? ssid,
    String? password,
    LocalProvisioningOutcome? outcome,
    String? errorMessage,
    bool clearOutcome = false,
    bool clearError = false,
    bool clearPassword = false,
  }) {
    return WifiProvisioningState(
      phase: phase ?? this.phase,
      ssid: ssid ?? this.ssid,
      password: clearPassword ? '' : (password ?? this.password),
      outcome: clearOutcome ? null : (outcome ?? this.outcome),
      errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
    );
  }
}

class WifiProvisioningViewModel extends StateNotifier<WifiProvisioningState> {
  WifiProvisioningViewModel(this._client)
      : super(const WifiProvisioningState());

  final LocalProvisioningClient _client;

  void acknowledgePossession() {
    state = state.copyWith(
      phase: WifiProvisioningPhase.form,
      clearError: true,
      clearOutcome: true,
    );
  }

  void setSsid(String value) => state = state.copyWith(ssid: value);

  void setPassword(String value) => state = state.copyWith(password: value);

  Future<void> submit() async {
    final ssid = state.ssid.trim();
    if (ssid.isEmpty) {
      state = state.copyWith(errorMessage: 'معرف الشبكة مطلوب');
      return;
    }

    state = state.copyWith(
      phase: WifiProvisioningPhase.submitting,
      clearError: true,
      clearOutcome: true,
    );

    final credentials = LocalWifiCredentials(
      ssid: ssid,
      password: state.password,
    );
    // Drop password from UI state before awaiting network.
    state = state.copyWith(clearPassword: true);

    final outcome = await _client.submitWifi(credentials);
    _client.clearSensitive();

    state = state.copyWith(
      phase: WifiProvisioningPhase.result,
      outcome: outcome,
      clearPassword: true,
    );
  }

  void retry() {
    state = state.copyWith(
      phase: WifiProvisioningPhase.form,
      clearOutcome: true,
      clearError: true,
      clearPassword: true,
    );
  }

  @override
  void dispose() {
    _client.clearSensitive();
    super.dispose();
  }
}

final wifiProvisioningViewModelProvider = StateNotifierProvider.autoDispose
    .family<WifiProvisioningViewModel, WifiProvisioningState, String>(
  (ref, deviceId) {
    // deviceId gates navigation; client is local-only.
    ref.watch(localProvisioningClientProvider);
    return WifiProvisioningViewModel(
      ref.read(localProvisioningClientProvider),
    );
  },
);
