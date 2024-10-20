#ifndef WEBSERVER_H
#define WEBSERVER_H

#include <stdlib.h>
#include <esp_http_server.h>
#include <esp_log.h>

class webServer
{
    private:
        webServer();
        ~webServer();
    public:
        static httpd_handle_t start_webserver();
        static esp_err_t stop_webserver(httpd_handle_t server);
};

#endif