/// Build-time environment via `--dart-define=APP_ENV=dev|staging|prod`.
enum AppEnvironment {
  dev,
  staging,
  prod;

  static AppEnvironment get current {
    const raw = String.fromEnvironment('APP_ENV', defaultValue: 'dev');
    return switch (raw.toLowerCase()) {
      'staging' || 'stage' => AppEnvironment.staging,
      'prod' || 'production' => AppEnvironment.prod,
      _ => AppEnvironment.dev,
    };
  }

  String get apiBaseUrl => switch (this) {
        AppEnvironment.dev =>
          const String.fromEnvironment(
            'API_BASE_URL',
            defaultValue: 'http://localhost:5080',
          ),
        AppEnvironment.staging =>
          const String.fromEnvironment(
            'API_BASE_URL',
            defaultValue: 'https://staging-api.example.invalid',
          ),
        AppEnvironment.prod =>
          const String.fromEnvironment(
            'API_BASE_URL',
            defaultValue: 'https://api.example.invalid',
          ),
      };

  String get label => name;
}
