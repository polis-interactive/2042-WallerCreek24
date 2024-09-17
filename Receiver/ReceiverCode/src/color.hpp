
#ifndef FastLED_RGBW_h
#define FastLED_RGBW_h

#include <Arduino.h>

struct CRGB  {
	union {
		struct {
			union {
				uint8_t g;
				uint8_t green;
			};
			union {
				uint8_t r;
				uint8_t red;
			};
			union {
				uint8_t b;
				uint8_t blue;
			};
		};
		uint8_t raw[3];
	};
	CRGB(){}
	CRGB(uint8_t rd, uint8_t grn, uint8_t blu){
		r = rd;
		g = grn;
		b = blu;
	}
};

struct CRGBW  {
	union {
		struct {
			union {
				uint8_t g;
				uint8_t green;
			};
			union {
				uint8_t r;
				uint8_t red;
			};
			union {
				uint8_t b;
				uint8_t blue;
			};
			union {
				uint8_t w;
				uint8_t white;
			};
		};
		uint8_t raw[4];
	};
	CRGBW(){
        r = 0;
        g = 0;
        b = 0;
        w = 0;
    }
	CRGBW(uint8_t rd, uint8_t grn, uint8_t blu, uint8_t wht){
		r = rd;
		g = grn;
		b = blu;
		w = wht;
	}
};

#endif