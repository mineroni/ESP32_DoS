#include "webServer.hpp"

esp_err_t nc_get_handler(httpd_req_t *req)
{
    httpd_resp_send(req, "", HTTPD_RESP_USE_STRLEN);
    return ESP_OK;
}

esp_err_t static_get_handler(httpd_req_t *req)
{
    const char resp[] = "Simple static http response.<br><br>\
                        Available commands:\
                        <ul>\
                        <li>/hello (GET)</li>\
                        <li>/pi    (GET)</li>\
                        <li>/echo  (POST)</li>\
                        </ul>";
    httpd_resp_send(req, resp, HTTPD_RESP_USE_STRLEN);
    return ESP_OK;
}

static esp_err_t hello_get_handler(httpd_req_t *req)
{
    size_t buf_len = httpd_req_get_hdr_value_len(req, "User-Agent") + 1;
    if(buf_len > 1)
    {
        char* buf = new char[buf_len]();
        if(buf == NULL)
        {
            ESP_LOGE("HelloHandler", "Out of memory!");
            httpd_resp_send_500(req);
            return ESP_FAIL;
        }
        if (httpd_req_get_hdr_value_str(req, "User-Agent", buf, buf_len) == ESP_OK) 
        {
            char* resp = new char[buf_len + 7];
            sprintf(resp, "Hello %s!", buf);
            httpd_resp_send(req, resp, HTTPD_RESP_USE_STRLEN);
            delete[] resp;
        }
        else
        {
            httpd_resp_set_status(req, "400 Bad Request");
            httpd_resp_send(req, "Failed to get \"User-Agent\"!", HTTPD_RESP_USE_STRLEN);
        }
        delete[] buf;
    }
    else
    {
        httpd_resp_set_status(req, "400 Bad Request");
        httpd_resp_send(req, "Header \"User-Agent\" not found!", HTTPD_RESP_USE_STRLEN);
    }
    return ESP_OK;
}

double calculatePi(int loops)
{
    // Initialize denominator and sum
    double k = 1, s = 0;

    for (int i = 0; i < loops; ++i)
    {
        // even index elements are positive
        if (i % 2 == 0)
            s += 4 / k;
        // odd index elements are negative
        else
            s -= 4 / k;

        // denominator is odd
        k += 2;
    }
    return s;
}

esp_err_t pi_get_handler(httpd_req_t *req)
{
    char buf[100] = {0};
    sprintf(buf, "The value of PI (with 5 digit precision): %.5f", calculatePi(500000));
    httpd_resp_send(req, buf, HTTPD_RESP_USE_STRLEN);
    return ESP_OK;
}

static esp_err_t post_handler(httpd_req_t *req)
{   
    char* buf = new char[req->content_len + 1]();
    if(buf == NULL)
    {
        ESP_LOGE("HelloHandler", "Out of memory!");
        httpd_resp_send_500(req);
        delete[] buf;
        return ESP_FAIL;
    } 

    size_t ret;
    if ((ret = httpd_req_recv(req, buf, req->content_len)) <= 0)
    {
        delete[] buf;
        return ESP_FAIL;
    }

    httpd_resp_send(req, buf, HTTPD_RESP_USE_STRLEN);
    delete[] buf;
    return ESP_OK;
}

static const httpd_uri_t nocontentGet = {
    .uri      = "/nc",
    .method   = HTTP_GET,
    .handler  = nc_get_handler,
    .user_ctx = NULL,
};

static const httpd_uri_t staticGet = {
    .uri      = "/",
    .method   = HTTP_GET,
    .handler  = static_get_handler,
    .user_ctx = NULL,
};

static const httpd_uri_t helloGet = {
    .uri       = "/hello",
    .method    = HTTP_GET,
    .handler   = hello_get_handler,
    .user_ctx  = NULL
};

static const httpd_uri_t piGet = {
    .uri      = "/pi",
    .method   = HTTP_GET,
    .handler  = pi_get_handler,
    .user_ctx = NULL,
};

static const httpd_uri_t echoPost = {
    .uri       = "/echo",
    .method    = HTTP_POST,
    .handler   = post_handler,
    .user_ctx  = NULL
};

httpd_handle_t webServer::start_webserver()
{
    httpd_handle_t server = NULL;
    httpd_config_t config = HTTPD_DEFAULT_CONFIG();
    config.max_open_sockets = 7;
    config.stack_size = 8000;
    config.task_priority = configMAX_PRIORITIES - 1;

    // Start the httpd server
    ESP_LOGI("WebServer", "Starting server on port: '%d'", config.server_port);
    if (httpd_start(&server, &config) == ESP_OK) 
    {
        // Set URI handlers
        ESP_LOGI("WebServer", "Registering URI handlers");
        httpd_register_uri_handler(server, &nocontentGet);
        httpd_register_uri_handler(server, &staticGet);
        httpd_register_uri_handler(server, &helloGet);
        httpd_register_uri_handler(server, &piGet);
        httpd_register_uri_handler(server, &echoPost);
        return server;
    }

    ESP_LOGI("WebServer", "Error starting server!");
    return NULL;
}

esp_err_t webServer::stop_webserver(httpd_handle_t server)
{
    // Stop the httpd server
    return httpd_stop(server);
}