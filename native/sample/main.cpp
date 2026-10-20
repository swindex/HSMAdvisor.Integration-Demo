// Native HSMAdvisor integration sample: load HSMAdvisorIntegration.dll, say Hello, calculate an end mill.
// The C++ counterpart of IntegrationSamples 01_Connect and 03_CalculateMilling.
//
// The DLL installed with HSMAdvisor is preferred; the copy next to this exe (copied from
// lib\native\<x64|x86> by the build) is the fallback for HSMAdvisor versions that do not install it.
#include <windows.h>

#include <cstdio>
#include <string>

#include "hsmadvisor_integration_loader.h"

static void OnLaunching(void*)
{
    std::printf("Starting HSMAdvisor...\n");
}

// Value of the first "name":"value" string field after `after` (enough for this sample;
// use a JSON library in real code).
static std::string StringField(const std::string& json, const char* name, const char* after = "")
{
    size_t from = json.find(after);
    std::string key = std::string("\"") + name + "\":\"";
    size_t at = json.find(key, from == std::string::npos ? 0 : from);
    if (at == std::string::npos) return "";
    at += key.size();
    return json.substr(at, json.find('"', at) - at);
}

// Prints the result; returns the payload (empty on failure).
static std::string Run(hsma_api& api, hsma_client* client, const char* type, const char* payload)
{
    char* response = nullptr;
    char* error = nullptr;
    int status = api.request(client, type, payload, 0, nullptr, &response, &error);
    std::string result = response ? response : "";
    if (status == HSMA_OK)
        std::printf("%s: ok\n", type);
    else if (status == HSMA_CANCELLED)
        std::printf("%s: cancelled\n", type);
    else
        std::printf("%s failed: %s\n", type, error ? error : "");
    api.free_memory(response);
    api.free_memory(error);
    return result;
}

int main()
{
    wchar_t exe[MAX_PATH];
    GetModuleFileNameW(nullptr, exe, MAX_PATH);
    std::wstring bundled(exe);
    bundled = bundled.substr(0, bundled.find_last_of(L'\\') + 1) + L"HSMAdvisorIntegration.dll";

    hsma_api api;
    hsma_load_result loaded = hsma_load(&api, bundled.c_str());
    if (loaded != HSMA_LOAD_OK)
    {
        std::printf("HSMAdvisorIntegration.dll could not be loaded (%d).\n", (int)loaded);
        return 1;
    }
    std::wprintf(L"Loaded %ls\n", hsma_loaded_from(&api));

    char* found = api.find_executable();
    std::printf("HSMAdvisor.exe: %s\n", found ? found : "(not installed, see " HSMA_DOWNLOAD_URL ")");
    api.free_memory(found);

    // Keep one client for the life of your application.
    hsma_client* client = api.client_create("HSMAdvisor native sample", "1.0");
    api.client_set_minimum_version(client, "3.2.0"); // older HSMAdvisor -> "version_too_old"
    api.client_set_launching_callback(client, OnLaunching, nullptr);

    std::string hello = Run(api, client, "Hello", nullptr);
    if (!hello.empty()) std::printf("  HSMAdvisor %s\n", StringField(hello, "app_version").c_str());

    // 12 mm 4-flute carbide end mill in 1018 steel (227, license-free). Values are strings; the *_mm
    // flags make them metric. Fields that are not sent take HSMAdvisor's defaults - including the
    // cut's diameter/stickout unit flags, which win over the tool's, so send those on the cut too.
    // feed_mm / result_units_mm return the feed in mm/min.
    const char* calculate = R"({"calculation":{
        "tool":{"name_id":"1","tool_material_id":"5","coating_id":"4",
                "diameter":"12","diameter_mm":"1","Flute_N":"4",
                "Flute_Len":"36","flute_len_mm":"1","stickout":"48","stickout_mm":"1"},
        "cut":{"material_id":"227","diameter":"12","diameter_mm":"1","stickout":"48","stickout_mm":"1",
               "doc":"12","doc_mm":"1","woc":"1.2","woc_mm":"1",
               "feed_mm":"1","input_units_mm":"1","result_units_mm":"1"}},
        "preset_speeds":true})";
    std::string calculated = Run(api, client, "Calculate", calculate);
    if (!calculated.empty())
    {
        std::printf("  RPM:  %s\n", StringField(calculated, "rpm", "\"cut\"").c_str());
        std::printf("  Feed: %s mm/min\n", StringField(calculated, "feed", "\"cut\"").c_str());
    }

    api.client_destroy(client);
    hsma_unload(&api);
    return 0;
}
