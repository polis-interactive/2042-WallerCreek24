
#ifndef POLIS_ARTNET_HPP
#define POLIS_ARTNET_HPP

#include <Arduino.h>

#include <QNEthernet.h>


#include "config.hpp"

#define ARTNET_POLL 0x2000
#define ARTNET_POLL_REPLY 0x2100
#define ARTNET_DMX 0x5000
#define ARTNET_SYNC 0x5200

#define MIN_BUFFER_ARTNET 10
#define MAX_BUFFER_ARTNET 530


const size_t ArtnetDmxStart = 18;

const std::string ArtnetID = "Art-Net\0";


namespace qn = qindesign::network;

struct ArtnetReply {
  uint8_t  id[8];
  uint16_t opCode;
  uint8_t  ip[4];
  uint16_t port;
  uint8_t  verH;
  uint8_t  ver;
  uint8_t  subH;
  uint8_t  sub;
  uint8_t  oemH;
  uint8_t  oem;
  uint8_t  ubea;
  uint8_t  status;
  uint8_t  etsaman[2];
  uint8_t  shortname[18];
  uint8_t  longname[64];
  uint8_t  nodereport[64];
  uint8_t  numbportsH;
  uint8_t  numbports;
  uint8_t  porttypes[4];//max of 4 ports per node
  uint8_t  goodinput[4];
  uint8_t  goodoutput[4];
  uint8_t  swin[4];
  uint8_t  swout[4];
  uint8_t  swvideo;
  uint8_t  swmacro;
  uint8_t  swremote;
  uint8_t  sp1;
  uint8_t  sp2;
  uint8_t  sp3;
  uint8_t  style;
  uint8_t  mac[6];
  uint8_t  bindip[4];
  uint8_t  bindindex;
  uint8_t  status2;
  uint8_t  filler[26];
} __attribute__((packed));

constexpr size_t ArtnetReplySize = sizeof(ArtnetReply);

union ArtnetReplyUnion {
    ArtnetReply reply;
    std::array<uint8_t, ArtnetReplySize> data;
};

struct Artnet {
    uint16_t universe;
    IPAddress local_address;
    IPAddress broadcast_address;
    uint8_t sequence;
    std::vector<uint8_t> data;
    ArtnetReplyUnion poll_reply;
};

void SetupArtnet(Artnet &artnet, const Config &config) {
    artnet.universe = config.universe;
    artnet.data = std::vector<uint8_t>(config.channels, 0);
    artnet.local_address = qn::Ethernet.localIP();
    artnet.broadcast_address = artnet.local_address;
    artnet.broadcast_address[0] = 255;
    artnet.broadcast_address[1] = 255;
    artnet.broadcast_address[2] = 255;
    artnet.broadcast_address[3] = 255;

    // setup poll reply so we don't need to later

    auto &artnet_reply = artnet.poll_reply.reply;
    memcpy(artnet_reply.id, ArtnetID.c_str(), ArtnetID.size());

    // kinda annoying I can't get a ptr but w.e
    artnet_reply.ip[0] = artnet.local_address[0];
    artnet_reply.ip[1] = artnet.local_address[1];
    artnet_reply.ip[2] = artnet.local_address[2];
    artnet_reply.ip[3] = artnet.local_address[3];

    artnet_reply.opCode = ARTNET_POLL_REPLY;
    artnet_reply.port =  ARTNET_PORT;

    // why memset though
    memset(artnet_reply.goodinput,  0x08, 4);
    memset(artnet_reply.goodoutput,  0x80, 4);
    memset(artnet_reply.porttypes,  0xc0, 4);

    const std::string short_name = "Teensy Artnet";
    std::stringstream ss;
    ss << "Teensy " << qn::Ethernet.macAddress();
    const auto long_name = ss.str();
    memcpy(artnet_reply.shortname, short_name.c_str(), short_name.size());
    memcpy(artnet_reply.longname, long_name.c_str(), long_name.size());

    artnet_reply.etsaman[0] = 0;
    artnet_reply.etsaman[1] = 0;
    artnet_reply.verH       = 1;
    artnet_reply.ver        = 0;
    artnet_reply.subH       = 0;
    artnet_reply.sub        = 0;
    artnet_reply.oemH       = 0;
    artnet_reply.oem        = 0xFF;
    artnet_reply.ubea       = 0;
    artnet_reply.status     = 0xd2;
    artnet_reply.swvideo    = 0;
    artnet_reply.swmacro    = 0;
    artnet_reply.swremote   = 0;
    artnet_reply.style      = 0;

    artnet_reply.numbportsH = 0;
    artnet_reply.numbports  = 1;
    artnet_reply.status2    = 0x08;

    artnet_reply.bindip[0] = artnet.local_address[0];
    artnet_reply.bindip[1] = artnet.local_address[1];
    artnet_reply.bindip[2] = artnet.local_address[2];
    artnet_reply.bindip[3] = artnet.local_address[3];

    artnet_reply.swin[0] = artnet.universe;

    ss.clear();
    ss << "Accepting DMX for Universe " << artnet.universe;
    const auto report = ss.str();
    memcpy(artnet_reply.nodereport, report.c_str(), report.size());

}

enum class ArtnetResponse {
    _ARTNET_POLL_REPLY,
    _ARTNET_SYNC,
    _ARTNET_DMX,
    _ARTNET_NONE
};

ArtnetResponse RunArtnet(Artnet &artnet, const uint8_t* data, const size_t data_size) {

    auto response = ArtnetResponse::_ARTNET_NONE;

    if (strncmp((const char *) data, ArtnetID.c_str(), ArtnetID.size()) != 0) {
        Serial.println("Received non artnet packet");
        return response;
    }

    const uint16_t op_code = data[8] | data[9] << 8;
    uint16_t universe, dmx_length;
    uint8_t sequence;

    switch(op_code) {
        case ARTNET_DMX:
            if (data_size < ArtnetDmxStart) {
                Serial.println("DMX packet too small to contain full header");
                break;
            }
            universe = data[14] | data[15] << 8;
            if (universe != artnet.universe) {
                break;
            }
            // sequence = data[12];
            // if (!isIncreasing(artnet.sequence, sequence)) {
            //     Serial.println("non increasing");
            //     break;
            // }
            dmx_length = data[17] | data[16] << 8;
            if (dmx_length < artnet.data.size()) {
                Serial.println("Not enough dmx data to write");
                break;
            } else if (data_size < dmx_length + ArtnetDmxStart) {
                Serial.println("DMX payload truncated or corrupeted");
                break;
            }
            memcpy(artnet.data.data(), data + ArtnetDmxStart, artnet.data.size());
            response = ArtnetResponse::_ARTNET_DMX;
            break;
        case ARTNET_POLL:
            response = ArtnetResponse::_ARTNET_POLL_REPLY;
            break;
        case ARTNET_SYNC:
            response = ArtnetResponse::_ARTNET_SYNC;
            break;
        case ARTNET_POLL_REPLY:
            break;
        default:
            Serial.print("Unhandled ArtNet OpCode: ");
            Serial.println(op_code);
            break;
    }

    return response;

}

#endif // POLIS_ARTNET_HPP