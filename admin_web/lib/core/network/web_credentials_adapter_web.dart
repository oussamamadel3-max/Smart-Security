import 'package:dio/browser.dart';
import 'package:dio/dio.dart';

/// Browser XHR/fetch with cookies (HttpOnly refresh + CSRF cookie).
HttpClientAdapter? createWebCredentialsAdapter() {
  final adapter = BrowserHttpClientAdapter()..withCredentials = true;
  return adapter;
}
