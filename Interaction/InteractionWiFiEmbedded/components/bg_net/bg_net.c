
#include "bg_net.h"


#if CONFIG_WIFI_SCAN_METHOD_FAST
#define WIFI_SCAN_METHOD WIFI_FAST_SCAN
#elif CONFIG_WIFI_SCAN_METHOD_ALL_CHANNEL
#define WIFI_SCAN_METHOD WIFI_ALL_CHANNEL_SCAN
#endif

#if CONFIG_WIFI_CONNECT_AP_BY_SIGNAL
#define WIFI_CONNECT_AP_SORT_METHOD WIFI_CONNECT_AP_BY_SIGNAL
#elif CONFIG_WIFI_CONNECT_AP_BY_SECURITY
#define WIFI_CONNECT_AP_SORT_METHOD WIFI_CONNECT_AP_BY_SECURITY
#endif


#if CONFIG_WIFI_AUTH_OPEN
#define WIFI_SCAN_AUTH_MODE_THRESHOLD WIFI_AUTH_OPEN
#elif CONFIG_WIFI_AUTH_WEP
#define WIFI_SCAN_AUTH_MODE_THRESHOLD WIFI_AUTH_WEP
#elif CONFIG_WIFI_AUTH_WPA_PSK
#define WIFI_SCAN_AUTH_MODE_THRESHOLD WIFI_AUTH_WPA_PSK
#elif CONFIG_WIFI_AUTH_WPA2_PSK
#define WIFI_SCAN_AUTH_MODE_THRESHOLD WIFI_AUTH_WPA2_PSK
#elif CONFIG_WIFI_AUTH_WPA_WPA2_PSK
#define WIFI_SCAN_AUTH_MODE_THRESHOLD WIFI_AUTH_WPA_WPA2_PSK
#elif CONFIG_WIFI_AUTH_WPA2_ENTERPRISE
#define WIFI_SCAN_AUTH_MODE_THRESHOLD WIFI_AUTH_WPA2_ENTERPRISE
#elif CONFIG_WIFI_AUTH_WPA3_PSK
#define WIFI_SCAN_AUTH_MODE_THRESHOLD WIFI_AUTH_WPA3_PSK
#elif CONFIG_WIFI_AUTH_WPA2_WPA3_PSK
#define WIFI_SCAN_AUTH_MODE_THRESHOLD WIFI_AUTH_WPA2_WPA3_PSK
#elif CONFIG_WIFI_AUTH_WAPI_PSK
#define WIFI_SCAN_AUTH_MODE_THRESHOLD WIFI_AUTH_WAPI_PSK
#endif

#define MIN(x, y) (((x) < (y)) ? (x) : (y))


typedef struct {
    const char* ssid;
    const char* password;
} wifi_network_t;

wifi_network_t wifi_networks[] = {
    { CONFIG_WIFI_SSID_1, CONFIG_WIFI_PASSWORD_1 },
    { CONFIG_WIFI_SSID_2, CONFIG_WIFI_PASSWORD_2 },
    { CONFIG_WIFI_SSID_3, CONFIG_WIFI_PASSWORD_3 }
};

static int selected_network = -1;

#define NETIF_DESC_STA "netif_sta"


const char* NET_LOG_TAG = "net";


static esp_netif_ip_info_t *net_info = NULL;


static esp_err_t initialize_wifi(void);


esp_err_t initialize_net(void) {

    ESP_LOGI(NET_LOG_TAG, "Initializing net; setting up drivers, event loop, connecting to wifi");

    ESP_RETURN_ON_ERROR(esp_netif_init(), NET_LOG_TAG, "Failed to initialize netif");
    ESP_RETURN_ON_ERROR(esp_event_loop_create_default(), NET_LOG_TAG, "Failed to start event loop");
    ESP_RETURN_ON_ERROR(initialize_wifi(), NET_LOG_TAG, "Failed to connect to wifi");

    ESP_LOGI(NET_LOG_TAG, "Component successfully initialized");
    return ESP_OK;
}

/*
 * INITIALIZE WIFI
 */


static esp_err_t start_wifi(void);
static esp_err_t scan_networks(void);
static esp_err_t connect_wifi(void);
static void shutdown_wifi(void);


static esp_netif_t *net_sta_netif = NULL;


