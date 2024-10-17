
#include "bg_net.h"

#include <sys/time.h>
#include <sys/select.h>
#include <errno.h>
#include <lwip/sockets.h>

/*
 * SOCKETS
 */

esp_err_t create_multicast_ipv4_socket(const uint16_t port, int *socket_handle) {

    ESP_LOGI(NET_LOG_TAG, "Creating multicast socket");

    esp_err_t ret = ESP_OK;

    /* setup socket */

    struct sockaddr_in saddr = { 0 };
    int err = 0;

    *socket_handle = socket(PF_INET, SOCK_DGRAM, IPPROTO_IP);
    if (*socket_handle < 0) {
        ESP_LOGE(NET_LOG_TAG, "Failed to create socket. Error %d", errno);
        return ESP_ERR_ESP_NETIF_INIT_FAILED;
    }

    saddr.sin_family = PF_INET;
    saddr.sin_port = htons(port);
    saddr.sin_addr.s_addr = htonl(INADDR_ANY);  // bind to any local iface
    err = bind(*socket_handle, (struct sockaddr *)&saddr, sizeof(struct sockaddr_in));
    if (err < 0) {
        ESP_LOGE(NET_LOG_TAG, "Failed to bind socket. Error %d", errno);
        ret = ESP_ERR_ESP_NETIF_DHCP_ALREADY_STARTED;
        goto err_sock;
    }

    uint8_t ttl = 255;
    err = setsockopt(sock, IPPROTO_IP, IP_MULTICAST_TTL, &ttl, sizeof(uint8_t));
    if (err < 0) {
        ESP_LOGE(PD_LOG_TAG, "Failed to set IP_MULTICAST_TTL. Error %d", errno);
        ret = ESP_ERR
        goto err_sock;
    }

    uint8_t loopback_val = 1;
    err = setsockopt(sock, IPPROTO_IP, IP_MULTICAST_LOOP, &loopback_val, sizeof(uint8_t));
    if (err < 0) {
        ESP_LOGE(PD_LOG_TAG, "Failed to set IP_MULTICAST_LOOP. Error %d", errno);
        goto err_sock;
    }

    /* connect it to multicast */

    struct ip_mreq imreq = { 0 };
    struct in_addr iaddr = { 0 };

    // Configure source interface
    const esp_netif_ip_info_t *net_info = get_net_info();

    imreq.imr_interface.s_addr = net_info->ip.addr;
    imreq.imr_multiaddr.s_addr = inet_addr(DISCOVERY_HOST);

    // Configure multicast address to listen to
    if (!IP_MULTICAST(ntohl(imreq.imr_multiaddr.s_addr))) {
        ESP_LOGW(PD_LOG_TAG, "Configured IPV4 multicast address '%s' is not a valid multicast address", DISCOVERY_HOST);
        goto err_sock;
    } else {
        ESP_LOGI(PD_LOG_TAG, "Configured IPV4 Multicast address %s", inet_ntoa(imreq.imr_multiaddr.s_addr));
    }

    err = setsockopt(sock, IPPROTO_IP, IP_ADD_MEMBERSHIP, &imreq, sizeof(struct ip_mreq));
    if (err < 0) {
        ESP_LOGE(PD_LOG_TAG, "Failed to set IP_ADD_MEMBERSHIP. Error %d", errno);
        goto err_sock;
    }

        
    iaddr.s_addr = net_info->ip.addr;
   
    // Assign the IPv4 multicast source interface, via its IP
    err = setsockopt(sock, IPPROTO_IP, IP_MULTICAST_IF, &iaddr, sizeof(struct in_addr));
    if (err < 0) {
        ESP_LOGE(PD_LOG_TAG, "Failed to set IP_MULTICAST_IF. Error %d", errno);
        goto err_sock;
    }
    
    ESP_LOGI(PD_LOG_TAG, "Successfully created multicast socket!");

    return sock;

err_sock:
    close(sock);
    return -1;
}

/*
 * NET UTILS
 */

static void set_timeval(struct timeval *t, size_t time_in_ms) {
    t->tv_sec = time_in_ms / 1000;
    t->tv_usec = (time_in_ms % 1000) * 1000;
}


esp_err_t wait_on_socket(const int socket_handle, const size_t timeout_in_ms) {
    struct timeval timeout_val = { 0 };
    memset(&timeout_val, 0, sizeof(struct timeval));

    set_timeval(&timeout_val, timeout_in_ms);
    fd_set rfds = { 0 };
    FD_ZERO(&rfds);
    FD_SET(socket_handle, &rfds);
    int s = 0;
    s = select(socket_handle + 1, &rfds, NULL, NULL, &timeout_val);
    if (s < 0) {
        ESP_LOGE(NET_LOG_TAG, "Select failed: errno %d", errno);
        return ESP_ERR_INVALID_RESPONSE;
    } else if (s == 0) {
        ESP_LOGW(NET_LOG_TAG, "Recieve listener timed out");
        return ESP_ERR_TIMEOUT;
    } else if (!FD_ISSET(socket_handle, &rfds)) {
        // datagram triggered the select, but isn't set? Some spurious wakeup thing?
        // ignore it for now
        ESP_LOGE(NET_LOG_TAG, "No idea what's going on...");
        return ESP_ERR_INVALID_STATE;
    }
    return ESP_OK;
}
