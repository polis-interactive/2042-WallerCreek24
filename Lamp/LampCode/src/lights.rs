use defmt::*;
use embassy_rp::{
  gpio::Output, peripherals::PIO0, pio_programs::ws2812::PioWs2812
};
use embassy_time::{Duration, Instant, Ticker};

use crate::{
  color::{scale8, LampColor, RGBA8},
  store::{get_store, step_toward_store, update_store}, walker::WalkerIntensity
};

pub const LED_COUNT: usize = 5;


#[embassy_executor::task]
pub async fn lights_task(mut lights: PioWs2812<'static, PIO0, 1, LED_COUNT>, mut en: Output<'static>, mut en_led: Output<'static>) {
  let mut ticker = Ticker::every(Duration::from_millis(10));
  let mut data = [RGBA8::default(); LED_COUNT];
  // we keep track of last value, so on switch we can fade into the new mode without it being jarring
  let mut last_value_data = [RGBA8::default(); LED_COUNT];
  // used so we don't post_process data
  let mut output_data = [RGBA8::default(); LED_COUNT];
  let mut local_store = get_store();
  local_store.brightness = 0;
  let mut target_store = get_store();
  // reset the lights as soon as we turn them on
  en.set_high(); 
  en_led.set_high();
  set_off(&mut data);
  lights.write_rgba(&data).await;
  ticker.next().await;
  let start = Instant::now();
  loop {
    update_store(&mut target_store);
    if target_store != local_store {
      if step_toward_store(&target_store, &mut local_store) {
        // reset walker
        last_value_data.copy_from_slice(&data);
      }
    }
    run_walker(&mut data, &local_store.color, &local_store.value.intensity);
    if local_store.value.pct < 255 {
      lerp_with_last(local_store.value.pct, &mut data, &last_value_data);
    }
    // todo: maybe brightness should be an input to walker
    post_process(&mut output_data, &data, local_store.brightness);
    lights.write_rgba(&output_data).await;
    ticker.next().await;
  }
}

fn set_off(data: &mut [RGBA8; LED_COUNT]) {
  for led in data.iter_mut() {
    led.r = 0;
    led.g = 0;
    led.b = 0;
    led.a = 0;
  }
}

fn run_walker(data: &mut [RGBA8; LED_COUNT], color: &RGBA8, intensity: &WalkerIntensity) {
  let walker_value: u8 = intensity.to_u8().saturating_mul(255u8.div_ceil(5));
  for led in data.iter_mut() {
    led.r = if color.a == 0 { color.r } else { walker_value };
    led.g = color.g;
    led.b = color.b;
    led.a = if color.a == 0 { walker_value } else { color.a };
  }
}

fn lerp_with_last(pct: u8, data: &mut [RGBA8; LED_COUNT], last_data: &[RGBA8; LED_COUNT]) {
  for (current, last) in data.iter_mut().zip(last_data.iter()) {
    current.lerp_from(last, pct);
  }
}


const GAMMA8: [u8; 256] = [
  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,
  1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 4, 4,
  4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10, 10, 11, 11, 11,
  12, 12, 13, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17, 18, 18, 19, 19, 20, 20, 21, 21, 22,
  22, 23, 24, 24, 25, 25, 26, 27, 27, 28, 29, 29, 30, 31, 32, 32, 33, 34, 35, 35, 36, 37,
  38, 39, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 50, 51, 52, 54, 55, 56, 57, 58,
  59, 60, 61, 62, 63, 64, 66, 67, 68, 69, 70, 72, 73, 74, 75, 77, 78, 79, 81, 82, 83, 85,
  86, 87, 89, 90, 92, 93, 95, 96, 98, 99, 101, 102, 104, 105, 107, 109, 110, 112, 114,
  115, 117, 119, 120, 122, 124, 126, 127, 129, 131, 133, 135, 137, 138, 140, 142, 144,
  146, 148, 150, 152, 154, 156, 158, 160, 162, 164, 167, 169, 171, 173, 175, 177, 180,
  182, 184, 186, 189, 191, 193, 196, 198, 200, 203, 205, 208, 210, 213, 215, 218, 220,
  223, 225, 228, 231, 233, 236, 239, 241, 244, 247, 249, 252, 255,
];


fn post_process(output_data: &mut [RGBA8; LED_COUNT], data: &[RGBA8; LED_COUNT], brightness: u8) {
  for (out_led, led) in output_data.iter_mut().zip(data.iter()) {
    out_led.r = GAMMA8[scale8(led.r, brightness) as usize];
    out_led.g = GAMMA8[scale8(led.g, brightness) as usize];
    out_led.b = GAMMA8[scale8(led.b, brightness) as usize];
    // might need different gamma on alpha value
    out_led.a = GAMMA8[scale8(led.a, brightness) as usize];
  }
}
