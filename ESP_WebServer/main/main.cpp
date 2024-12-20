#include <stdio.h>

#include <freertos/FreeRTOS.h>
#include <freertos/task.h>
#include <esp_http_server.h>

#include "wifi.hpp"
#include "webServer.hpp"

extern "C" void app_main()
{  
    static httpd_handle_t server = NULL;

    Wifi::init();

    server = webServer::start_webserver();

    while (server)
    {
        vTaskDelay(1000/portTICK_PERIOD_MS);
    }
}