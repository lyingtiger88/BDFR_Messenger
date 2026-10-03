# BDFR Messenger - Telegram Desktop BDFR login bootstrap
$ErrorActionPreference = "Stop"
$root = (Get-Location).Path
if (-not (Test-Path (Join-Path $root "CMakeLists.txt"))) { throw "Run this script from Client\TelegramDesktop." }
$telegram = Join-Path $root "Telegram"
$intro = Join-Path $telegram "SourceFiles\intro"
$bdfr = Join-Path (Split-Path $root -Parent) "BDFRAuth"
if (-not (Test-Path $bdfr)) { throw "BDFRAuth not found at $bdfr" }
function Backup-Once([string]$path) {
    $bak = "$path.bdfr-v1.bak"
    if (-not (Test-Path $bak)) { Copy-Item $path $bak }
}
function Save-Text([string]$path, [string]$text) { Set-Content -Path $path -Value $text -Encoding utf8NoBOM }
$nl = [Environment]::NewLine

$rootCmake = Join-Path $root "CMakeLists.txt"
$rootText = Get-Content $rootCmake -Raw
if (-not $rootText.Contains("BDFR_V1_AUTH_TARGET")) {
    Backup-Once $rootCmake
    $anchor = "add_subdirectory(Telegram)"
    $replacement = "# BDFR_V1_AUTH_TARGET$nl" + 'add_subdirectory(${CMAKE_CURRENT_SOURCE_DIR}/../BDFRAuth ${CMAKE_BINARY_DIR}/bdfr_auth)' + "$nl# END_BDFR_V1_AUTH_TARGET$nl$nl$anchor"
    if (-not $rootText.Contains($anchor)) { throw "Root CMake anchor not found." }
    Save-Text $rootCmake ($rootText.Replace($anchor, $replacement))
}

$telegramCmake = Join-Path $telegram "CMakeLists.txt"
$telegramText = Get-Content $telegramCmake -Raw
if (-not $telegramText.Contains("BDFR_V1_AUTH_LINK")) {
    Backup-Once $telegramCmake
    $anchor = "target_link_libraries(Telegram" + $nl + "PRIVATE"
    if (-not $telegramText.Contains($anchor)) { throw "Telegram target_link_libraries anchor not found." }
    $replacement = $anchor + $nl + $nl + "# BDFR_V1_AUTH_LINK" + $nl + "bdfr_auth" + $nl + "# END_BDFR_V1_AUTH_LINK"
    $telegramText = $telegramText.Replace($anchor, $replacement)
}
if (-not $telegramText.Contains("intro/intro_bdfr.cpp")) {
    Backup-Once $telegramCmake
    if (-not $telegramText.Contains("intro/intro_start.cpp")) { throw "intro/intro_start.cpp not found." }
    $telegramText = $telegramText.Replace("intro/intro_start.cpp", "intro/intro_start.cpp$nl" + "intro/intro_bdfr.cpp")
    if ($telegramText.Contains("intro/intro_start.h") -and -not $telegramText.Contains("intro/intro_bdfr.h")) {
        $telegramText = $telegramText.Replace("intro/intro_start.h", "intro/intro_start.h$nl" + "intro/intro_bdfr.h")
    }
}
Save-Text $telegramCmake $telegramText

$bdfrH = @'
#pragma once
#include "intro/intro_step.h"
#include <memory>
namespace Ui { class InputField; class PasswordInput; }
namespace BDFR { class AuthClient; struct AuthSession; struct AuthError; }
namespace Intro {
namespace details {
class BDFRWidget final : public Step {
public:
    BDFRWidget(QWidget *parent, not_null<Main::Account*> account, not_null<Data*> data);
    void submit() override;
    rpl::producer<QString> nextButtonText() const override;
    void activate() override;
    void setInnerFocus() override;
    bool hasBack() const override { return true; }
protected:
    void resizeEvent(QResizeEvent *e) override;
private:
    void updateControlsGeometry();
    void loginSucceeded(BDFR::AuthSession session);
    void loginFailed(BDFR::AuthError error);
    object_ptr<Ui::InputField> _login;
    object_ptr<Ui::PasswordInput> _password;
    std::unique_ptr<BDFR::AuthClient> _auth;
    bool _requestInFlight = false;
    bool _authenticated = false;
};
} // namespace details
} // namespace Intro
'@

$bdfrCpp = @'
#include "intro/intro_bdfr.h"
#include "bdfr_auth_client.h"
#include "styles/style_intro.h"
#include "ui/widgets/fields/input_field.h"
#include "ui/widgets/fields/password_input.h"

