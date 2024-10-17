

#include "bg_net.h"

#include <esp_log.h>
#include <esp_system.h>
#include <freertos/FreeRTOS.h>
#include <freertos/task.h>


static const char *MAIN_TAG = "main";


// extern void tcp_client(void);


void app_main(void)
{
    ESP_LOGI(MAIN_TAG, "WC24 Startup..");
    ESP_LOGI(MAIN_TAG, "Free memory: %" PRIu32 " bytes", esp_get_free_heap_size());
    ESP_LOGI(MAIN_TAG, "IDF version: %s", esp_get_idf_version());

    esp_log_level_set("*", ESP_LOG_INFO);
    
    
    ESP_ERROR_CHECK(initialize_net());

    ESP_LOGI(MAIN_TAG, "WC24 Fully Initialized!");
}