static esp_err_t initialize_wifi(void) {

    ESP_LOGI(NET_LOG_TAG, "Initializing wifi");

    ESP_RETURN_ON_ERROR(start_wifi(), NET_LOG_TAG, "Failed to start wifi driver");
    ESP_RETURN_ON_ERROR(esp_wifi_set_ps(WIFI_PS_NONE), NET_LOG_TAG, "Failed to set wifi power mode");
    ESP_RETURN_ON_ERROR(scan_networks(), NET_LOG_TAG, "Failed to find network from preconfigured SSIDs");
    ESP_RETURN_ON_ERROR(connect_wifi(), NET_LOG_TAG, "Failed to connect to specified AP");
    ESP_RETURN_ON_ERROR(
        esp_register_shutdown_handler(&shutdown_wifi),
        NET_LOG_TAG,
        "Failed to setup wifi shutdown handler"
    );

    ESP_LOGI(NET_LOG_TAG, "Finished initializing wifi!");
    return ESP_OK;
}

static esp_err_t start_wifi(void) {
    
    ESP_LOGI(NET_LOG_TAG, "Starting wifi driver");

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_RETURN_ON_ERROR(esp_wifi_init(&cfg), NET_LOG_TAG, "Invalid wifi config");

     esp_netif_inherent_config_t esp_netif_config = ESP_NETIF_INHERENT_DEFAULT_WIFI_STA();
    // Warning: the interface desc is used in tests to capture actual connection details (IP, gw, mask)
    esp_netif_config.if_desc = NETIF_DESC_STA;
    esp_netif_config.route_prio = 128;
    net_sta_netif = esp_netif_create_wifi(WIFI_IF_STA, &esp_netif_config);
    esp_wifi_set_default_wifi_sta_handlers();

    ESP_RETURN_ON_ERROR(esp_wifi_set_storage(WIFI_STORAGE_RAM), NET_LOG_TAG, "Unable to setup wifi sotrage");
    ESP_RETURN_ON_ERROR(esp_wifi_set_mode(WIFI_MODE_STA), NET_LOG_TAG, "Unable to set wifi to STA");
    ESP_RETURN_ON_ERROR(esp_wifi_start(), NET_LOG_TAG, "Unable to startup wifi driver");

    ESP_LOGI(NET_LOG_TAG, "Started wifi driver!");

    return ESP_OK;
}


static esp_err_t do_scan_networks(void);


static esp_err_t scan_networks(void) {

    ESP_LOGI(NET_LOG_TAG, "Scanning for predefined wifi networks");

    // ensure we have a valid network defined
    bool has_valid_network = false;
    for (int i = 0; i < sizeof(wifi_networks) / sizeof(wifi_network_t); i++) {
        const wifi_network_t *try_net = &wifi_networks[i];
        if (try_net->ssid[0] != '\0' && try_net->password[0] != '\0') {
            has_valid_network = true;
            break;
        }
    }
    if (!has_valid_network) {
        ESP_LOGE(NET_LOG_TAG, "No valid wifi networks defined");
        return ESP_ERR_NOT_FOUND;
    }

    esp_err_t ret = ESP_OK;
    for (int i = 0; i < CONFIG_WIFI_SCAN_MAX_RETRY; i++) {
        ESP_LOGI(NET_LOG_TAG, "Doing wifi scan");
        ret = do_scan_networks();
        if (ret == ESP_ERR_NOT_FOUND) {
            if (i != CONFIG_WIFI_SCAN_MAX_RETRY) {
                ESP_LOGW(NET_LOG_TAG, "Scan %i unsuccessful; trying again", i + 1);
                continue;
            } else {
                ESP_LOGE(NET_LOG_TAG, "Failed to find configured networks after %i scans; exiting", CONFIG_WIFI_SCAN_MAX_RETRY);
                break;
            }
        } else if (ret == ESP_OK) {
            if (0 <= selected_network && selected_network <= 2) {
                return ESP_OK;
            } else {
                ESP_LOGE(NET_LOG_TAG, "Scan returned ok but no network selected?");
                break;
            }
        } else {
            ESP_LOGE(NET_LOG_TAG, "Scan failed with error (%s)", esp_err_to_name(ret));
            break;
        }
    }
    return ESP_ERR_NOT_FINISHED;
}


