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
    std::unique_ptr<OctoWS2811> output = nullptr;
};

void RunController(Controller &controller);


void SetupController(Controller &controller, const Config &config) {
    controller.is_rgbw = config.is_rgbw;
    const auto led_pixels = controller.is_rgbw ? 4 : 3;
    // setting doesn't matter, just know what you're doing?
    const int ws_config = (config.is_rgbw ? WS2811_GRBW : WS2811_GRB) | WS2811_800kHz;
    controller.output = std::make_unique<OctoWS2811>(
        MaxString, display_data, display_data, ws_config, MaxStrings, pinList
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
        for (int j = 0; j < MaxLeds; ++j) {
            memset(display_data, 0, sizeof(display_data));
            display_data[j * led_pixels] = color.r;
            display_data[j * led_pixels + 1] = color.g;
            display_data[j * led_pixels + 2] = color.b;
            if (controller.is_rgbw) {
                display_data[j * led_pixels + 3] = color.w;
            }
            RunController(controller);
            delay(50);
        }
    }
    
    Serial.println("Running flash");
    for (int i = 0; i < 6; i++) {
        const uint8_t val = (i % 2) == 0 ? 128 : 0;
        memset(display_data, val, sizeof(display_data));
        RunController(controller);
        if (i != 5) {
            delay(300);
        }
    }
}

static void swapBytes() {
    uint8_t r, g, b, w;
    for (size_t i = 0; i < sizeof(display_data); i += 4) {
        // Assuming data is in r, g, b, w order:
        // Swap in-place to convert to w, r, g, b order
        // r = display_data[i];     // Save r
        g = display_data[i + 1]; // Save g
        b = display_data[i + 2]; // Save b
        // w = display_data[i + 3]; // Save w

        // Rearrange to w, r, g, b
        // display_data[i] = r;
        display_data[i + 1] = b;
        display_data[i + 2] = g;
        // display_data[i + 3] = w;
    }
}

void UpdateController(Controller &controller, std::vector<uint8_t> data) {
    memset(display_data, 0, sizeof(display_data));
    memcpy(display_data, data.data(), data.size());
    if (controller.is_rgbw) {
      swapBytes();
    }
}

void RunController(Controller &controller) {
    controller.output->show();
}

#endif // POLIS_CONTROLLER_HPP