use embassy_rp::{
  peripherals::PIO0,
  pio_programs::ws2812::PioWs2812
};
use embassy_time::{Duration, Ticker};

use crate::{
  color::RGBA8,
  store::{get_store, update_store, Store, step_toward_store}
};

pub const LED_COUNT: usize = 5;


#[embassy_executor::task]
pub async fn lights_task(mut lights: PioWs2812<'static, PIO0, 1, LED_COUNT>) {
  let mut ticker = Ticker::every(Duration::from_millis(10));
  let mut data = [RGBA8::default(); LED_COUNT];
  let mut local_store = get_store();
  local_store.is_on = true;
  let mut target_store = get_store();
  loop {
    update_store(&mut target_store);
    if !target_store.is_on {
      set_off(&mut data);
    } else {
      if target_store != local_store {
        step_toward_store(&target_store, &mut local_store);
      }
      set_from_store(&local_store, &mut data);
    }
    lights.write_rgba(&data).await;
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

// Generate the gamma correction table at compile time

const fn scale8(i: u8, scale: u8) -> u8 {
  (((i as u16) * (1 + scale as u16)) >> 8) as u8
}


fn set_from_store(store: &Store, data: &mut [RGBA8; LED_COUNT]) {
  for led in data.iter_mut() {
    led.r = GAMMA8[scale8(store.color.r, store.brightness) as usize];
    led.g = GAMMA8[scale8(store.color.g, store.brightness) as usize];
    led.b = GAMMA8[scale8(store.color.b, store.brightness) as usize];
    // might need different gamma on alpha value
    led.a = GAMMA8[scale8(store.color.a, store.brightness) as usize];
  }
}