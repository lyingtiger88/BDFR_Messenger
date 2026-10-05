#include "bdfr_messaging_client.h"

#include <algorithm>
#include <optional>
#include <utility>

#include <QtCore/QJsonArray>
#include <QtCore/QJsonDocument>
#include <QtCore/QJsonObject>
#include <QtCore/QUrl>
#include <QtCore/QUrlQuery>
#include <QtNetwork/QNetworkReply>
#include <QtNetwork/QNetworkRequest>

namespace BDFR {
namespace {

QString ErrorText(QNetworkReply *reply, const QByteArray &payload) {
    const auto document = QJsonDocument::fromJson(payload);
    if (document.isObject()) {
        const auto object = document.object();
        for (const auto &key : { QStringLiteral("error"), QStringLiteral("message"), QStringLiteral("detail") }) {
            const auto value = object.value(key);
            if (value.isString() && !value.toString().isEmpty()) {
                return value.toString();
            }
        }
    }
    return reply->errorString().isEmpty()
        ? QStringLiteral("BDFR messaging request failed.")
        : reply->errorString();
}

std::optional<DirectMessage> ParseMessage(const QJsonObject &object) {
    DirectMessage message{
        .id = QUuid(object.value(QStringLiteral("id")).toString()),
        .senderId = QUuid(object.value(QStringLiteral("senderId")).toString()),
        .recipientId = QUuid(object.value(QStringLiteral("recipientId")).toString()),
        .content = object.value(QStringLiteral("content")).toString(),
        .sentAt = QDateTime::fromString(object.value(QStringLiteral("sentAt")).toString(), Qt::ISODate),
        .readAt = QDateTime::fromString(object.value(QStringLiteral("readAt")).toString(), Qt::ISODate),
    };
    if (message.id.isNull() || message.senderId.isNull() || message.recipientId.isNull()) {
        return std::nullopt;
    }
    return message;
}

} // namespace

MessagingClient::MessagingClient(QString baseUrl)
: _baseUrl(std::move(baseUrl)) {
    while (_baseUrl.endsWith('/')) _baseUrl.chop(1);
}

void MessagingClient::setAccessToken(QString accessToken) {
    _accessToken = std::move(accessToken);
}

QNetworkRequest MessagingClient::authorizedRequest(const QUrl &url) const {
    QNetworkRequest request(url);
    request.setHeader(QNetworkRequest::ContentTypeHeader, QStringLiteral("application/json"));
    if (!_accessToken.isEmpty()) {
        request.setRawHeader("Authorization", QByteArray("Bearer ") + _accessToken.toUtf8());
    }
    return request;
}

void MessagingClient::searchUsers(
        QString query,
        std::function<void(QList<UserSummary>)> success,
        std::function<void(ApiError)> failure) {
    QUrl url(_baseUrl + QStringLiteral("/api/users/search"));
    QUrlQuery params;
    params.addQueryItem(QStringLiteral("q"), std::move(query));
    url.setQuery(params);

    auto *reply = _network.get(authorizedRequest(url));
    QObject::connect(reply, &QNetworkReply::finished, [reply, success = std::move(success), failure = std::move(failure)]() mutable {
        const auto payload = reply->readAll();
        const int status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();
        const auto document = QJsonDocument::fromJson(payload);
        if (status >= 200 && status < 300 && document.isArray()) {
            QList<UserSummary> users;
            for (const auto &value : document.array()) {
                const auto object = value.toObject();
                UserSummary user{ QUuid(object.value(QStringLiteral("id")).toString()), object.value(QStringLiteral("username")).toString() };
                if (!user.id.isNull() && !user.username.isEmpty()) users.push_back(std::move(user));
            }
            if (success) success(std::move(users));
        } else if (failure) {
            failure({ status, ErrorText(reply, payload) });
        }
        reply->deleteLater();
    });
}

void MessagingClient::loadConversation(
        QUuid otherUserId,
        int take,
        std::function<void(QList<DirectMessage>)> success,
        std::function<void(ApiError)> failure) {
    QUrl url(_baseUrl + QStringLiteral("/api/messages/with/") + otherUserId.toString(QUuid::WithoutBraces));
    QUrlQuery params;
    params.addQueryItem(QStringLiteral("take"), QString::number(std::clamp(take, 1, 200)));
    url.setQuery(params);

    auto *reply = _network.get(authorizedRequest(url));
    QObject::connect(reply, &QNetworkReply::finished, [reply, success = std::move(success), failure = std::move(failure)]() mutable {
        const auto payload = reply->readAll();
        const int status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();
        const auto document = QJsonDocument::fromJson(payload);
        if (status >= 200 && status < 300 && document.isArray()) {
            QList<DirectMessage> messages;
            for (const auto &value : document.array()) {
                if (auto message = ParseMessage(value.toObject())) messages.push_back(std::move(*message));
            }
            if (success) success(std::move(messages));
        } else if (failure) {
            failure({ status, ErrorText(reply, payload) });
        }
        reply->deleteLater();
    });
}

void MessagingClient::sendDirectMessage(
        QUuid recipientUserId,
        QString content,
        std::function<void(DirectMessage)> success,
        std::function<void(ApiError)> failure) {
    const QUrl url(_baseUrl + QStringLiteral("/api/messages/to/") + recipientUserId.toString(QUuid::WithoutBraces));
    const auto payload = QJsonDocument(QJsonObject{
        { QStringLiteral("content"), std::move(content) },
    }).toJson(QJsonDocument::Compact);

    auto *reply = _network.post(authorizedRequest(url), payload);
    QObject::connect(reply, &QNetworkReply::finished, [reply, success = std::move(success), failure = std::move(failure)]() mutable {
        const auto payload = reply->readAll();
        const int status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();
        const auto document = QJsonDocument::fromJson(payload);
        if (status >= 200 && status < 300 && document.isObject()) {
            if (auto message = ParseMessage(document.object())) {
                if (success) success(std::move(*message));
            } else if (failure) {
                failure({ status, QStringLiteral("Invalid BDFR message response.") });
            }
        } else if (failure) {
            failure({ status, ErrorText(reply, payload) });
        }
        reply->deleteLater();
    });
}

void MessagingClient::markRead(
        QUuid messageId,
        std::function<void()> success,
        std::function<void(ApiError)> failure) {
    const QUrl url(_baseUrl + QStringLiteral("/api/messages/") +
        messageId.toString(QUuid::WithoutBraces) + QStringLiteral("/read"));
    auto *reply = _network.post(authorizedRequest(url), QByteArray("{}"));
    QObject::connect(reply, &QNetworkReply::finished, [reply, success = std::move(success), failure = std::move(failure)]() mutable {
        const auto payload = reply->readAll();
        const int status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();
        if (status >= 200 && status < 300) {
            if (success) success();
        } else if (failure) {
            failure({ status, ErrorText(reply, payload) });
        }
        reply->deleteLater();
    });
}

} // namespace BDFR
