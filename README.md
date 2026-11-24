# Packstation Simulator

An ASP.NET Core MVC web application written in F# for a parcel locker workflow. It includes sender, recipient, employee, and administration views, parcel tracking and pickup-code flows, locker/compartment records, MySQL persistence, and email notifications. The Raspberry Pi integration is optional and is disabled until SSH is configured.

F# provides concise, strongly typed domain and application code while running on the .NET platform and integrating with ASP.NET Core, MySQL, MailKit, and SSH.NET. The project is a prototype: several screens currently check that a user is signed in but do not enforce role-specific authorization. Add and test authorization policies before deploying with real users or parcels.

## Requirements

- .NET 10 SDK
- MySQL server and a database account allowed to create/use the configured database
- Python 3 on the Raspberry Pi only when hardware control is needed

## Configure and run the web app

Do not put real passwords, email addresses, private keys, or device credentials in tracked files. Set local environment variables or use a secret manager. For example, in PowerShell:

```powershell
$env:ConnectionStrings__Admin = "server=localhost;port=3306;user id=<db-user>;password=<db-password>;charset=utf8mb4;"
$env:Db__Database = "PackstationDb"
$env:EmailSettings__SmtpHost = "<smtp-host>"
$env:EmailSettings__SmtpPort = "587"
$env:EmailSettings__SmtpUser = "<smtp-user>"
$env:EmailSettings__SmtpPassword = "<smtp-password>"
$env:EmailSettings__FromAddress = "<sender-address>"
dotnet run
```

`ConnectionStrings__Admin` must identify a database account that can create the configured database. The application creates its schema during startup. The checked-in `appsettings.json` intentionally leaves account and service credentials empty.

## Raspberry Pi and servo setup

The scripts in `raspberry-pi/` use an Adafruit 16-channel PWM/servo driver over I2C and the Adafruit CircuitPython ServoKit library. Enable I2C on the Pi, connect the driver and servos according to the board documentation, and test with a mechanically safe load. Confirm the gate's open and closed angles before connecting real lockers.

Install Python and create a virtual environment on the Pi:

```sh
sudo apt update
sudo apt install -y python3 python3-venv
python3 -m venv --system-site-packages /home/pi/.venvs/packstation
/home/pi/.venvs/packstation/bin/pip install adafruit-circuitpython-servokit
```

Install the scripts under `/home/pi`. To create the application script manually, run `nano /home/pi/gate_control.py`, paste the contents of `raspberry-pi/gate_control.py`, then save with **Ctrl+O**, **Enter**, and exit with **Ctrl+X**. Copy both files from a machine that has the project instead if preferred:

```sh
scp raspberry-pi/gate_control.py raspberry-pi/all_servos_test.py <pi-user>@<pi-host>:/home/pi/
```

Run a single-channel test on the Pi; choose any channel from 0 to 15:

```sh
. /home/pi/.venvs/packstation/bin/activate
cd /home/pi
python3 all_servos_test.py 0
python3 all_servos_test.py 1
deactivate
# Continue with channels 2 through 15 as needed.
```

The test briefly sets the selected servo to the configured open angle and then returns it to the configured closed angle. For an explicit gate command:

```sh
/home/pi/.venvs/packstation/bin/python /home/pi/gate_control.py 0 open
/home/pi/.venvs/packstation/bin/python /home/pi/gate_control.py 0 close
```

Adjust `OPEN_ANGLE` and `CLOSED_ANGLE` in `gate_control.py` to match the mechanism. In the web application, compartment numbers 1-16 map to servo channels 0-15 respectively; compartments outside that range are rejected for hardware control.

### Configure SSH access from the web-app host

Use SSH public-key authentication; do not put a Pi password or private key in the repository. Create a dedicated key for the web-app host, authorize its public key for the Pi account, and verify the Pi host key fingerprint out of band. Set these environment variables on the machine running the web app:

```text
RaspberryPi__Host=<pi-hostname-or-address>
RaspberryPi__Port=22
RaspberryPi__User=<pi-user>
RaspberryPi__PrivateKeyPath=<private-key-path-on-the-web-app-host>
RaspberryPi__HostKeyFingerprint=<verified-MD5-fingerprint-without-the-MD5-prefix>
RaspberryPi__PythonPath=/home/pi/.venvs/packstation/bin/python
RaspberryPi__ScriptPath=/home/pi/gate_control.py
```

The fingerprint is compared against the SSH server key before commands are sent. Keep the private key readable only by the service account running the web application. The app invokes the script with the compartment's mapped channel and an `open` or `close` action.

## Project layout

