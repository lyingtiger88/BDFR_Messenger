add_library(bdfr_auth STATIC
    bdfr_auth_client.cpp
    bdfr_auth_client.h
    bdfr_marketplace_client.cpp
    bdfr_marketplace_client.h
)

target_link_libraries(bdfr_auth
    PRIVATE
        Qt::Core
        Qt::Network
)

target_include_directories(bdfr_auth PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
