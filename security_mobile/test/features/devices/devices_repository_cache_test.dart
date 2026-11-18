import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/features/devices/data/repositories/devices_repository_impl.dart';
import 'package:security_mobile/features/devices/data/datasources/devices_remote_datasource.dart';
import 'package:security_mobile/features/devices/data/models/owner_device_dto.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';
import 'package:dio/dio.dart';

class _FakeRemote extends DevicesRemoteDataSource {
  _FakeRemote() : super(Dio());

  List<OwnerDeviceDto> items = const [];
  Result<List<OwnerDeviceDto>>? listResult;
  Result<OwnerDeviceDto>? detailResult;

  @override
  Future<Result<List<OwnerDeviceDto>>> listMine() async {
    return listResult ?? Success(items);
  }

  @override
  Future<Result<OwnerDeviceDto>> getMine(String deviceId) async {
    return detailResult ??
        const Failure_(Failure.forbidden(message: 'ممنوع'));
  }
}

void main() {
  test('clearCache drops retained owner rows after auth loss path', () async {
    final remote = _FakeRemote()
      ..items = [
        const OwnerDeviceDto(
          deviceId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          displayName: 'A',
          ownershipStatus: 'ACTIVE',
          ownershipVersion: 1,
          confirmedOwnershipVersion: 1,
          awaitingOwnershipAck: false,
          canControl: true,
          linkStatusMessage: 'الملكية فعالة',
        ),
      ];
    final repo = DevicesRepositoryImpl(remote);

    final listed = await repo.listMine();
    expect(listed.isSuccess, isTrue);

    repo.clearCache();

    remote.detailResult = const Failure_(Failure.forbidden());
    final detail = await repo.getMine('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    expect(detail.isFailure, isTrue);
  });
}
