#ifndef BG_NET_H
#define BG_NET_H

#include <esp_check.h>
#include <esp_netif.h>
#include <esp_log.h>
#include <esp_event.h>
#include <esp_wifi.h>
#include <string.h>
#include <stdint.h>

extern const char* NET_LOG_TAG;

/* initializes event loop, wifi, connects to AP  */

esp_err_t initialize_net(void);
const esp_netif_ip_info_t *get_net_info(void);

/* socket flavors */

esp_err_t create_multicast_ipv4_socket(const uint16_t port, int *socket_handle);

/* socket usage */

esp_err_t send_socket_message(const int socket_handle, const char *host, const uint16_t port);

esp_err_t wait_on_socket(const int socket_handle, const size_t timeout_in_ms);

#endif // BG_NET_H