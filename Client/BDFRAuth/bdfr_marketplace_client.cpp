#include "bdfr_marketplace_client.h"

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
        if (value.isString()) return value.toString();
    }
    const auto network = reply->errorString();
    return network.isEmpty()
        ? QStringLiteral("BDFR marketplace request failed.")
        : network;
}

StoreKind ParseKind(int value) {
    switch (value) {
    case 1: return StoreKind::Services;
    case 2: return StoreKind::ProductsAndServices;
    default: return StoreKind::Products;
    }
}

} // namespace

MarketplaceClient::MarketplaceClient(QString baseUrl)
: _baseUrl(std::move(baseUrl)) {
    while (_baseUrl.endsWith('/')) _baseUrl.chop(1);
}

void MarketplaceClient::getMyProfile(
        QString accessToken,
        ProfileSuccess success,
        Failure failure) {
    request(
        QStringLiteral("GET"),
        QStringLiteral("/api/users/me"),
        std::move(accessToken),
        {},
        [success = std::move(success), failure = std::move(failure)](QNetworkReply *reply) mutable {
            const auto payload = reply->readAll();
            const auto status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();
            if (status >= 200 && status < 300) {
                QString error;
                const auto profile = parseProfile(payload, &error);
                if (profile) {
                    if (success) success(*profile);
                } else if (failure) {
                    failure({ status, error });
                }
            } else if (failure) {
                failure({ status, ErrorMessage(reply, payload) });
            }
        },
        std::move(failure));
}

void MarketplaceClient::getStore(
        QString accessToken,
        QUuid ownerUserId,
        StoreSuccess success,
        Failure failure) {
    request(
        QStringLiteral("GET"),
        QStringLiteral("/api/marketplace/stores/") + ownerUserId.toString(QUuid::WithoutBraces),
        std::move(accessToken),
        {},
        [success = std::move(success), failure = std::move(failure)](QNetworkReply *reply) mutable {
            const auto payload = reply->readAll();
            const auto status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();
            if (status >= 200 && status < 300) {
                QString error;
                const auto store = parseStore(payload, &error);
                if (store) {
                    if (success) success(*store);
                } else if (failure) {
                    failure({ status, error });
                }
            } else if (failure) {
                failure({ status, ErrorMessage(reply, payload) });
            }
        },
        std::move(failure));
}

void MarketplaceClient::getMyStore(
        QString accessToken,
        StoreSuccess success,
        Failure failure) {
    request(
        QStringLiteral("GET"),
        QStringLiteral("/api/marketplace/stores/me"),
        std::move(accessToken),
        {},
        [success = std::move(success), failure = std::move(failure)](QNetworkReply *reply) mutable {
            const auto payload = reply->readAll();
            const auto status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();
            if (status >= 200 && status < 300) {
                QString error;
                const auto store = parseStore(payload, &error);
                if (store) {
                    if (success) success(*store);
                } else if (failure) {
                    failure({ status, error });
                }
            } else if (failure) {
                failure({ status, ErrorMessage(reply, payload) });
            }
        },
        std::move(failure));
}

void MarketplaceClient::createStore(
        QString accessToken,
        QString name,
        QString description,
        StoreKind kind,
        QString cardNumber,
        QString iban,
        QString accountNumber,
        QString bankCode,
        QString nationalCode,
        QString birthDate,
        StoreSuccess success,
        Failure failure) {
    const auto payload = JsonBytes(QJsonObject{
        { QStringLiteral("name"), std::move(name) },
        { QStringLiteral("description"), std::move(description) },
        { QStringLiteral("kind"), static_cast<int>(kind) },
        { QStringLiteral("cardNumber"), std::move(cardNumber) },
        { QStringLiteral("iban"), std::move(iban) },
        { QStringLiteral("accountNumber"), std::move(accountNumber) },
        { QStringLiteral("bankCode"), std::move(bankCode) },
        { QStringLiteral("nationalCode"), std::move(nationalCode) },
        { QStringLiteral("birthDate"), std::move(birthDate) },
    });

    request(
        QStringLiteral("POST"),
        QStringLiteral("/api/marketplace/stores"),
        std::move(accessToken),
        payload,
        [success = std::move(success), failure = std::move(failure)](QNetworkReply *reply) mutable {
            const auto payload = reply->readAll();
            const auto status = reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();
            if (status >= 200 && status < 300) {
                QString error;
                const auto store = parseStore(payload, &error);
                if (store) {
                    if (success) success(*store);
                } else if (failure) {
                    failure({ status, error });
                }
            } else if (failure) {
                failure({ status, ErrorMessage(reply, payload) });
            }
        },
        std::move(failure));
}

