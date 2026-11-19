# بناء `security_esp/firmware` — دليل بيئة التطوير والبناء

آخر تحديث: 2026-11-19 (طبقة connectivity + fullclean/build 1018/1018)

## حالة هذه الوثيقة

**ESP-IDF مثبَّت ومؤكَّد فعلياً، والمشروع كامله (`core`+`wire_json`+`port_esp32`+`main`) يُبنى بنجاح بصفر تحذيرات عبر بناء نظيف كامل من الصفر.** لا عتاد حقيقي مُختبَر، ولا `idf.py flash` نُفِّذ في أي جولة. **لا تثق بأي رقم إصدار هنا دون مطابقته مع §"الحالة الفعلية المتحقَّقة"** — هذا القسم يُحدَّث بصدق عند كل تغيير، لا يُفترض نجاحه.

## بنية المصدر

```
firmware/
  CMakeLists.txt                  # (ESP-C-02) جذر مشروع ESP-IDF
  sdkconfig.defaults               # (ESP-C-02) إعدادات تطوير فقط — لا Secure Boot/Flash Encryption
  main/
    CMakeLists.txt
    app_main.c                     # (ESP-C-02) إثبات ربط core تحت ESP-IDF فقط — لا منطق حقيقي، لا GPIO
  components/
    core/                          # منطق مستقل تماماً عن العتاد/الشبكة/cJSON (C11 صرف)
      include/*.h
      src/*.c
      CMakeLists.txt               # (ESP-C-02)
    wire_json/                     # (ESP-C-04) تحليل/بناء JSON تجريبي لمسودة عقد MQTT — يعتمد على core + cJSON
      include/*.h
      src/*.c
      CMakeLists.txt               # REQUIRES core json (مكوّن ESP-IDF المدمج، لا cJSON المستنسخة)
    port_esp32/                     # GPIO/NVS/RNG
    connectivity/                   # Wi-Fi/MQTT/SNTP/Identity/SoftAP/orchestrator (تجريبي)
  tests/host/                       # Unity على المضيف
third_party/                        # يُجلب عبر tools/fetch_host_deps.sh (Unity v2.6.1، cJSON v1.7.18)
tools/
  IDF_VERSION.lock                  # v5.3.6 / commit db32f7de…
  fetch_host_deps.sh
```

## تشغيل اختبارات المضيف

```bash
cd security_esp/firmware/tests/host
bash build_and_run.sh   # يستدعي fetch_host_deps.sh إن لزم
```

**آخر نتيجة فعلية (2026-11-19): 22 ملف، 100 اختبار، 0 فشل.**

## تجهيز بيئة ESP-IDF — الأوامر الفعلية المستخدَمة (قابلة لإعادة التنفيذ على جهاز آخر)

```powershell
# 1) Python (عبر winget)
winget install --id Python.Python.3.12 -e --accept-package-agreements --accept-source-agreements --silent

# 2) استنساخ ESP-IDF (فرع release/v5.3، سطحي مع الوحدات الفرعية)
mkdir %USERPROFILE%\esp
cd %USERPROFILE%\esp
git clone -b release/v5.3 --recursive --depth 1 --shallow-submodules https://github.com/espressif/esp-idf.git

# 3) تثبيت أدوات ESP-IDF — يجب تشغيله من PowerShell أصلية (native)، لا من
#    Git Bash/MSYS: install.ps1 يرفض صراحة أي بيئة MSYS/MinGW مكتشَفة من
#    متغيرات البيئة المتسربة (MSYSTEM) حتى عند استدعاء powershell.exe من
#    داخل bash. استخدم تبويب PowerShell مباشرة لا `bash -c "powershell ..."`.
cd %USERPROFILE%\esp\esp-idf
.\install.ps1 esp32

# 4) في كل جلسة جديدة قبل البناء:
. .\export.ps1
```

**درس مُسجَّل فعلياً (لا نظري):** أول محاولة لتشغيل `install.ps1` عبر استدعاء `powershell.exe` من داخل Git Bash فشلت بخطأ `"MSys/Mingw is not supported"` رغم أن الأمر نفسه أُعيد بنجاح لاحقاً عند تشغيله من تبويب PowerShell مباشرة بلا أي تغيير آخر — السبب متغيرات بيئة MSYS المتسربة (`MSYSTEM`) حتى عبر استدعاء عملية PowerShell فرعية. وثِّقت هذه الملاحظة لتوفير وقت إعادة المحاولة على جهاز آخر.

**عطل ثانٍ مُسجَّل (متكرر، سببه مُشخَّص لا مُخمَّن):** بعد تنزيل حزمة `xtensa-esp-elf-13.2.0_20250707` (281 MB) حتى 100%، يفشل `idf_tools.py` في إعادة تسمية الملف المؤقت `.tmp` إلى اسمه النهائي بخطأ `PermissionError: [WinError 32]` (قفل من عملية أخرى) — **مُعاد الحدوث في محاولتين متتاليتين على هذا الملف بالتحديد**. التشخيص: عملية `MsMpEng.exe` (محرك فحص Windows Defender الفعلي) كانت نشطة فعلياً وقت الفحص (`Get-Process MsMpEng` أعاد نتيجة بـCPU مرتفع)، متوافق تماماً مع تحذير السكربت نفسه. **لم يُعطَّل أي برنامج حماية ولم يُستخدم أي تجاوز — القرار المتبع: الانتظار الطبيعي لانتهاء الفحص، أو تشغيل `install.ps1` وحيداً دون أعمال بناء متوازية تستهلك المعالج/القرص، لتقليل مدة الفحص وتعارض القفل.** محاولة إعادة تسمية الملف يدوياً بعد انتهاء العملية الأصلية فشلت أيضاً في اللحظة نفسها (القفل لا يزال نشطاً وقت الفحص)، مما يؤكد أنه فحص قيد التنفيذ لا خطأ في السكربت نفسه. **إعادة المحاولة بعد انتظار طبيعي (بلا أي تجاوز) نجحت لاحقاً فعلياً — exit code 0.**

