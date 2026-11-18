// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'Hisn';

  @override
  String get appTitle => 'Smart Security System';

  @override
  String get ok => 'OK';

  @override
  String get cancel => 'Cancel';

  @override
  String get save => 'Save';

  @override
  String get delete => 'Delete';

  @override
  String get edit => 'Edit';

  @override
  String get add => 'Add';

  @override
  String get close => 'Close';

  @override
  String get confirm => 'Confirm';

  @override
  String get back => 'Back';

  @override
  String get next => 'Next';

  @override
  String get done => 'Done';

  @override
  String get retry => 'Retry';

  @override
  String get refresh => 'Refresh';

  @override
  String get search => 'Search';

  @override
  String get clear => 'Clear';

  @override
  String get submit => 'Submit';

  @override
  String get continueText => 'Continue';

  @override
  String get skip => 'Skip';

  @override
  String get yes => 'Yes';

  @override
  String get no => 'No';

  @override
  String get loading => 'Loading...';

  @override
  String get pleaseWait => 'Please wait...';

  @override
  String get noData => 'No data available';

  @override
  String get noResults => 'No results found';

  @override
  String get error => 'Error';

  @override
  String get success => 'Success';

  @override
  String get warning => 'Warning';

  @override
  String get info => 'Information';

  @override
  String get generalError => 'Something went wrong. Please try again.';

  @override
  String get networkError =>
      'No internet connection. Please check your network.';

  @override
  String get serverError => 'Server error. Please try again later.';

  @override
  String get timeoutError => 'Request timed out. Please try again.';

  @override
  String get unauthorizedError => 'Session expired. Please sign in again.';

  @override
  String get forbiddenError => 'You do not have permission for this action.';

  @override
  String get login => 'Sign in';

  @override
  String get logout => 'Sign out';

  @override
  String get username => 'Username';

  @override
  String get password => 'Password';

  @override
  String get email => 'Email';

  @override
  String get fieldRequired => 'This field is required';

  @override
  String get home => 'Home';

  @override
  String get profile => 'Profile';

  @override
  String get settings => 'Settings';

  @override
  String get language => 'Language';

  @override
  String get arabic => 'Arabic';

  @override
  String get english => 'English';

  @override
  String get sessionExpiredTitle => 'Session expired';

  @override
  String get sessionExpiredMessage => 'Please sign in again to continue';

  @override
  String get logoutConfirm => 'Do you want to sign out?';

  @override
  String get bootstrapPlaceholder =>
      'App shell is ready — feature screens come in later tasks';

  @override
  String get welcomeBack => 'Welcome back';

  @override
  String get loginSubtitle => 'Sign in to access your security devices.';

  @override
  String get loginFailed =>
      'Sign-in failed. Check your credentials and try again.';

  @override
  String get ownerRoleRequired => 'This app is for OWNER accounts only.';

  @override
  String get signingIn => 'Signing in...';

  @override
  String get myDevices => 'My devices';

  @override
  String get myDevicesSubtitle =>
      'Review linked devices and their confirmed states.';

  @override
  String get pendingLinkNotice =>
      'Devices awaiting link acknowledgment show name and link status only — no controls or history.';

  @override
  String get noDevicesTitle => 'No linked devices';

  @override
  String get noDevicesSubtitle =>
      'When an admin links a device to your account it will appear here.';

  @override
  String get connectionOnline => 'Online';

  @override
  String get connectionOffline => 'Offline';

  @override
  String get armArmed => 'Armed';

  @override
  String get armDisarmed => 'Disarmed';

  @override
  String get armUnknown => 'Arm state unknown';

  @override
  String get healthHealthy => 'Hardware healthy';

  @override
  String get healthFault => 'Hardware fault';

  @override
  String get healthUnknown => 'Hardware health unknown';

  @override
  String get sirenOff => 'Siren off';

  @override
  String get sirenAlarm => 'Alarm siren';

  @override
  String get sirenTest => 'Siren test';

  @override
  String get sirenUnknown => 'Siren state unknown';

  @override
  String get lastSeen => 'Last seen';

  @override
  String get lastConfirmedState => 'Last confirmed state';

  @override
  String get awaitingOwnershipSync => 'Awaiting ownership version sync';

  @override
  String get linkPendingTitle => 'Link pending completion';

  @override
  String get ownershipActive => 'Ownership active';

  @override
  String get staleWhileOffline =>
      'Device is offline. Showing last known state, not live state.';

  @override
  String get commandHoldUntilSync =>
      'A command request may be accepted when online; the server holds publish until ownership sync.';

  @override
  String get cannotControlYet =>
      'Control is unavailable until ownership sync completes.';

  @override
  String get refreshDevices => 'Refresh devices';

  @override
  String get deviceDetails => 'Device details';

  @override
  String get viewDeviceHistory => 'View events and commands history';

  @override
  String get deviceHistoryTitle => 'Device history';

  @override
  String get alarmEventsTab => 'Events';

  @override
  String get commandsHistoryTab => 'Commands';

  @override
  String get searchHistoryHint => 'Search by id, type, or status';

  @override
  String get noAlarmEventsTitle => 'No alarm events';

  @override
  String get noAlarmEventsSubtitle =>
      'Alarm incidents for the current ownership period appear here.';

  @override
  String get noCommandsTitle => 'No commands';

  @override
  String get noCommandsSubtitle =>
      'Control commands for the current ownership period appear here.';

  @override
  String get alarmEventLabel => 'Alarm event';

  @override
  String get alarmEventDetailTitle => 'Event details';

  @override
  String get commandDetailTitle => 'Command details';

  @override
  String get receivedAtLabel => 'Received at';

  @override
  String get occurredAtLabel => 'Occurred at';

  @override
  String get occurrenceUntrusted =>
      'Occurrence time is unconfirmed — device clock was not trusted.';

  @override
  String get occurrenceUntrustedShort => 'Unconfirmed';

  @override
  String get bufferedLabel => 'Buffered';

  @override
  String get bufferedNotice =>
      'This event arrived late from the device buffer. No invented delay duration is shown.';

  @override
  String get wasBufferedLabel => 'wasBuffered';

  @override
  String get sensorLabel => 'Sensor';

  @override
  String get statusLabel => 'Status';

  @override
  String get deviceIdLabel => 'Device id';

  @override
  String get eventUidLabel => 'eventUid';

  @override
  String get alarmEventIdLabel => 'alarmEventId';

  @override
  String get mutedAtLabel => 'Muted at';

  @override
  String get notificationLinkLater =>
      'Alarm details can be opened from the notifications inbox via notificationId after server ownership checks. Live FCM still depends on Firebase.';

  @override
  String get sirenTestCommand => 'Siren test';

  @override
  String get timedOutUnknownNotice =>
      'TIMED_OUT does not prove execution failure until reconciliation.';

  @override
  String get commandTypeLabel => 'Command type';

  @override
  String get createdAtLabel => 'Created at';

  @override
  String get expiresAtLabel => 'Expires';

  @override
  String get rejectCodeLabel => 'Reject code';

  @override
  String get targetAlarmEventUidLabel => 'targetAlarmEventUid';

  @override
  String get actualStateChangedLabel => 'actualStateChanged';

  @override
  String get commandIdLabel => 'commandId';

  @override
  String get wifiSetupTitle => 'Local Wi-Fi setup';

  @override
  String get wifiCloudCommandsSeparated =>
      'This path talks to the device locally only. ARM/DISARM stay on the cloud API and are never sent here.';

  @override
  String get wifiPossessionSteps =>
      '1) Confirm you are the owner or authorized technician.\n2) Perform the physical action to open provisioning on the device.\n3) Join the device temporary network from phone settings.\n4) Return here to send Wi-Fi credentials locally.';

  @override
  String get wifiContinueToForm => 'Joined temporary network — continue';

  @override
  String get wifiFormNotice =>
      'The Wi-Fi password is sent to the device locally only and is never stored on the server or in the app.';

  @override
  String get wifiSsidLabel => 'Network name (SSID)';

  @override
  String get wifiPasswordLabel => 'Network password';

  @override
  String get wifiSubmitLocal => 'Send to device locally';

  @override
  String get wifiResultConnectedTitle => 'Device confirmed connection';

  @override
  String get wifiResultConnectedBody =>
      'The device itself confirmed it joined the new network.';

  @override
  String get wifiResultFailedTitle => 'Network join failed';

  @override
  String get wifiResultFailedBody =>
      'The device reported a join failure. You can retry.';

  @override
  String get wifiResultTimeoutTitle => 'Unconfirmed / timed out';

  @override
  String get wifiResultTimeoutBody =>
      'Timed out without device confirmation. Sending credentials alone is not success.';

  @override
  String get wifiResultUnavailableTitle =>
      'Local provisioning contract not available yet';

  @override
  String get wifiResultUnavailableBody =>
      'The soft-AP path awaits the ESP/D-04 contract. Connection success was not claimed.';

  @override
  String get navDashboard => 'Home';

  @override
  String get navDevices => 'Devices';

  @override
  String get navNotifications => 'Notifications';

  @override
  String get navProfile => 'Account';

  @override
  String dashboardGreeting(String name) {
    return 'Hello, $name';
  }

  @override
  String get dashboardGreetingGeneric => 'Welcome';

  @override
  String get dashboardSubtitle =>
      'A quick look at the security devices linked to your account.';

  @override
  String get metricTotalDevices => 'Total devices';

  @override
  String get metricOnlineDevices => 'Online devices';

  @override
  String get metricArmedDevices => 'Armed';

  @override
  String get metricPendingLinks => 'Pending links';

  @override
  String get viewAllDevices => 'View all';

  @override
  String get dashboardConfirmedStateNotice =>
      'Arm state shown is the last device-confirmed state via the server — not a UI guess.';

  @override
  String get accountSectionTitle => 'Account details';

  @override
  String get profileDisplayName => 'Name';

  @override
  String get profileRoleLabel => 'Role';

  @override
  String get ownerRoleLabel => 'Owner (OWNER)';

  @override
  String get profileEnvironment => 'App environment';

  @override
  String get profileFcmNote =>
      'Live FCM delivery requires Firebase setup per flavor — not active under the current NoOp messaging port.';

  @override
  String get notificationsOsPermissionNotice =>
      'OS permission goes through the FCM port. With NoOp, push is not live; the inbox still loads server notifications.';

  @override
  String get requestNotificationPermission => 'Request notification permission';

  @override
  String get notificationPermissionGranted =>
      'OS permission: granted (per current port).';

  @override
  String get notificationPermissionDeniedOrUnavailable =>
      'OS permission: denied or unavailable via the current port.';

  @override
  String get notificationsInboxTitle => 'Notification inbox';

  @override
  String get noNotificationsTitle => 'No notifications';

  @override
  String get noNotificationsSubtitle =>
      'Account notifications from the server appear here when available.';

  @override
  String get notificationOpenUnsupported =>
      'This notification cannot be opened as an alarm-event detail from the current payload.';

  @override
  String get deviceCommandsSection => 'Control commands';

  @override
  String get commandArm => 'Arm';

  @override
  String get commandDisarm => 'Disarm';

  @override
  String get commandMute => 'Mute siren';

  @override
  String get commandTestSiren => 'Test siren';

  @override
  String get commandStopTest => 'Stop test';

  @override
  String get deviceOfflineNoLiveControl =>
      'Device is offline — live control is not accepted.';

  @override
  String pendingCommandBanner(String type, String status) {
    return 'Command request $type: $status. Confirmed state below does not change on HTTP 202.';
  }
}
