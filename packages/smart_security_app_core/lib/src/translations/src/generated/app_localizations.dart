import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_ar.dart';
import 'app_localizations_en.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'generated/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('ar'),
    Locale('en'),
  ];

  /// Product brand name
  ///
  /// In en, this message translates to:
  /// **'Hisn'**
  String get appName;

  /// No description provided for @appTitle.
  ///
  /// In en, this message translates to:
  /// **'Smart Security System'**
  String get appTitle;

  /// No description provided for @ok.
  ///
  /// In en, this message translates to:
  /// **'OK'**
  String get ok;

  /// No description provided for @cancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get cancel;

  /// No description provided for @save.
  ///
  /// In en, this message translates to:
  /// **'Save'**
  String get save;

  /// No description provided for @delete.
  ///
  /// In en, this message translates to:
  /// **'Delete'**
  String get delete;

  /// No description provided for @edit.
  ///
  /// In en, this message translates to:
  /// **'Edit'**
  String get edit;

  /// No description provided for @add.
  ///
  /// In en, this message translates to:
  /// **'Add'**
  String get add;

  /// No description provided for @close.
  ///
  /// In en, this message translates to:
  /// **'Close'**
  String get close;

  /// No description provided for @confirm.
  ///
  /// In en, this message translates to:
  /// **'Confirm'**
  String get confirm;

  /// No description provided for @back.
  ///
  /// In en, this message translates to:
  /// **'Back'**
  String get back;

  /// No description provided for @next.
  ///
  /// In en, this message translates to:
  /// **'Next'**
  String get next;

  /// No description provided for @done.
  ///
  /// In en, this message translates to:
  /// **'Done'**
  String get done;

  /// No description provided for @retry.
  ///
  /// In en, this message translates to:
  /// **'Retry'**
  String get retry;

  /// No description provided for @refresh.
  ///
  /// In en, this message translates to:
  /// **'Refresh'**
  String get refresh;

  /// No description provided for @search.
  ///
  /// In en, this message translates to:
  /// **'Search'**
  String get search;

  /// No description provided for @clear.
  ///
  /// In en, this message translates to:
  /// **'Clear'**
  String get clear;

  /// No description provided for @submit.
  ///
  /// In en, this message translates to:
  /// **'Submit'**
  String get submit;

  /// No description provided for @continueText.
  ///
  /// In en, this message translates to:
  /// **'Continue'**
  String get continueText;

  /// No description provided for @skip.
  ///
  /// In en, this message translates to:
  /// **'Skip'**
  String get skip;

  /// No description provided for @yes.
  ///
  /// In en, this message translates to:
  /// **'Yes'**
  String get yes;

  /// No description provided for @no.
  ///
  /// In en, this message translates to:
  /// **'No'**
  String get no;

  /// No description provided for @loading.
  ///
  /// In en, this message translates to:
  /// **'Loading...'**
  String get loading;

  /// No description provided for @pleaseWait.
  ///
  /// In en, this message translates to:
  /// **'Please wait...'**
  String get pleaseWait;

  /// No description provided for @noData.
  ///
  /// In en, this message translates to:
  /// **'No data available'**
  String get noData;

  /// No description provided for @noResults.
  ///
  /// In en, this message translates to:
  /// **'No results found'**
  String get noResults;

  /// No description provided for @error.
  ///
  /// In en, this message translates to:
  /// **'Error'**
  String get error;

  /// No description provided for @success.
  ///
  /// In en, this message translates to:
  /// **'Success'**
  String get success;

  /// No description provided for @warning.
  ///
  /// In en, this message translates to:
  /// **'Warning'**
  String get warning;

  /// No description provided for @info.
  ///
  /// In en, this message translates to:
  /// **'Information'**
  String get info;

  /// No description provided for @generalError.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong. Please try again.'**
  String get generalError;

  /// No description provided for @networkError.
  ///
  /// In en, this message translates to:
  /// **'No internet connection. Please check your network.'**
  String get networkError;

  /// No description provided for @serverError.
  ///
  /// In en, this message translates to:
  /// **'Server error. Please try again later.'**
  String get serverError;

  /// No description provided for @timeoutError.
  ///
  /// In en, this message translates to:
  /// **'Request timed out. Please try again.'**
  String get timeoutError;

  /// No description provided for @unauthorizedError.
  ///
  /// In en, this message translates to:
  /// **'Session expired. Please sign in again.'**
  String get unauthorizedError;

  /// No description provided for @forbiddenError.
  ///
  /// In en, this message translates to:
  /// **'You do not have permission for this action.'**
  String get forbiddenError;

  /// No description provided for @login.
  ///
  /// In en, this message translates to:
  /// **'Sign in'**
  String get login;

  /// No description provided for @logout.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get logout;

  /// No description provided for @username.
  ///
  /// In en, this message translates to:
  /// **'Username'**
  String get username;

  /// No description provided for @password.
  ///
  /// In en, this message translates to:
  /// **'Password'**
  String get password;

  /// No description provided for @email.
  ///
  /// In en, this message translates to:
  /// **'Email'**
  String get email;

  /// No description provided for @fieldRequired.
  ///
  /// In en, this message translates to:
  /// **'This field is required'**
  String get fieldRequired;

  /// No description provided for @home.
  ///
  /// In en, this message translates to:
  /// **'Home'**
  String get home;

  /// No description provided for @profile.
  ///
  /// In en, this message translates to:
  /// **'Profile'**
  String get profile;

  /// No description provided for @settings.
  ///
  /// In en, this message translates to:
  /// **'Settings'**
  String get settings;

  /// No description provided for @language.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get language;

  /// No description provided for @arabic.
  ///
  /// In en, this message translates to:
  /// **'Arabic'**
  String get arabic;

  /// No description provided for @english.
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get english;

  /// No description provided for @sessionExpiredTitle.
  ///
  /// In en, this message translates to:
  /// **'Session expired'**
  String get sessionExpiredTitle;

  /// No description provided for @sessionExpiredMessage.
  ///
  /// In en, this message translates to:
  /// **'Please sign in again to continue'**
  String get sessionExpiredMessage;

  /// No description provided for @logoutConfirm.
  ///
  /// In en, this message translates to:
  /// **'Do you want to sign out?'**
  String get logoutConfirm;

  /// No description provided for @bootstrapPlaceholder.
  ///
  /// In en, this message translates to:
  /// **'App shell is ready — feature screens come in later tasks'**
  String get bootstrapPlaceholder;

  /// No description provided for @welcomeBack.
  ///
  /// In en, this message translates to:
  /// **'Welcome back'**
  String get welcomeBack;

  /// No description provided for @loginSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Sign in to access your security devices.'**
  String get loginSubtitle;

  /// No description provided for @loginFailed.
  ///
  /// In en, this message translates to:
  /// **'Sign-in failed. Check your credentials and try again.'**
  String get loginFailed;

  /// No description provided for @ownerRoleRequired.
  ///
  /// In en, this message translates to:
  /// **'This app is for OWNER accounts only.'**
  String get ownerRoleRequired;

  /// No description provided for @signingIn.
  ///
  /// In en, this message translates to:
  /// **'Signing in...'**
  String get signingIn;

  /// No description provided for @myDevices.
  ///
  /// In en, this message translates to:
  /// **'My devices'**
  String get myDevices;

  /// No description provided for @myDevicesSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Review linked devices and their confirmed states.'**
  String get myDevicesSubtitle;

  /// No description provided for @pendingLinkNotice.
  ///
  /// In en, this message translates to:
  /// **'Devices awaiting link acknowledgment show name and link status only — no controls or history.'**
  String get pendingLinkNotice;

  /// No description provided for @noDevicesTitle.
  ///
  /// In en, this message translates to:
  /// **'No linked devices'**
  String get noDevicesTitle;

  /// No description provided for @noDevicesSubtitle.
  ///
  /// In en, this message translates to:
  /// **'When an admin links a device to your account it will appear here.'**
  String get noDevicesSubtitle;

  /// No description provided for @connectionOnline.
  ///
  /// In en, this message translates to:
  /// **'Online'**
  String get connectionOnline;

  /// No description provided for @connectionOffline.
  ///
  /// In en, this message translates to:
  /// **'Offline'**
  String get connectionOffline;

  /// No description provided for @armArmed.
  ///
  /// In en, this message translates to:
  /// **'Armed'**
  String get armArmed;

  /// No description provided for @armDisarmed.
  ///
  /// In en, this message translates to:
  /// **'Disarmed'**
  String get armDisarmed;

  /// No description provided for @armUnknown.
  ///
  /// In en, this message translates to:
  /// **'Arm state unknown'**
  String get armUnknown;

  /// No description provided for @healthHealthy.
  ///
  /// In en, this message translates to:
  /// **'Hardware healthy'**
  String get healthHealthy;

  /// No description provided for @healthFault.
  ///
  /// In en, this message translates to:
  /// **'Hardware fault'**
  String get healthFault;

  /// No description provided for @healthUnknown.
  ///
  /// In en, this message translates to:
  /// **'Hardware health unknown'**
  String get healthUnknown;

  /// No description provided for @sirenOff.
  ///
  /// In en, this message translates to:
  /// **'Siren off'**
  String get sirenOff;

  /// No description provided for @sirenAlarm.
  ///
  /// In en, this message translates to:
  /// **'Alarm siren'**
  String get sirenAlarm;

  /// No description provided for @sirenTest.
  ///
  /// In en, this message translates to:
  /// **'Siren test'**
  String get sirenTest;

  /// No description provided for @sirenUnknown.
  ///
  /// In en, this message translates to:
  /// **'Siren state unknown'**
  String get sirenUnknown;

  /// No description provided for @lastSeen.
  ///
  /// In en, this message translates to:
  /// **'Last seen'**
  String get lastSeen;

  /// No description provided for @lastConfirmedState.
  ///
  /// In en, this message translates to:
  /// **'Last confirmed state'**
  String get lastConfirmedState;

  /// No description provided for @awaitingOwnershipSync.
  ///
  /// In en, this message translates to:
  /// **'Awaiting ownership version sync'**
  String get awaitingOwnershipSync;

  /// No description provided for @linkPendingTitle.
  ///
  /// In en, this message translates to:
  /// **'Link pending completion'**
  String get linkPendingTitle;

  /// No description provided for @ownershipActive.
  ///
  /// In en, this message translates to:
  /// **'Ownership active'**
  String get ownershipActive;

  /// No description provided for @staleWhileOffline.
  ///
  /// In en, this message translates to:
  /// **'Device is offline. Showing last known state, not live state.'**
  String get staleWhileOffline;

  /// No description provided for @commandHoldUntilSync.
  ///
  /// In en, this message translates to:
  /// **'A command request may be accepted when online; the server holds publish until ownership sync.'**
  String get commandHoldUntilSync;

  /// No description provided for @cannotControlYet.
  ///
  /// In en, this message translates to:
  /// **'Control is unavailable until ownership sync completes.'**
  String get cannotControlYet;

  /// No description provided for @refreshDevices.
  ///
  /// In en, this message translates to:
  /// **'Refresh devices'**
  String get refreshDevices;

  /// No description provided for @deviceDetails.
  ///
  /// In en, this message translates to:
  /// **'Device details'**
  String get deviceDetails;

  /// No description provided for @viewDeviceHistory.
  ///
  /// In en, this message translates to:
  /// **'View events and commands history'**
  String get viewDeviceHistory;

  /// No description provided for @deviceHistoryTitle.
  ///
  /// In en, this message translates to:
  /// **'Device history'**
  String get deviceHistoryTitle;

  /// No description provided for @alarmEventsTab.
  ///
  /// In en, this message translates to:
  /// **'Events'**
  String get alarmEventsTab;

  /// No description provided for @commandsHistoryTab.
  ///
  /// In en, this message translates to:
  /// **'Commands'**
  String get commandsHistoryTab;

  /// No description provided for @searchHistoryHint.
  ///
  /// In en, this message translates to:
  /// **'Search by id, type, or status'**
  String get searchHistoryHint;

  /// No description provided for @noAlarmEventsTitle.
  ///
  /// In en, this message translates to:
  /// **'No alarm events'**
  String get noAlarmEventsTitle;

  /// No description provided for @noAlarmEventsSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Alarm incidents for the current ownership period appear here.'**
  String get noAlarmEventsSubtitle;

  /// No description provided for @noCommandsTitle.
  ///
  /// In en, this message translates to:
  /// **'No commands'**
  String get noCommandsTitle;

  /// No description provided for @noCommandsSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Control commands for the current ownership period appear here.'**
  String get noCommandsSubtitle;

  /// No description provided for @alarmEventLabel.
  ///
  /// In en, this message translates to:
  /// **'Alarm event'**
  String get alarmEventLabel;

  /// No description provided for @alarmEventDetailTitle.
  ///
  /// In en, this message translates to:
  /// **'Event details'**
  String get alarmEventDetailTitle;

  /// No description provided for @commandDetailTitle.
  ///
  /// In en, this message translates to:
  /// **'Command details'**
  String get commandDetailTitle;

  /// No description provided for @receivedAtLabel.
  ///
  /// In en, this message translates to:
  /// **'Received at'**
  String get receivedAtLabel;

  /// No description provided for @occurredAtLabel.
  ///
  /// In en, this message translates to:
  /// **'Occurred at'**
  String get occurredAtLabel;

  /// No description provided for @occurrenceUntrusted.
  ///
  /// In en, this message translates to:
  /// **'Occurrence time is unconfirmed — device clock was not trusted.'**
  String get occurrenceUntrusted;

  /// No description provided for @occurrenceUntrustedShort.
  ///
  /// In en, this message translates to:
  /// **'Unconfirmed'**
  String get occurrenceUntrustedShort;

  /// No description provided for @bufferedLabel.
  ///
  /// In en, this message translates to:
  /// **'Buffered'**
  String get bufferedLabel;

  /// No description provided for @bufferedNotice.
  ///
  /// In en, this message translates to:
  /// **'This event arrived late from the device buffer. No invented delay duration is shown.'**
  String get bufferedNotice;

  /// No description provided for @wasBufferedLabel.
  ///
  /// In en, this message translates to:
  /// **'wasBuffered'**
  String get wasBufferedLabel;

  /// No description provided for @sensorLabel.
  ///
  /// In en, this message translates to:
  /// **'Sensor'**
  String get sensorLabel;

  /// No description provided for @statusLabel.
  ///
  /// In en, this message translates to:
  /// **'Status'**
  String get statusLabel;

  /// No description provided for @deviceIdLabel.
  ///
  /// In en, this message translates to:
  /// **'Device id'**
  String get deviceIdLabel;

  /// No description provided for @eventUidLabel.
  ///
  /// In en, this message translates to:
  /// **'eventUid'**
  String get eventUidLabel;

  /// No description provided for @alarmEventIdLabel.
  ///
  /// In en, this message translates to:
  /// **'alarmEventId'**
  String get alarmEventIdLabel;

  /// No description provided for @mutedAtLabel.
  ///
  /// In en, this message translates to:
  /// **'Muted at'**
  String get mutedAtLabel;

  /// No description provided for @notificationLinkLater.
  ///
  /// In en, this message translates to:
  /// **'Alarm details can be opened from the notifications inbox via notificationId after server ownership checks. Live FCM still depends on Firebase.'**
  String get notificationLinkLater;

  /// No description provided for @sirenTestCommand.
  ///
  /// In en, this message translates to:
  /// **'Siren test'**
  String get sirenTestCommand;

  /// No description provided for @timedOutUnknownNotice.
  ///
  /// In en, this message translates to:
  /// **'TIMED_OUT does not prove execution failure until reconciliation.'**
  String get timedOutUnknownNotice;

  /// No description provided for @commandTypeLabel.
  ///
  /// In en, this message translates to:
  /// **'Command type'**
  String get commandTypeLabel;

  /// No description provided for @createdAtLabel.
  ///
  /// In en, this message translates to:
  /// **'Created at'**
  String get createdAtLabel;

  /// No description provided for @expiresAtLabel.
  ///
  /// In en, this message translates to:
  /// **'Expires'**
  String get expiresAtLabel;

  /// No description provided for @rejectCodeLabel.
  ///
  /// In en, this message translates to:
  /// **'Reject code'**
  String get rejectCodeLabel;

  /// No description provided for @targetAlarmEventUidLabel.
  ///
  /// In en, this message translates to:
  /// **'targetAlarmEventUid'**
  String get targetAlarmEventUidLabel;

  /// No description provided for @actualStateChangedLabel.
  ///
  /// In en, this message translates to:
  /// **'actualStateChanged'**
  String get actualStateChangedLabel;

  /// No description provided for @commandIdLabel.
  ///
  /// In en, this message translates to:
  /// **'commandId'**
  String get commandIdLabel;

  /// No description provided for @wifiSetupTitle.
  ///
  /// In en, this message translates to:
  /// **'Local Wi-Fi setup'**
  String get wifiSetupTitle;

  /// No description provided for @wifiCloudCommandsSeparated.
  ///
  /// In en, this message translates to:
  /// **'This path talks to the device locally only. ARM/DISARM stay on the cloud API and are never sent here.'**
  String get wifiCloudCommandsSeparated;

  /// No description provided for @wifiPossessionSteps.
  ///
  /// In en, this message translates to:
  /// **'1) Confirm you are the owner or authorized technician.\n2) Perform the physical action to open provisioning on the device.\n3) Join the device temporary network from phone settings.\n4) Return here to send Wi-Fi credentials locally.'**
  String get wifiPossessionSteps;

  /// No description provided for @wifiContinueToForm.
  ///
  /// In en, this message translates to:
  /// **'Joined temporary network — continue'**
  String get wifiContinueToForm;

  /// No description provided for @wifiFormNotice.
  ///
  /// In en, this message translates to:
  /// **'The Wi-Fi password is sent to the device locally only and is never stored on the server or in the app.'**
  String get wifiFormNotice;

  /// No description provided for @wifiSsidLabel.
  ///
  /// In en, this message translates to:
  /// **'Network name (SSID)'**
  String get wifiSsidLabel;

  /// No description provided for @wifiPasswordLabel.
  ///
  /// In en, this message translates to:
  /// **'Network password'**
  String get wifiPasswordLabel;

  /// No description provided for @wifiSubmitLocal.
  ///
  /// In en, this message translates to:
  /// **'Send to device locally'**
  String get wifiSubmitLocal;

  /// No description provided for @wifiResultConnectedTitle.
  ///
  /// In en, this message translates to:
  /// **'Device confirmed connection'**
  String get wifiResultConnectedTitle;

  /// No description provided for @wifiResultConnectedBody.
  ///
  /// In en, this message translates to:
  /// **'The device itself confirmed it joined the new network.'**
  String get wifiResultConnectedBody;

  /// No description provided for @wifiResultFailedTitle.
  ///
  /// In en, this message translates to:
  /// **'Network join failed'**
  String get wifiResultFailedTitle;

  /// No description provided for @wifiResultFailedBody.
  ///
  /// In en, this message translates to:
  /// **'The device reported a join failure. You can retry.'**
  String get wifiResultFailedBody;

  /// No description provided for @wifiResultTimeoutTitle.
  ///
  /// In en, this message translates to:
  /// **'Unconfirmed / timed out'**
  String get wifiResultTimeoutTitle;

  /// No description provided for @wifiResultTimeoutBody.
  ///
  /// In en, this message translates to:
  /// **'Timed out without device confirmation. Sending credentials alone is not success.'**
  String get wifiResultTimeoutBody;

  /// No description provided for @wifiResultUnavailableTitle.
  ///
  /// In en, this message translates to:
  /// **'Local provisioning contract not available yet'**
  String get wifiResultUnavailableTitle;

  /// No description provided for @wifiResultUnavailableBody.
  ///
  /// In en, this message translates to:
  /// **'The soft-AP path awaits the ESP/D-04 contract. Connection success was not claimed.'**
  String get wifiResultUnavailableBody;

  /// No description provided for @navDashboard.
  ///
  /// In en, this message translates to:
  /// **'Home'**
  String get navDashboard;

  /// No description provided for @navDevices.
  ///
  /// In en, this message translates to:
  /// **'Devices'**
  String get navDevices;

  /// No description provided for @navNotifications.
  ///
  /// In en, this message translates to:
  /// **'Notifications'**
  String get navNotifications;

  /// No description provided for @navProfile.
  ///
  /// In en, this message translates to:
  /// **'Account'**
  String get navProfile;

  /// No description provided for @dashboardGreeting.
  ///
  /// In en, this message translates to:
  /// **'Hello, {name}'**
  String dashboardGreeting(String name);

  /// No description provided for @dashboardGreetingGeneric.
  ///
  /// In en, this message translates to:
  /// **'Welcome'**
  String get dashboardGreetingGeneric;

  /// No description provided for @dashboardSubtitle.
  ///
  /// In en, this message translates to:
  /// **'A quick look at the security devices linked to your account.'**
  String get dashboardSubtitle;

  /// No description provided for @metricTotalDevices.
  ///
  /// In en, this message translates to:
  /// **'Total devices'**
  String get metricTotalDevices;

  /// No description provided for @metricOnlineDevices.
  ///
  /// In en, this message translates to:
  /// **'Online devices'**
  String get metricOnlineDevices;

  /// No description provided for @metricArmedDevices.
  ///
  /// In en, this message translates to:
  /// **'Armed'**
  String get metricArmedDevices;

  /// No description provided for @metricPendingLinks.
  ///
  /// In en, this message translates to:
  /// **'Pending links'**
  String get metricPendingLinks;

  /// No description provided for @viewAllDevices.
  ///
  /// In en, this message translates to:
  /// **'View all'**
  String get viewAllDevices;

  /// No description provided for @dashboardConfirmedStateNotice.
  ///
  /// In en, this message translates to:
  /// **'Arm state shown is the last device-confirmed state via the server — not a UI guess.'**
  String get dashboardConfirmedStateNotice;

  /// No description provided for @accountSectionTitle.
  ///
  /// In en, this message translates to:
  /// **'Account details'**
  String get accountSectionTitle;

  /// No description provided for @profileDisplayName.
  ///
  /// In en, this message translates to:
  /// **'Name'**
  String get profileDisplayName;

  /// No description provided for @profileRoleLabel.
  ///
  /// In en, this message translates to:
  /// **'Role'**
  String get profileRoleLabel;

  /// No description provided for @ownerRoleLabel.
  ///
  /// In en, this message translates to:
  /// **'Owner (OWNER)'**
  String get ownerRoleLabel;

  /// No description provided for @profileEnvironment.
  ///
  /// In en, this message translates to:
  /// **'App environment'**
  String get profileEnvironment;

  /// No description provided for @profileFcmNote.
  ///
  /// In en, this message translates to:
  /// **'Live FCM delivery requires Firebase setup per flavor — not active under the current NoOp messaging port.'**
  String get profileFcmNote;

  /// No description provided for @notificationsOsPermissionNotice.
  ///
  /// In en, this message translates to:
  /// **'OS permission goes through the FCM port. With NoOp, push is not live; the inbox still loads server notifications.'**
  String get notificationsOsPermissionNotice;

  /// No description provided for @requestNotificationPermission.
  ///
  /// In en, this message translates to:
  /// **'Request notification permission'**
  String get requestNotificationPermission;

  /// No description provided for @notificationPermissionGranted.
  ///
  /// In en, this message translates to:
  /// **'OS permission: granted (per current port).'**
  String get notificationPermissionGranted;

  /// No description provided for @notificationPermissionDeniedOrUnavailable.
  ///
  /// In en, this message translates to:
  /// **'OS permission: denied or unavailable via the current port.'**
  String get notificationPermissionDeniedOrUnavailable;

  /// No description provided for @notificationsInboxTitle.
  ///
  /// In en, this message translates to:
  /// **'Notification inbox'**
  String get notificationsInboxTitle;

  /// No description provided for @noNotificationsTitle.
  ///
  /// In en, this message translates to:
  /// **'No notifications'**
  String get noNotificationsTitle;

  /// No description provided for @noNotificationsSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Account notifications from the server appear here when available.'**
  String get noNotificationsSubtitle;

  /// No description provided for @notificationOpenUnsupported.
  ///
  /// In en, this message translates to:
  /// **'This notification cannot be opened as an alarm-event detail from the current payload.'**
  String get notificationOpenUnsupported;

  /// No description provided for @deviceCommandsSection.
  ///
  /// In en, this message translates to:
  /// **'Control commands'**
  String get deviceCommandsSection;

  /// No description provided for @commandArm.
  ///
  /// In en, this message translates to:
  /// **'Arm'**
  String get commandArm;

  /// No description provided for @commandDisarm.
  ///
  /// In en, this message translates to:
  /// **'Disarm'**
  String get commandDisarm;

  /// No description provided for @commandMute.
  ///
  /// In en, this message translates to:
  /// **'Mute siren'**
  String get commandMute;

  /// No description provided for @commandTestSiren.
  ///
  /// In en, this message translates to:
  /// **'Test siren'**
  String get commandTestSiren;

  /// No description provided for @commandStopTest.
  ///
  /// In en, this message translates to:
  /// **'Stop test'**
  String get commandStopTest;

  /// No description provided for @deviceOfflineNoLiveControl.
  ///
  /// In en, this message translates to:
  /// **'Device is offline — live control is not accepted.'**
  String get deviceOfflineNoLiveControl;

  /// No description provided for @pendingCommandBanner.
  ///
  /// In en, this message translates to:
  /// **'Command request {type}: {status}. Confirmed state below does not change on HTTP 202.'**
  String pendingCommandBanner(String type, String status);
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['ar', 'en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'ar':
      return AppLocalizationsAr();
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