static esp_err_t do_scan_networks(void) {
    
    uint16_t ap_found = CONFIG_WIFI_SCAN_LIST_SIZE;
    wifi_ap_record_t ap_info[CONFIG_WIFI_SCAN_LIST_SIZE];
    uint16_t ap_count = 0;
    memset(ap_info, 0, sizeof(ap_info));

    ESP_RETURN_ON_ERROR(esp_wifi_scan_start(NULL, true), NET_LOG_TAG, "Invalid wifi scan config");

    ESP_RETURN_ON_ERROR(esp_wifi_scan_get_ap_records(&ap_found, ap_info), NET_LOG_TAG, "Failed to fetch ap records");
    ESP_RETURN_ON_ERROR(esp_wifi_scan_get_ap_num(&ap_count), NET_LOG_TAG, "Failed to fetch ap count");
    ESP_LOGI(NET_LOG_TAG, "Total APs scanned = %u, actual AP number ap_info holds = %u", ap_count, ap_found);
    for (int i = 0; i < ap_found; i++) {
        const char *ap_ssid = (const char *) &ap_info[i].ssid;
        ESP_LOGI(NET_LOG_TAG, "Found SSID %s", ap_info[i].ssid);
        for (int j = 0; j < 3; j++) {
            const wifi_network_t *try_net = &wifi_networks[j];
            if (try_net->ssid[0] == '\0' || try_net->password[0] == '\0') {
                continue;
            }
            if (memcmp(ap_ssid, try_net->ssid, MIN(strlen(try_net->ssid), strlen(ap_ssid))) == 0) {
                ESP_LOGI(NET_LOG_TAG, "SSID matches configured network %i", j);
                selected_network = j;
                return ESP_OK;
            }
        }
    }
    ESP_LOGE(NET_LOG_TAG, "Couldn't find predefined wifi networks during scan");
    return ESP_ERR_NOT_FOUND;
}


/*
 * CONNECT WIFI
 */


static void handle_wifi_disconnect(
    void *arg, esp_event_base_t event_base, int32_t event_id, void *event_data
);
static void handle_wifi_ip(
    void *arg, esp_event_base_t event_base, int32_t event_id, void *event_data
);


static SemaphoreHandle_t net_wait_ip_semph = NULL;
static int net_connect_retry_cnt = 0;


static esp_err_t connect_wifi(void) {

    ESP_LOGI(NET_LOG_TAG, "Connecting to wifi");

    if (selected_network != 0 && selected_network != 1 && selected_network != 2) {
        ESP_LOGE(NET_LOG_TAG, "Invalid wifi network selected");
        return ESP_ERR_NOT_FOUND;
    } else if (
        wifi_networks[selected_network].ssid[0] == '\0' ||
        wifi_networks[selected_network].password[0] == '\0'
    ) {
        ESP_LOGE(NET_LOG_TAG, "Selected wifi network is misconfigured");
        return ESP_ERR_INVALID_ARG;
    }

    
    wifi_config_t wifi_config = {0};

    memcpy(wifi_config.sta.ssid, wifi_networks[selected_network].ssid, strlen(wifi_networks[selected_network].ssid) + 1);
    memcpy(wifi_config.sta.password, wifi_networks[selected_network].password, strlen(wifi_networks[selected_network].password) + 1);

    wifi_config.sta.scan_method = WIFI_SCAN_METHOD;
    wifi_config.sta.sort_method = WIFI_CONNECT_AP_SORT_METHOD;
    wifi_config.sta.threshold.rssi = CONFIG_WIFI_SCAN_RSSI_THRESHOLD;
    wifi_config.sta.threshold.authmode = WIFI_SCAN_AUTH_MODE_THRESHOLD;

    net_wait_ip_semph = xSemaphoreCreateBinary();
    if (net_wait_ip_semph == NULL) {
        ESP_LOGE(NET_LOG_TAG, "Failed to create connect sempahore; out of memory?");
        return ESP_ERR_NO_MEM;
    }

    net_connect_retry_cnt = 0;
    ESP_RETURN_ON_ERROR(
        esp_event_handler_register(WIFI_EVENT, WIFI_EVENT_STA_DISCONNECTED, &handle_wifi_disconnect, NULL),
        NET_LOG_TAG,
        "Unable to setup wifi disconnect handler"
    );
    ESP_RETURN_ON_ERROR(
        esp_event_handler_register(IP_EVENT, IP_EVENT_STA_GOT_IP, &handle_wifi_ip, NULL),
        NET_LOG_TAG,
        "Unable to setup wifi ip handler"
    );

    ESP_LOGI(NET_LOG_TAG, "Connecting to %s", wifi_config.sta.ssid);
    ESP_RETURN_ON_ERROR(esp_wifi_set_config(WIFI_IF_STA, &wifi_config), NET_LOG_TAG, "Invalid wifi config");
    ESP_RETURN_ON_ERROR(esp_wifi_connect(), NET_LOG_TAG, "Failed to initiate wifi connection");

    ESP_LOGI(NET_LOG_TAG, "Waiting for IP(s)");
    xSemaphoreTake(net_wait_ip_semph, portMAX_DELAY);
    if (net_connect_retry_cnt > CONFIG_WIFI_CONN_MAX_RETRY) {
        ESP_LOGE(NET_LOG_TAG, "Unable to connect to wifi in %d attempts", net_connect_retry_cnt);
        return ESP_ERR_TIMEOUT;
    }

    ESP_LOGI(NET_LOG_TAG, "Connected to wifi!");
    return ESP_OK;
}