- `Controllers/`, `Models/`, `Repositories/`: MVC endpoints, domain types, and MySQL access
- `Views/`, `wwwroot/`: Razor pages and static assets
- `raspberry-pi/`: Python gate-control and single-channel test scripts
- `appsettings.json`: non-secret defaults; use environment variables for local credentials
- `wwwroot/uploads/`: runtime uploads, ignored by Git

## Packstation-Simulator

Diese ASP.NET-Core-MVC-Webanwendung ist in F# geschrieben und bildet einen Paketstation-Ablauf ab. Sie enthält Ansichten für Absender, Empfänger, Mitarbeiter und Administration, Sendungsverfolgung und Abholcodes, Paketstationen und Fächer, MySQL-Speicherung sowie E-Mail-Benachrichtigungen. Die Raspberry-Pi-Anbindung ist optional und bleibt deaktiviert, bis SSH sicher eingerichtet wurde.

F# bietet prägnanten, stark typisierten Code für Fachlogik und Anwendung und läuft zugleich auf .NET mit Zugriff auf ASP.NET Core, MySQL, MailKit und SSH.NET. Das Projekt ist ein Prototyp: Einige Seiten prüfen derzeit nur die Anmeldung und erzwingen keine rollenbezogene Autorisierung. Vor einem Einsatz mit echten Benutzern oder Paketen müssen Autorisierungsrichtlinien ergänzt und getestet werden.

### Voraussetzungen und Start

Benötigt werden das .NET-10-SDK, ein MySQL-Server und ein Datenbankkonto, das die konfigurierte Datenbank erstellen und verwenden darf. Zugangsdaten gehören nicht in eingecheckte Dateien. Setze sie lokal als Umgebungsvariablen, zum Beispiel `ConnectionStrings__Admin`, `EmailSettings__SmtpHost`, `EmailSettings__SmtpUser`, `EmailSettings__SmtpPassword` und `EmailSettings__FromAddress`, und starte anschließend `dotnet run`. Beim Start legt die Anwendung das Schema an. Die Werte in `appsettings.json` für Zugangsdaten sind absichtlich leer.

### Raspberry Pi und Servos

Die Skripte in `raspberry-pi/` verwenden einen Adafruit-PWM-/Servo-Treiber mit 16 Kanälen über I2C und das Python-Paket **Adafruit CircuitPython ServoKit**. Aktiviere I2C auf dem Raspberry Pi und verbinde Treiber und Servos entsprechend der Herstellerdokumentation. Prüfe die Mechanik und die Öffnungs- und Schließwinkel zunächst ohne angeschlossene reale Paketfächer.

Installiere Python und das virtuelle Environment auf dem Pi:

```sh
sudo apt update
sudo apt install -y python3 python3-venv
python3 -m venv --system-site-packages /home/pi/.venvs/packstation
/home/pi/.venvs/packstation/bin/pip install adafruit-circuitpython-servokit
```

Lege das Skript unter `/home/pi` ab: Mit `nano /home/pi/gate_control.py` kannst du den Inhalt von `raspberry-pi/gate_control.py` einfügen und mit **Ctrl+O**, **Enter**, **Ctrl+X** speichern und beenden. Alternativ kannst du die Dateien mit `scp raspberry-pi/gate_control.py raspberry-pi/all_servos_test.py <pi-user>@<pi-host>:/home/pi/` übertragen.

Teste einen Kanal von **0 bis 15**:

```sh
. /home/pi/.venvs/packstation/bin/activate
cd /home/pi
python3 all_servos_test.py 0
python3 all_servos_test.py 1
deactivate
```

Der Test fährt den gewählten Servo kurz auf den Öffnungswinkel und danach zurück auf den Schließwinkel. Passe `OPEN_ANGLE` und `CLOSED_ANGLE` in `gate_control.py` an. Für einen direkten Befehl verwendest du beispielsweise `/home/pi/.venvs/packstation/bin/python /home/pi/gate_control.py 0 open` oder denselben Aufruf mit `close`. Fachnummern 1-16 werden in der Webanwendung den Servo-Kanälen 0-15 zugeordnet; andere Fachnummern werden für die Hardwaresteuerung abgewiesen.

### SSH-Konfiguration

Verwende SSH-Schlüssel statt eines Pi-Passworts. Autorisiere den öffentlichen Schlüssel auf dem Pi, prüfe dessen Host-Key-Fingerprint über einen vertrauenswürdigen Weg und setze auf dem Web-App-Host `RaspberryPi__Host`, `RaspberryPi__Port`, `RaspberryPi__User`, `RaspberryPi__PrivateKeyPath`, `RaspberryPi__HostKeyFingerprint`, `RaspberryPi__PythonPath` und `RaspberryPi__ScriptPath`. Der private Schlüssel muss auf dem Web-App-Host liegen und nur für dessen Dienstkonto lesbar sein. Der Host-Key wird vor der Ausführung geprüft.

