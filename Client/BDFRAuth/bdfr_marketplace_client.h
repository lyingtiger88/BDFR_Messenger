#pragma once

#include <functional>
#include <optional>

#include <QtCore/QDateTime>
#include <QtCore/QString>
#include <QtCore/QUuid>
#include <QtNetwork/QNetworkAccessManager>

namespace BDFR {

enum class StoreKind {
    Products = 0,
    Services = 1,
    ProductsAndServices = 2,
};

struct SellerProfile {
    QUuid id;
    QString username;
    QDateTime createdAt;
    QDateTime lastSeenAt;
    bool isActive = false;
    bool isSellerVerified = false;
    QString verificationBadge;
    QString verificationBadgeColor;
    bool hasStore = false;
};

struct StoreInfo {
    QUuid id;
    QUuid ownerUserId;
    QString name;
    QString description;
    StoreKind kind = StoreKind::Products;
    bool isActive = false;
    bool isSellerVerified = false;
    QString verificationBadge;
    QString verificationBadgeColor;
    QString bankName;
    QString cardLast4;
    QDateTime createdAt;
    QDateTime updatedAt;
};

struct MarketplaceError {
    int httpStatus = 0;
    QString message;
};

class MarketplaceClient final {
public:
    using ProfileSuccess = std::function<void(SellerProfile)>;
    using StoreSuccess = std::function<void(StoreInfo)>;
    using Failure = std::function<void(MarketplaceError)>;

    explicit MarketplaceClient(QString baseUrl);

    void getMyProfile(
        QString accessToken,
        ProfileSuccess success,
        Failure failure);

    void getStore(
        QString accessToken,
        QUuid ownerUserId,
        StoreSuccess success,
        Failure failure);

    void getMyStore(
        QString accessToken,
        StoreSuccess success,
        Failure failure);

    void createStore(
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
        Failure failure);

private:
    void request(
        QString method,
        QString path,
        QString accessToken,
        QByteArray payload,
        std::function<void(QNetworkReply*)> success,
        Failure failure);

    static std::optional<SellerProfile> parseProfile(const QByteArray &payload, QString *error);
    static std::optional<StoreInfo> parseStore(const QByteArray &payload, QString *error);

    QString _baseUrl;
    QNetworkAccessManager _network;
};

} // namespace BDFR