namespace Intro {
namespace details {

BDFRWidget::BDFRWidget(
        QWidget *parent,
        not_null<Main::Account*> account,
        not_null<Data*> data)
: Step(parent, account, data)
, _login(this, st::introName, tr::lng_login_email(), QString())
, _password(this, st::introName, rpl::single(u"Password"_q), QString())
, _auth(std::make_unique<BDFR::AuthClient>(QStringLiteral("http://localhost:8080"))) {
    setErrorCentered(true);
    setTitleText(rpl::single(u"BDFR Messenger"_q));
    setMouseTracking(true);
    setTabOrder(_login, _password);
}

void BDFRWidget::activate() {
    Step::activate();
    _login->show();
    _password->show();
    updateControlsGeometry();
    setInnerFocus();
}

void BDFRWidget::setInnerFocus() {
    if (_password->hasFocus()) {
        _password->setFocusFast();
    } else {
        _login->setFocusFast();
    }
}

void BDFRWidget::resizeEvent(QResizeEvent *e) {
    Step::resizeEvent(e);
    updateControlsGeometry();
}

void BDFRWidget::updateControlsGeometry() {
    const auto firstTop = contentTop() + st::introStepFieldTop;
    const auto secondTop = firstTop + st::introName.heightMin + st::introPhoneTop;
    _login->moveToLeft(contentLeft(), firstTop);
    _password->moveToLeft(contentLeft(), secondTop);
}

void BDFRWidget::submit() {
    if (_requestInFlight || _authenticated) return;
    const auto login = _login->getLastText().trimmed();
    const auto password = _password->getLastText();
    if (login.isEmpty()) {
        showError(rpl::single(u"Enter your email or username."_q));
        _login->setFocus();
        return;
    }
    if (password.isEmpty()) {
        showError(rpl::single(u"Enter your password."_q));
        _password->setFocus();
        return;
    }
    hideError();
    _requestInFlight = true;
    _auth->login(
        login,
        password,
        QStringLiteral("BDFR Desktop"),
        QStringLiteral("Windows"),
        [this](BDFR::AuthSession session) { loginSucceeded(std::move(session)); },
        [this](BDFR::AuthError error) { loginFailed(std::move(error)); });
}

void BDFRWidget::loginSucceeded(BDFR::AuthSession session) {
    Q_UNUSED(session);
    _requestInFlight = false;
    _authenticated = true;
    _login->setEnabled(false);
    _password->setEnabled(false);
    showError(rpl::single(u"BDFR login successful."_q));
}

void BDFRWidget::loginFailed(BDFR::AuthError error) {
    _requestInFlight = false;
    const auto message = error.message.isEmpty()
        ? QStringLiteral("BDFR login failed (HTTP %1).").arg(error.httpStatus)
        : error.message;
    showError(rpl::single(message));
}

rpl::producer<QString> BDFRWidget::nextButtonText() const {
    return _authenticated ? rpl::single(u"Done"_q) : rpl::single(u"Sign in"_q);
}

} // namespace details
} // namespace Intro
'@

Save-Text (Join-Path $intro "intro_bdfr.h") $bdfrH
Save-Text (Join-Path $intro "intro_bdfr.cpp") $bdfrCpp

$startH = Join-Path $intro "intro_start.h"
$startCpp = Join-Path $intro "intro_start.cpp"
$startHText = Get-Content $startH -Raw
if (-not $startHText.Contains('#include "intro/intro_bdfr.h"')) {
    Backup-Once $startH
    $startHText = $startHText.Replace('#include "intro/intro_step.h"', '#include "intro/intro_step.h"' + $nl + '#include "intro/intro_bdfr.h"')
    Save-Text $startH $startHText
}
$startCppText = Get-Content $startCpp -Raw
if (-not $startCppText.Contains("goNext<BDFRWidget>()")) {
    Backup-Once $startCpp
    if (-not $startCppText.Contains("goNext<QrWidget>();")) { throw "goNext<QrWidget>() not found." }
    Save-Text $startCpp ($startCppText.Replace("goNext<QrWidget>();", "goNext<BDFRWidget>();"))
}
Write-Host "BDFR Login v1 applied." -ForegroundColor Green
Write-Host 'Run: cmake -S . -B out -G "Visual Studio 17 2022" -A x64'
Write-Host 'Then: cmake --build out --config Release --parallel'
