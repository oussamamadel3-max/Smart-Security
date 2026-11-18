import 'dart:async';

import 'package:admin_web/core/session/session_epoch.dart';
import 'package:admin_web/features/devices/data/datasources/devices_remote_datasource.dart';
import 'package:admin_web/features/devices/data/mappers/owner_device_mapper.dart';
import 'package:admin_web/features/devices/data/models/owner_device_dto.dart';
import 'package:admin_web/features/devices/data/repositories/devices_repository_impl.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _FakeRemote extends DevicesRemoteDataSource {
  _FakeRemote() : super(Dio());

  List<OwnerDeviceDto> items = const [];
  Result<List<OwnerDeviceDto>>? listResult;
  Result<OwnerDeviceDto>? detailResult;
  Completer<void>? listGate;

  @override
  Future<Result<List<OwnerDeviceDto>>> listMine() async {
    final gate = listGate;
    if (gate != null) await gate.future;
    return listResult ?? Success(items);
  }

  @override
  Future<Result<OwnerDeviceDto>> getMine(String deviceId) async {
    return detailResult ?? const Failure_(Failure.forbidden(message: 'ممنوع'));
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
    expect(repo.hasCachedList, isTrue);

    repo.clearCache();
    expect(repo.hasCachedList, isFalse);

    remote.detailResult = const Failure_(Failure.forbidden());
    final detail = await repo.getMine('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    expect(detail.isFailure, isTrue);
  });

  test(
    'late list success after session epoch invalidate does not refill cache',
    () async {
      final epoch = SessionEpoch();
      final remote = _FakeRemote()
        ..items = [
          const OwnerDeviceDto(
            deviceId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
            displayName: 'سابق',
            ownershipStatus: 'ACTIVE',
            ownershipVersion: 1,
            confirmedOwnershipVersion: 1,
            awaitingOwnershipAck: false,
            canControl: true,
            linkStatusMessage: 'الملكية فعالة',
          ),
        ]
        ..listGate = Completer<void>();
      final repo = DevicesRepositoryImpl(remote, epoch: epoch);

      final pending = repo.listMine();
      epoch.invalidate();
      remote.listGate!.complete();
      final listed = await pending;

      expect(listed.isSuccess, isTrue);
      expect(repo.hasCachedList, isFalse);
    },
  );

  test('unknown ownership wire value is not ACTIVE success', () {
    final entity = OwnerDeviceMapper.toEntity(
      const OwnerDeviceDto(
        deviceId: 'x',
        displayName: 'X',
        ownershipStatus: 'WEIRD',
        ownershipVersion: 0,
        confirmedOwnershipVersion: 0,
        awaitingOwnershipAck: false,
        canControl: false,
        linkStatusMessage: '',
      ),
    );
    expect(entity.ownershipStatus, OwnershipStatus.unknown);
    expect(entity.showsOperationalIndicators, isFalse);
  });
}
