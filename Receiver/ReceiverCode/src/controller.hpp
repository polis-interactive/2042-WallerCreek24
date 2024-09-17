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
byte pinList[MaxStrings] = {2, 3, 4, 5, 6, 7, 8, 9, 36, 37, 14, 15, 18, 19, 22, 23};


struct Controller {
    bool is_rgbw = false;
    int led_count = 0;
    std::vector<size_t> strings;
    std::unique_ptr<OctoWS2811> output = nullptr;
};

void setLed(
    const Controller &controller,
    const size_t led_pos,
    const CRGBW &value
) {
    size_t string = 0;
    size_t use_pos = led_pos;
    const size_t pixel_size = controller.is_rgbw ? 4 : 3;
    for (const auto &string_length : controller.strings) {
        if (use_pos < string_length) {
            const size_t start_pixel = (string * MaxString + use_pos) * pixel_size;
            display_data[start_pixel] = value.r;
            display_data[start_pixel + 1] = value.g;
            display_data[start_pixel + 2] = value.b;
            if (controller.is_rgbw) {
                display_data[start_pixel + 3] = value.w;
            }
            return;
        } else {
            use_pos -= string_length;
            string += 1;
        }
    }
}


void SetupController(Controller &controller, const Config &config) {
    auto strip_count = config.strings.size();
    controller.strings = config.strings;
    controller.is_rgbw = config.is_rgbw;
    controller.led_count = std::reduce(config.strings.begin(), config.strings.end());
    const int ws_config = (config.is_rgbw ? WS2811_GRBW : WS2811_GRB) | WS2811_800kHz;
    controller.output = std::make_unique<OctoWS2811>(
        MaxString, display_data, display_data, ws_config, strip_count, pinList
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
            Serial.println("White Chase");
            break;
        } else {
            color.w = 255;
        }
        for (int j = 0; j < MaxLeds; ++j) {
            memset(display_data, 0, sizeof(display_data));
            display_data[j * 3] = color.r;
            display_data[j * 3 + 1] = color.g;
            display_data[j * 3 + 2] = color.b;
            controller.output->show();
            delay(50);
        }
    }
    
    Serial.println("Running flash");
    for (int i = 0; i < 6; i++) {
        const uint8_t val = (i % 2) == 0 ? 255 : 0;
        memset(display_data, val, sizeof(display_data));
        controller.output->show();
        if (i != 5) {
            delay(300);
        }
    }
}

void UpdateController(Controller &controller, std::vector<uint8_t> data) {
    size_t string = 0;
    size_t pixels = 0;
    const size_t pixel_size = controller.is_rgbw ? 4 : 3;
    memset(display_data, 0, sizeof(display_data));
    for (const auto &string_length : controller.strings) {
        memcpy(
            display_data + string * MaxString * pixel_size,
            data.data() + pixels * pixel_size,
            string_length * pixel_size
        );
        pixels += string_length;
        string += 1;
    }
}

void RunController(Controller &controller) {
    controller.output->show();
}

#endif // POLIS_CONTROLLER_HPP