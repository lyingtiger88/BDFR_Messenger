#pragma once

#include <functional>

#include <QtCore/QDateTime>
#include <QtCore/QList>
#include <QtCore/QString>
#include <QtCore/QUuid>
#include <QtNetwork/QNetworkAccessManager>

namespace BDFR {

struct UserSummary {
    QUuid id;
    QString username;
};

struct DirectMessage {
    QUuid id;
    QUuid senderId;
    QUuid recipientId;
    QString content;
    QDateTime sentAt;
    QDateTime readAt;
};

struct ApiError {
    int httpStatus = 0;
    QString message;
};

class MessagingClient final {
public:
    explicit MessagingClient(QString baseUrl);

    void setAccessToken(QString accessToken);

    void searchUsers(
        QString query,
        std::function<void(QList<UserSummary>)> success,
        std::function<void(ApiError)> failure);

    void loadConversation(
        QUuid otherUserId,
        int take,
        std::function<void(QList<DirectMessage>)> success,
        std::function<void(ApiError)> failure);

    void sendDirectMessage(
        QUuid recipientUserId,
        QString content,
        std::function<void(DirectMessage)> success,
        std::function<void(ApiError)> failure);

    void markRead(
        QUuid messageId,
        std::function<void()> success,
        std::function<void(ApiError)> failure);

private:
    QNetworkRequest authorizedRequest(const QUrl &url) const;
    QString _baseUrl;
    QString _accessToken;
    QNetworkAccessManager _network;
};

} // namespace BDFR
