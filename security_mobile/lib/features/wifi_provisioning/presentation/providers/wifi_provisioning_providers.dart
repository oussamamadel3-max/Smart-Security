import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/local_provisioning_client.dart';

final localProvisioningClientProvider =
    Provider<LocalProvisioningClient>((ref) {
  return UnavailableLocalProvisioningClient();
});
