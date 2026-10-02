#pragma once

#include <functional>
#include <optional>

#include <QtCore/QString>
#include <QtCore/QUuid>
#include <QtNetwork/QNetworkAccessManager>

namespace BDFR {

struct AuthSession {
    QUuid userId;
    QString username;
    QString accessToken;
    QString refreshToken;
    bool isSellerVerified = false;
};

struct AuthError {
    int httpStatus = 0;
    QString message;
};

class AuthClient final {
public:
    using Success = std::function<void(AuthSession)>;
    using Failure = std::function<void(AuthError)>;

    explicit AuthClient(QString baseUrl);

    void login(
        QString login,
        QString password,
        QString deviceName,
        QString platform,
        Success success,
        Failure failure);

    void refresh(
        QString refreshToken,
        Success success,
        Failure failure);

    void logout(
        QString refreshToken,
        std::function<void()> success,
        Failure failure);

private:
    void postAuth(
        QString path,
        QByteArray payload,
        Success success,
        Failure failure);

    static std::optional<AuthSession> parseSession(
        const QByteArray &payload,
        QString *error);

    QString _baseUrl;
    QNetworkAccessManager _network;
};

} // namespace BDFR
