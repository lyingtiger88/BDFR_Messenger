#include "bdfr_auth_client.h"

#include <QtCore/QJsonDocument>
#include <QtCore/QJsonObject>
#include <QtCore/QUrl>
#include <QtNetwork/QNetworkReply>
#include <QtNetwork/QNetworkRequest>

namespace BDFR {
namespace {

QByteArray JsonBytes(QJsonObject object) {
    return QJsonDocument(std::move(object)).toJson(QJsonDocument::Compact);
}

QString ErrorMessage(QNetworkReply *reply, const QByteArray &payload) {
    const auto document = QJsonDocument::fromJson(payload);
    if (document.isObject()) {
        const auto object = document.object();
        const auto value = object.value(QStringLiteral("error"));
        if (value.isString()) {
            return value.toString();
        }
    }
    const auto network = reply->errorString();
    return network.isEmpty()
        ? QStringLiteral("BDFR authentication request failed.")
        : network;
}

} // namespace

AuthClient::AuthClient(QString baseUrl)
: _baseUrl(std::move(baseUrl)) {
    while (_baseUrl.endsWith('/')) {
        _baseUrl.chop(1);
    }
}

void AuthClient::login(
        QString login,
        QString password,
        QString deviceName,
        QString platform,
        Success success,
        Failure failure) {
    auto object = QJsonObject{
        { QStringLiteral("login"), std::move(login) },
        { QStringLiteral("password"), std::move(password) },
        { QStringLiteral("deviceName"), std::move(deviceName) },
        { QStringLiteral("platform"), std::move(platform) },
    };
    postAuth(
        QStringLiteral("/api/auth/login"),
        JsonBytes(std::move(object)),
        std::move(success),
        std::move(failure));
}

void AuthClient::refresh(
        QString refreshToken,
        Success success,
        Failure failure) {
    auto object = QJsonObject{
        { QStringLiteral("refreshToken"), std::move(refreshToken) },
    };
    postAuth(
        QStringLiteral("/api/auth/refresh"),
        JsonBytes(std::move(object)),
        std::move(success),
        std::move(failure));
}

void AuthClient::logout(
        QString refreshToken,
        std::function<void()> success,
        Failure failure) {
    const auto url = QUrl(_baseUrl + QStringLiteral("/api/auth/logout"));
    auto request = QNetworkRequest(url);
    request.setHeader(QNetworkRequest::ContentTypeHeader, QStringLiteral("application/json"));

    auto *reply = _network.post(
        request,
        JsonBytes(QJsonObject{
            { QStringLiteral("refreshToken"), std::move(refreshToken) },
        }));

    QObject::connect(reply, &QNetworkReply::finished, [reply, success = std::move(success), failure = std::move(failure)]() mutable {
        const auto payload = reply->readAll();
        const auto status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();
        const auto ok = (status >= 200 && status < 300);
        if (ok) {
            if (success) success();
        } else if (failure) {
            failure(AuthError{ status, ErrorMessage(reply, payload) });
        }
        reply->deleteLater();
    });
}

void AuthClient::postAuth(
        QString path,
        QByteArray payload,
        Success success,
        Failure failure) {
    const auto url = QUrl(_baseUrl + path);
    auto request = QNetworkRequest(url);
    request.setHeader(QNetworkRequest::ContentTypeHeader, QStringLiteral("application/json"));

    auto *reply = _network.post(request, payload);
    QObject::connect(reply, &QNetworkReply::finished, [reply, success = std::move(success), failure = std::move(failure)]() mutable {
        const auto payload = reply->readAll();
        const auto status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();

        if (status >= 200 && status < 300) {
            auto parseError = QString();
            const auto session = parseSession(payload, &parseError);
            if (session) {
                if (success) success(*session);
            } else if (failure) {
                failure(AuthError{ status, parseError });
            }
        } else if (failure) {
            failure(AuthError{ status, ErrorMessage(reply, payload) });
        }

        reply->deleteLater();
    });
}

std::optional<AuthSession> AuthClient::parseSession(
        const QByteArray &payload,
        QString *error) {
    const auto document = QJsonDocument::fromJson(payload);
    if (!document.isObject()) {
        if (error) *error = QStringLiteral("Invalid BDFR authentication response.");
        return std::nullopt;
    }

    const auto object = document.object();
    AuthSession result{
        .userId = QUuid(object.value(QStringLiteral("userId")).toString()),
        .username = object.value(QStringLiteral("username")).toString(),
        .accessToken = object.value(QStringLiteral("accessToken")).toString(),
        .refreshToken = object.value(QStringLiteral("refreshToken")).toString(),
    };

    if (result.userId.isNull()
        || result.username.isEmpty()
        || result.accessToken.isEmpty()
        || result.refreshToken.isEmpty()) {
        if (error) *error = QStringLiteral("Incomplete BDFR authentication response.");
        return std::nullopt;
    }
    return result;
}

} // namespace BDFR
