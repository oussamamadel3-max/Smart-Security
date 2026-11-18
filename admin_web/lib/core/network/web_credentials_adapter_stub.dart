import 'package:dio/dio.dart';

/// Non-web: credentials adapter is unused (tests inject their own adapter).
HttpClientAdapter? createWebCredentialsAdapter() => null;