## محاكي محطة الطرود

هذا تطبيق ويب ببنية ASP.NET Core MVC ومكتوب بلغة F# لمحاكاة دورة عمل محطة الطرود. يتضمن واجهات للمرسل والمستلم والموظف والإدارة، وتتبع الشحنات ورموز الاستلام وسجلات المحطات والخانات، والتخزين باستخدام MySQL وإشعارات البريد الإلكتروني. ربط الراسبيري باي اختياري ولا يعمل قبل إعداد SSH بصورة آمنة.

تقدم F# صياغة موجزة وأنواعاً قوية لتمثيل منطق المجال، مع الاستفادة من منصة .NET والتكامل مع ASP.NET Core وMySQL وMailKit وSSH.NET. المشروع نموذج أولي؛ بعض الصفحات تتحقق حالياً من تسجيل الدخول فقط ولا تفرض صلاحيات منفصلة حسب الدور. يجب إضافة سياسات التفويض واختبارها قبل استخدام التطبيق مع مستخدمين أو طرود حقيقية.

### المتطلبات والتشغيل

تحتاج إلى .NET SDK 10 وخادم MySQL وحساب قاعدة بيانات يمكنه إنشاء قاعدة البيانات المحددة واستخدامها. لا تحفظ كلمات المرور أو بيانات الحسابات في الملفات المتتبعة في Git. أضف الإعدادات محلياً كمتغيرات بيئة مثل `ConnectionStrings__Admin` و`EmailSettings__SmtpHost` و`EmailSettings__SmtpUser` و`EmailSettings__SmtpPassword` و`EmailSettings__FromAddress` ثم شغّل `dotnet run`. تنشئ الخدمة الجداول عند التشغيل، وتبقى بيانات الدخول في `appsettings.json` فارغة عمداً.

### الراسبيري باي ومحركات السيرفو

تستخدم السكربتات في `raspberry-pi/` لوحة تحكم Adafruit ذات 16 قناة PWM/Servo عبر I2C ومكتبة بايثون **Adafruit CircuitPython ServoKit**. فعّل I2C على الراسبيري باي، ووصل اللوحة والمحركات وفق توثيق الشركة المصنّعة. اختبر حركة المحرك وزاويتي الفتح والإغلاق بحذر قبل ربط أبواب الخانات الحقيقية.

ثبّت بايثون وأنشئ بيئة افتراضية على الراسبيري باي:

```sh
sudo apt update
sudo apt install -y python3 python3-venv
python3 -m venv --system-site-packages /home/pi/.venvs/packstation
/home/pi/.venvs/packstation/bin/pip install adafruit-circuitpython-servokit
```

ضع السكربت في `/home/pi`: نفّذ `nano /home/pi/gate_control.py` ثم الصق محتوى `raspberry-pi/gate_control.py` واحفظ باستخدام **Ctrl+O** ثم **Enter** واخرج بـ **Ctrl+X**. أو انقل السكربتين من جهاز المشروع باستخدام `scp raspberry-pi/gate_control.py raspberry-pi/all_servos_test.py <pi-user>@<pi-host>:/home/pi/`.

اختبر قناة واحدة من **0 إلى 15**:

```sh
. /home/pi/.venvs/packstation/bin/activate
cd /home/pi
python3 all_servos_test.py 0
python3 all_servos_test.py 1
deactivate
```

يفتح الاختبار المحرك المختار مؤقتاً ثم يعيده إلى زاوية الإغلاق. عدّل `OPEN_ANGLE` و`CLOSED_ANGLE` في `gate_control.py` لتناسب آلية الباب. يمكن إرسال أمر مباشر مثل `/home/pi/.venvs/packstation/bin/python /home/pi/gate_control.py 0 open` أو استخدام `close`. تربط الخدمة أرقام الخانات 1-16 بالقنوات 0-15؛ وترفض أرقام الخانات خارج هذا النطاق عند التحكم بالأجهزة.

### إعداد SSH

استخدم المصادقة بمفتاح SSH بدلاً من كلمة مرور الراسبيري باي. أضف المفتاح العام لحساب الراسبيري باي، وتحقق من بصمة مفتاح المضيف بطريقة موثوقة، ثم اضبط متغيرات البيئة `RaspberryPi__Host` و`RaspberryPi__Port` و`RaspberryPi__User` و`RaspberryPi__PrivateKeyPath` و`RaspberryPi__HostKeyFingerprint` و`RaspberryPi__PythonPath` و`RaspberryPi__ScriptPath` على خادم تطبيق الويب. يبقى المفتاح الخاص على خادم التطبيق ويجب أن يقتصر الوصول إليه على حساب الخدمة. يتحقق التطبيق من بصمة مضيف SSH قبل إرسال الأمر.