static void handle_wifi_disconnect(
    void *arg, esp_event_base_t event_base, int32_t event_id, void *event_data
) {
    if (++net_connect_retry_cnt > CONFIG_WIFI_CONN_MAX_RETRY) {
        ESP_LOGI(NET_LOG_TAG, "WiFi Connect failed %d times, stop reconnect.", net_connect_retry_cnt);
        if (net_wait_ip_semph) {
            xSemaphoreGive(net_wait_ip_semph);
        }
        return;
    }

    ESP_LOGI(NET_LOG_TAG, "Wi-Fi disconnected, trying to reconnect...");
    esp_err_t err = esp_wifi_connect();
    if (err == ESP_ERR_WIFI_NOT_STARTED) {
        return;
    }
    ESP_ERROR_CHECK(err);
}

static bool is_our_netif(const char *prefix, esp_netif_t *netif)
{
    return strncmp(prefix, esp_netif_get_desc(netif), strlen(prefix) - 1) == 0;
}

static void handle_wifi_ip(
    void *arg, esp_event_base_t event_base, int32_t event_id, void *event_data
) {
    net_connect_retry_cnt = 0;
    ip_event_got_ip_t *event = (ip_event_got_ip_t *)event_data;
    if (!is_our_netif(NETIF_DESC_STA, event->esp_netif)) {
        return;
    }
    ESP_LOGI(
        NET_LOG_TAG,
        "Got IPv4 event: Interface \"%s\" address: " IPSTR,
        esp_netif_get_desc(event->esp_netif),
        IP2STR(&event->ip_info.ip)
    );
    if (!net_wait_ip_semph) {
        ESP_LOGW(NET_LOG_TAG, "Got IP but think we are already bailing");
        return;
    }
    xSemaphoreGive(net_wait_ip_semph);
    const size_t ip_size = sizeof(esp_netif_ip_info_t);
    net_info = malloc(ip_size);
    if (net_info == NULL) {
        ESP_LOGE(NET_LOG_TAG, "Unable to allocate memory for ip; panicking");
        esp_restart();
    }
    memcpy(net_info, &event->ip_info, ip_size);
}


/*
 * SHUTDOWN WIFI
 */

static void shutdown_wifi(void) {
    // teardown listeners
    ESP_ERROR_CHECK(esp_event_handler_unregister(WIFI_EVENT, WIFI_EVENT_STA_DISCONNECTED, &handle_wifi_disconnect));
    ESP_ERROR_CHECK(esp_event_handler_unregister(IP_EVENT, IP_EVENT_STA_GOT_IP, &handle_wifi_ip));
    if (net_wait_ip_semph) {
        vSemaphoreDelete(net_wait_ip_semph);
    }

    // actually disconnect
    esp_wifi_disconnect();
    esp_err_t err = esp_wifi_stop();
    if (err == ESP_ERR_WIFI_NOT_INIT) {
        return;
    }

    // deinit
    ESP_ERROR_CHECK(err);
    ESP_ERROR_CHECK(esp_wifi_deinit());
    ESP_ERROR_CHECK(esp_wifi_clear_default_wifi_driver_and_handlers(net_sta_netif));
    esp_netif_destroy(net_sta_netif);
    net_sta_netif = NULL;
}


const esp_netif_ip_info_t *get_net_info(void) {
    if (net_info == NULL) {
        while (1) {
            ESP_LOGE(NET_LOG_TAG, "Never found ip addr... you're stuck here");
            vTaskDelay(2500 / portTICK_PERIOD_MS);
        }
    }
    return net_info;
}
