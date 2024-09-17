
#include <Arduino.h>

#include "config.hpp"
#include "ethernet.hpp"
#include "artnet.hpp"
#include "controller.hpp"


Config config;

namespace qn = qindesign::network;
qn::EthernetUDP udp;

Artnet artnet;

Controller controller;


void setup() {
    Serial.begin(115200);
    Serial.println("Setup started");
    pinMode(LED_BUILTIN, OUTPUT);
    digitalWrite(LED_BUILTIN, HIGH);
    Serial.println("reading config");
    ReadConfig(config);
    Serial.println("setting up ethernet");
    SetupEthernet(udp, config);
    Serial.println("setting up artnet");
    SetupArtnet(artnet, config);
    Serial.println("setting up leds");
    SetupController(controller, config);
    Serial.println("Setup done!");
}

long last_millis = 0;

long packets_received = 0;
long dmx_received = 0;

void loop() {
    auto current_millis = millis();
    if (current_millis - last_millis > 1000) {
        digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
        last_millis = current_millis;
        Serial.print("Packets Received ");
        Serial.print(packets_received);
        Serial.print(" DMX Received ");
        Serial.println(dmx_received);
        packets_received = 0;
        dmx_received = 0;
    }
    int packet_size = udp.parsePacket();

    if (packet_size >= MIN_BUFFER_ARTNET && packet_size <= MAX_BUFFER_ARTNET) {
        packets_received++;
        const auto artnet_response = RunArtnet(artnet, udp.data(), packet_size);
        if (artnet_response == ArtnetResponse::_ARTNET_POLL_REPLY) {
            udp.send(
                artnet.broadcast_address, ARTNET_PORT,
                artnet.poll_reply.data.data(), ArtnetReplySize
            );
        } else if (artnet_response == ArtnetResponse::_ARTNET_DMX) {
            dmx_received++;
            UpdateController(controller, artnet.data);
            RunController(controller);
        } else if (artnet_response == ArtnetResponse::_ARTNET_SYNC) {
            // todo: maybe only show here?
        }
    }
}
