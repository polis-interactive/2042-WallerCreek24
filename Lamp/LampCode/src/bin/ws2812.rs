
#![no_std]
#![no_main]

use defmt::*;
use embassy_executor::Spawner;
use embassy_rp::bind_interrupts;
use embassy_rp::peripherals::PIO0;
use embassy_rp::pio::{InterruptHandler, Pio};
use embassy_rp::pio_programs::ws2812::{PioWs2812, PioWs2812Program};
use embassy_time::{Duration, Ticker};
use smart_leds::RGBA;
use {defmt_rtt as _, panic_probe as _};
use libm::{sin, floor};


pub type RGBA8 = RGBA<u8>;

bind_interrupts!(struct Irqs {
    PIO0_IRQ_0 => InterruptHandler<PIO0>;
});

/// Input a value 0 to 255 to get a color value
/// The colours are a transition r - g - b - back to r.
fn wheel(mut wheel_pos: u8, white_pos: u8) -> RGBA8 {
    wheel_pos = 255 - wheel_pos;
    if wheel_pos < 85 {
        return (255 - wheel_pos * 3, 0, wheel_pos * 3, white_pos).into();
    }
    if wheel_pos < 170 {
        wheel_pos -= 85;
        return (0, wheel_pos * 3, 255 - wheel_pos * 3, white_pos).into();
    }
    wheel_pos -= 170;
    (wheel_pos * 3, 255 - wheel_pos * 3, 0, white_pos).into()
}

#[embassy_executor::main]
async fn main(_spawner: Spawner) {
    info!("Start");
    let p = embassy_rp::init(Default::default());

    let Pio { mut common, sm0, .. } = Pio::new(p.PIO0, Irqs);

    // This is the number of leds in the string. Helpfully, the sparkfun thing plus and adafruit
    // feather boards for the 2040 both have one built in.
    const NUM_LEDS: usize = 5;
    let mut data = [RGBA8::default(); NUM_LEDS];

    // Common neopixel pins:
    // Thing plus: 8
    // Adafruit Feather: 16;  Adafruit Feather+RFM95: 4
    let program = PioWs2812Program::new(&mut common);
    let mut ws2812 = PioWs2812::new(&mut common, sm0, p.DMA_CH0, p.PIN_15, &program);

    // Loop forever making RGB values and pushing them out to the WS2812.
    let mut ticker = Ticker::every(Duration::from_millis(10));

    const TWO_PI: f64 = f64::from_bits(0x400921fb000FD5DD);
    let mut w;

    loop {
        for j in 0..(256 * 5) {
            // w  = 191 + sin(Fix::from_num(j) / two_pi).wrapping_mul(Fix::from_num(64)).to_num::<u8>();
            w = (128i32 + floor(sin((j as f64) / TWO_PI / 20f64) * 127f64) as i32) as u8;
            debug!("New Colors:");
            for i in 0..NUM_LEDS {
                data[i] = wheel((((i * 256) as u16 / NUM_LEDS as u16 + j as u16) & 255) as u8, w);
                debug!("R: {} G: {} B: {} A: {}", data[i].r, data[i].g, data[i].b, data[i].a);
            }
            ws2812.write_rgba(&data).await;

            ticker.next().await;
        }
    }
}