**عطل ثالث مُسجَّل (بيئي، له حل ثابت):** رغم أن `python --version` في PowerShell يطبع رقم إصدار صحيح (3.12.10) أحياناً، فإن `python.exe` الموجود أولاً في `PATH` هو فعلياً **App Execution Alias** تابع لمتجر Windows (`%LOCALAPPDATA%\Microsoft\WindowsApps\python.exe`) — رابط وهمي لا مفسِّر حقيقي. `install.ps1`/`export.ps1` يستدعيان `python` (بلا مسار كامل) فيضربان هذا الـalias فيفشلان بخطأ "Python was not found; run without arguments to install from the Microsoft Store...". المفسِّر الحقيقي مثبَّت فعلياً في `C:\Users\Osama\AppData\Local\Programs\Python\Python312\python.exe` (عبر `py -0p`/`py -c "import sys; print(sys.executable)"`). **الحل المُتحقَّق فعلياً:** تقديم مجلد هذا المفسِّر الحقيقي (ومجلد `Scripts` التابع له) في بداية `$env:PATH` لنفس جلسة PowerShell قبل تشغيل `install.ps1` أو `export.ps1` — لا حاجة لتغيير أي إعداد نظام أو تعطيل الـalias. **ملاحظة تهمّ أي جلسة مستقبلية:** تغييرات `$env:PATH` هذه لا تَبقى بين استدعاءات PowerShell منفصلة في هذه البيئة (كل استدعاء جلسة جديدة) — يجب تكرار هذا السطر في بداية كل جلسة تستخدم `idf.py`/`install.ps1`/`export.ps1`:
```powershell
$env:PATH = "C:\Users\Osama\AppData\Local\Programs\Python\Python312;C:\Users\Osama\AppData\Local\Programs\Python\Python312\Scripts;" + $env:PATH
```

## الحالة الفعلية المتحقَّقة (يُحدَّث بصدق، لا يُفترض)

| الأداة | الحالة | الإصدار المتحقَّق |
|---|---|---|
| GCC (WinLibs, للمضيف فقط) | ✅ مثبَّت ويعمل | 16.2.0 (POSIX.UCRT) |
| Python | ✅ مثبَّت ويعمل (عبر مسار صريح — alias المتجر لا يعمل، انظر "عطل ثالث" أعلاه) | 3.12.10 |
| Git | ✅ متوفر مسبقاً | 2.48.1 |
| استنساخ ESP-IDF | ✅ تم | `release/v5.3` → commit يطابق `v5.3.6` (`ESP_IDF_VERSION_MAJOR/MINOR/PATCH` في `esp_idf_version.h`) |
| `install.ps1` (أدوات ESP-IDF: toolchain، cmake، ninja، openocd، ccache) | ✅ **مكتمل فعلياً — تحقَّق بـ`idf.py --version`** | جميع الأدوات في `%USERPROFILE%\.espressif\tools\` (`xtensa-esp-elf` 13.2.0، `cmake` 3.30.2، `ninja` 1.12.1، `openocd-esp32` v0.12.0، `ccache` 4.12.1) + بيئة Python مستقلة `idf5.3_py3.12_env` |
| `idf.py --version` | ✅ نُفِّذ فعلياً | `ESP-IDF v5.3.6` |
| `idf.py set-target esp32` | ✅ نُفِّذ فعلياً | Configuring/Generating done |
| `idf.py build` (أولي، `core`+`wire_json`+`main`) | ✅ نُفِّذ فعلياً | `Project build complete`، صفر تحذيرات/أخطاء |
| `idf.py build` (بعد إضافة `port_esp32`) | ✅ نُفِّذ فعلياً — خطأ حقيقي واحد (`pin_bitmask`) اكتُشف وصُحِّح | صفر تحذيرات/أخطاء بعد التصحيح |
| **`idf.py fullclean` + `idf.py build` (2026-11-19، بعد connectivity)** | ✅ **نُفِّذ فعلياً** | **1018/1018**، `smart_security_esp.bin` = `0xea740` (960320 بايت، ~8% فراغ)، 0 أخطاء مترجم |
| `idf.py flash` | 🚫 **لم يُنفَّذ** | — |

**ESP-IDF المقيَّد:** انظر `security_esp/tools/IDF_VERSION.lock` (`v5.3.6` / `db32f7de6368445b431361f30e72be38ba2994d3`).

## البناء المستهدف (بعد نجاح `install.ps1`)

```powershell
cd %USERPROFILE%\esp\esp-idf
. .\export.ps1
cd <repo>\security_esp\firmware
idf.py set-target esp32
idf.py build
```

لا `idf.py flash` في أي جولة — ممنوع صريحاً (لا رفع فعلي على عتاد).

## `components/port_esp32/` — موجودة ومبنية فعلياً (ESP-C-03)

تربط `core` بـNVS حقيقي (`nvs_storage_backend.*`)، RNG عتادي حقيقي (`hw_rng.*`)، وGPIO حساس/صفارة باستقصاء دوري عبر `esp_timer` (`sensor_siren_port.*` — قرار مقصود، لا مقاطعة GPIO مباشرة، انظر `docs/reports/esp/ESP-C-03-report.md`). أرقام GPIO عناصر نائبة بانتظار `D-01`. `esp_wifi`/`esp_mqtt` (T-06) **غير مكتوبين بعد** — خارج نطاق `port_esp32` الحالي، ويبقيان عملاً لاحقاً (لا عتاد لاختبار اتصال شبكي حقيقي بأي حال).
