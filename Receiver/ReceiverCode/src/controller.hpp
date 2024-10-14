#ifndef POLIS_CONTROLLER_HPP
#define POLIS_CONTROLLER_HPP

#include <Arduino.h>


#include <OctoWS2811.h>

#include <numeric>
#include <memory>

#include "config.hpp"
#include "color.hpp"

// size it for rgbw, even if we don't need the space
DMAMEM uint8_t display_data[MaxLeds * 4];
byte pinList[MaxStrings] = {
    // set 1
    33, 34, 35, 36, 37, 38, 39, 40, 
    // set 2
    14, 15, 16, 17, 18, 19, 20, 21,
    // set 3, other side, wonky
     7,  6,  5,  4,  3,  0,  1,  2,
};


struct Controller {
    bool is_rgbw = false;
    int led_count = 0;
    int pixel_count = 0;
    int strings = 0;
    std::unique_ptr<OctoWS2811> output = nullptr;
};


void SetupController(Controller &controller, const Config &config) {
    controller.strings = config.strings;
    controller.is_rgbw = config.is_rgbw;
    controller.led_count = config.strings * MaxString;
    const auto led_pixels = controller.is_rgbw ? 4 : 3;
    controller.pixel_count = led_pixels * controller.led_count;
    const int ws_config = (config.is_rgbw ? WS2811_GRBW : WS2811_GRB) | WS2811_800kHz;
    controller.output = std::make_unique<OctoWS2811>(
        MaxString, display_data, display_data, ws_config, config.strings, pinList
    );
    controller.output->begin();
    controller.output->show();
    Serial.println("Running Chase");
    for (int i = 0; i < 4; i++) {
        CRGBW color;
        if (i == 0) {
            Serial.println("Red Chase");
            color.r = 255;
        } else if (i == 1) {
            Serial.println("Green Chase");
            color.g = 255;
        } else if (i == 2) {
            Serial.println("Blue Chase");
            color.b = 255;
        } else if (!controller.is_rgbw) {
            break;
        } else {
            Serial.println("White Chase");
            color.w = 255;
        }
        for (int j = 0; j < controller.led_count; ++j) {
            memset(display_data, 0, sizeof(display_data));
            display_data[j * led_pixels] = color.r;
            display_data[j * led_pixels + 1] = color.g;
            display_data[j * led_pixels + 2] = color.b;
            if (controller.is_rgbw) {
                display_data[j * led_pixels + 3] = color.w;
            }
            controller.output->show();
            delay(50);
        }
    }
    
    Serial.println("Running flash");
    for (int i = 0; i < 6; i++) {
        const uint8_t val = (i % 2) == 0 ? 128 : 0;
        memset(display_data, val, sizeof(display_data));
        controller.output->show();
        if (i != 5) {
            delay(300);
        }
    }
}

void UpdateController(Controller &controller, std::vector<uint8_t> data) {
    memset(display_data, 0, sizeof(display_data));
    memcpy(display_data, data.data(), controller.pixel_count);
}

void RunController(Controller &controller) {
    controller.output->show();
}

#endif // POLIS_CONTROLLER_HPP