void MarketplaceClient::request(
        QString method,
        QString path,
        QString accessToken,
        QByteArray payload,
        std::function<void(QNetworkReply*)> success,
        Failure failure) {
    const auto url = QUrl(_baseUrl + path);
    auto request = QNetworkRequest(url);
    request.setHeader(QNetworkRequest::ContentTypeHeader, QStringLiteral("application/json"));
    request.setRawHeader("Authorization", QByteArray("Bearer ") + accessToken.toUtf8());

    QNetworkReply *reply = nullptr;
    if (method == QStringLiteral("POST")) {
        reply = _network.post(request, payload);
    } else {
        reply = _network.get(request);
    }

    QObject::connect(reply, &QNetworkReply::finished,
        [reply, success = std::move(success), failure = std::move(failure)]() mutable {
            if (success) success(reply);
            reply->deleteLater();
        });
}

std::optional<SellerProfile> MarketplaceClient::parseProfile(
        const QByteArray &payload,
        QString *error) {
    const auto document = QJsonDocument::fromJson(payload);
    if (!document.isObject()) {
        if (error) *error = QStringLiteral("Invalid BDFR profile response.");
        return std::nullopt;
    }

    const auto object = document.object();
    SellerProfile result{
        .id = QUuid(object.value(QStringLiteral("id")).toString()),
        .username = object.value(QStringLiteral("username")).toString(),
        .createdAt = QDateTime::fromString(object.value(QStringLiteral("createdAt")).toString(), Qt::ISODateWithMs),
        .lastSeenAt = QDateTime::fromString(object.value(QStringLiteral("lastSeenAt")).toString(), Qt::ISODateWithMs),
        .isActive = object.value(QStringLiteral("isActive")).toBool(),
        .isSellerVerified = object.value(QStringLiteral("isSellerVerified")).toBool(),
        .verificationBadge = object.value(QStringLiteral("verificationBadge")).toString(),
        .verificationBadgeColor = object.value(QStringLiteral("verificationBadgeColor")).toString(),
        .hasStore = object.value(QStringLiteral("hasStore")).toBool(),
    };

    if (result.id.isNull() || result.username.isEmpty()) {
        if (error) *error = QStringLiteral("Incomplete BDFR profile response.");
        return std::nullopt;
    }
    return result;
}

std::optional<StoreInfo> MarketplaceClient::parseStore(
        const QByteArray &payload,
        QString *error) {
    const auto document = QJsonDocument::fromJson(payload);
    if (!document.isObject()) {
        if (error) *error = QStringLiteral("Invalid BDFR store response.");
        return std::nullopt;
    }

    const auto object = document.object();
    StoreInfo result{
        .id = QUuid(object.value(QStringLiteral("id")).toString()),
        .ownerUserId = QUuid(object.value(QStringLiteral("ownerUserId")).toString()),
        .name = object.value(QStringLiteral("name")).toString(),
        .description = object.value(QStringLiteral("description")).toString(),
        .kind = ParseKind(object.value(QStringLiteral("kind")).toInt()),
        .isActive = object.value(QStringLiteral("isActive")).toBool(),
        .isSellerVerified = object.value(QStringLiteral("isSellerVerified")).toBool(),
        .verificationBadge = object.value(QStringLiteral("verificationBadge")).toString(),
        .verificationBadgeColor = object.value(QStringLiteral("verificationBadgeColor")).toString(),
        .bankName = object.value(QStringLiteral("bankName")).toString(),
        .cardLast4 = object.value(QStringLiteral("cardLast4")).toString(),
        .createdAt = QDateTime::fromString(object.value(QStringLiteral("createdAt")).toString(), Qt::ISODateWithMs),
        .updatedAt = QDateTime::fromString(object.value(QStringLiteral("updatedAt")).toString(), Qt::ISODateWithMs),
    };

    if (result.id.isNull() || result.ownerUserId.isNull() || result.name.isEmpty()) {
        if (error) *error = QStringLiteral("Incomplete BDFR store response.");
        return std::nullopt;
    }
    return result;
}

} // namespace BDFR
