
#ifndef POLIS_ETHERNET_HPP
#define POLIS_ETHERNET_HPP

#include <Arduino.h>

#include <QNEthernet.h>

#include "utils.hpp"
#include "config.hpp"


namespace qn = qindesign::network;

constexpr uint32_t kDHCPTimeout = 15'000;  // 15 seconds

void SetupEthernet(qn::EthernetUDP &udp, const Config &config) {
    if (!qn::Ethernet.begin()) {
        infinitePrint("Failed to startup ethernet manager");
    }
    bool ip_set = false;
    if (config.use_dhcp) {
        if (qn::Ethernet.waitForLocalIP(kDHCPTimeout)) {
            ip_set = true;
        } else {
            Serial.println("Unable to get DHCP address");
        }
    }
    if (!ip_set && config.local_ip != INADDR_NONE) {
        qn::Ethernet.setLocalIP(config.local_ip);
    } else {
        infinitePrint("DHCP failed and local IP is not set");
    }

    udp.begin(ARTNET_PORT);
}



#endif // POLIS_ETHERNET_HPP