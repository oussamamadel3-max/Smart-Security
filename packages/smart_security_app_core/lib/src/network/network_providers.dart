import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_core_config.dart';
import 'dio_client.dart';

/// Base Dio without auth refresh. Apps may override or wrap for T-02.
final dioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appCoreConfigProvider);
  return DioClientSetup.build(config: config);
});
