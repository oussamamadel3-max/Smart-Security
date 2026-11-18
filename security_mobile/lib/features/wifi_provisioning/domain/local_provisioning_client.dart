/// Outcome of a local soft-AP Wi-Fi attempt — never inferred from HTTP alone
/// unless the device body explicitly confirms connection.
enum LocalProvisioningOutcome {
  connected,
  failed,
  timedOut,
  unavailable,
}

class LocalWifiCredentials {
  const LocalWifiCredentials({
    required this.ssid,
    required this.password,
  });

  final String ssid;

  /// Kept in memory for the request only — never persisted or sent to Backend.
  final String password;
}

/// Talks to the device soft-AP only. Must not share Dio/Auth with cloud APIs.
abstract class LocalProvisioningClient {
  Future<LocalProvisioningOutcome> submitWifi(LocalWifiCredentials credentials);

  /// Clears any in-memory credentials retained by the client.
  void clearSensitive();
}

/// Default until ESP-T-08 / D-04 publish the local HTTP contract.
///
/// Does not claim success. Callers must treat [unavailable] as integration-pending.
class UnavailableLocalProvisioningClient implements LocalProvisioningClient {
  LocalWifiCredentials? _held;

  @override
  Future<LocalProvisioningOutcome> submitWifi(
    LocalWifiCredentials credentials,
  ) async {
    _held = credentials;
    // Soft-AP contract is not adopted — never report connected.
    clearSensitive();
    return LocalProvisioningOutcome.unavailable;
  }

  @override
  void clearSensitive() {
    _held = null;
  }

  /// Test helper — ensures password is not retained after submit.
  bool get retainsPasswordForTest => _held?.password.isNotEmpty == true;
}
