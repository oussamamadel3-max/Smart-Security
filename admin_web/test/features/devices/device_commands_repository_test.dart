import 'package:admin_web/features/devices/data/datasources/device_commands_remote_datasource.dart';
import 'package:admin_web/features/devices/data/models/device_command_dto.dart';
import 'package:admin_web/features/devices/data/repositories/device_commands_repository_impl.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _FailingCreateSource extends DeviceCommandsRemoteDataSource {
  _FailingCreateSource() : super(Dio());

  int creates = 0;

  @override
  Future<Result<DeviceCommandDto>> create({
    required String deviceId,
    required CreateDeviceCommandRequestDto request,
  }) async {
    creates += 1;
    return const Result.failure(Failure.network());
  }
}

void main() {
  test('POST create is not auto-retried', () async {
    final remote = _FailingCreateSource();
    final repo = DeviceCommandsRepositoryImpl(remote);

    final result = await repo.create(
      deviceId: 'd1',
      type: CommandType.arm,
      idempotencyKey: '11111111-1111-4111-8111-111111111111',
    );

    expect(result.isFailure, isTrue);
    expect(remote.creates, 1);
  });
}
