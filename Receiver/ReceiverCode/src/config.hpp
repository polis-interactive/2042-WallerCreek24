
#ifndef POLIS_CONFIG_HPP
#define POLIS_CONFIG_HPP

#include <Arduino.h>

#include <SD.h>
#include <SPI.h>

#include <ArduinoJson.h>

#include <NativeEthernet.h>

// #include <QNEthernet.h>

#include <string>
#include <sstream>
#include <vector>

#include "utils.hpp"

#define ARTNET_PORT 6454

constexpr int MaxString = 5;
constexpr int MaxStrings = 24;
constexpr int MaxLeds = MaxString * MaxStrings;


std::string config_file_prefix = "receiver_config_";
std::string config_file_suffix = ".json";

struct Config {
    int universe;
    int channels;
    int strings;
    bool is_rgbw;
    bool use_dhcp;
    IPAddress local_ip;
};

void ReadConfig(Config &config) {
    if (MaxLeds > 512) {
        infinitePrint("Oh no, you possibly have more pixels than an artnet universe...");
    }
    if (!SD.begin(BUILTIN_SDCARD)) {
        infinitePrint("Error reading SD card");
    }
    auto root = SD.open("/", FILE_READ);
    File entry;
    std::stringstream ss;
    while (true) {
        entry = root.openNextFile(FILE_READ);
        if (!entry) {
            break;
        } else if (entry.isDirectory()) {
            continue;
        }
        auto file_name = entry.name();
        if (!beginsWith(file_name, config_file_prefix)) {
            continue;
        } else if (!endsWith(file_name, config_file_suffix)) {
            continue;
        }
        break;
    }
    root.close();
    if (!entry) {
        ss.clear();
        ss << "Couldn't find confile with format (" <<
            config_file_prefix << "*" << config_file_suffix << ") on SD card"
        ;
        infinitePrint(ss.str());
    }
    const auto entry_name = entry.name();
    const auto entry_size = entry.size();
    if (entry_size <= 0) {
        entry.close();
        ss.clear();
        ss << "Found file " << entry_name << " has size of 0";
        infinitePrint(ss.str());
    }
    
    char json[entry_size];
    for (size_t i = 0; i < entry_size; i++) {
        json[i] = entry.read();
    }
    entry.close();

    JsonDocument doc;
    auto err = deserializeJson(doc, json);
    if (err) {
        ss.clear();
        ss << "Failed to deserialize json of file " << entry_name;
        infinitePrint(ss.str());
    }

    const int universe = doc["universe"] | -1;
    if (universe < 0) {
        infinitePrint("Universe is either missing from config, or has a non positive value");
    }
    
    const int channels = doc["channels"] | 0;
    if (channels <= 0) {
        infinitePrint("Channels is either missing from config, or has a non positive value");
    } else if (channels > 512) {
        infinitePrint("Channels is bigger than an artnet universe");
    }
    const int strings = doc["strings"] | 0;
    if (strings <= 0) {
        infinitePrint("Strings is either missing from config, or has a non positive value");
    } else if (strings > MaxStrings) {
        infinitePrint("Strings is larger than physical outputs");
    }

    const bool is_rgbw = doc["is_rgbw"] | false;

    const bool use_dhcp = doc["use_dhcp"] | false;
    const std::string local_ip = doc["local_ip"] | "";

    if (!local_ip.empty() && !config.local_ip.fromString(local_ip.c_str())) {
        infinitePrint("local_ip is set to an invalid value");
    } else if (local_ip.empty() && !use_dhcp) {
        infinitePrint("local_ip is not set while use_dhcp is false or not set...");
    }

    config.universe = universe;
    config.channels = channels;
    config.strings = strings;
    config.is_rgbw = is_rgbw;
    config.use_dhcp = use_dhcp;
}



#endif // POLIS_CONFIG_HPP