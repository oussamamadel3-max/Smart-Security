// =============================================================================
// errors.dart — Barrel Export
// =============================================================================
// استيراد واحد يُغطي نظام الأخطاء بالكامل.
//
// في أي Repository أو Notifier:
//   import 'package:your_app/core/errors/errors.dart';
// =============================================================================

// Result
export 'result/result.dart';

// Failures
export 'failures/failure.dart';

// Exceptions
export 'exceptions/api_exception.dart';

// Models
export 'models/api_error_model.dart';

// Codes
export 'codes/api_error_codes.dart';

// Mappers
export 'mappers/error_mapper.dart';

// Error state
export 'error_state.dart';

// Handlers
export 'handlers/failure_handler.dart';
