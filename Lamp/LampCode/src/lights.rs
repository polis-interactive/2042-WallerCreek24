use embassy_rp::{
  gpio::Output, peripherals::PIO0, pio_programs::ws2812::PioWs2812
};
use embassy_time::{Duration, Instant, Ticker};

use crate::{
  color::{scale8, LampColor, RGBA8},
  mode::{get_mode_runners, ModeRunnerTrait},
  store::{get_store, step_toward_store, update_store}
};

pub const LED_COUNT: usize = 5;


#[embassy_executor::task]
pub async fn lights_task(mut lights: PioWs2812<'static, PIO0, 1, LED_COUNT>, mut en: Output<'static>, mut en_led: Output<'static>) {
  let mut ticker = Ticker::every(Duration::from_millis(10));
  let mut data = [RGBA8::default(); LED_COUNT];
  let mut last_mode_data = [RGBA8::default(); LED_COUNT];
  let mut modes = get_mode_runners();
  let mut local_store = get_store();
  let mut mode = modes.get_mut(&local_store.mode.mode).unwrap();
  local_store.brightness = 0;
  let mut target_store = get_store();
  // as soon as we turn the lights on, write out blank to reset just in case
  en.set_high(); 
  en_led.set_high();
  set_off(&mut data);
  lights.write_rgba(&data).await;
  let start = Instant::now();
  loop {
    update_store(&mut target_store);
    if target_store != local_store {
      match step_toward_store(&target_store, &mut local_store)  {
          crate::store::ChangeType::ColorChange => {
            mode.update_mode(&local_store.color);
          },
          crate::store::ChangeType::ModeChange => {
            last_mode_data.copy_from_slice(&data);
            mode = modes.get_mut(&local_store.mode.mode).unwrap();
            mode.update_mode(&local_store.color);
          },
          _ => (),
      }
    }
    mode.run_mode(
      start.elapsed().as_millis(), 
      &mut data
    );
    if local_store.mode.pct < 255 {
      lerp_mode(local_store.mode.pct, &mut data, &last_mode_data);
    }
    post_process(&mut data, local_store.brightness);
    lights.write_rgba(&data).await;
    ticker.next().await;
  }
}

/* should i turn them off to start? */
#[allow(dead_code)]
fn set_off(data: &mut [RGBA8; LED_COUNT]) {
  for led in data.iter_mut() {
    led.r = 0;
    led.g = 0;
    led.b = 0;
    led.a = 0;
  }
}

fn lerp_mode(pct: u8, data: &mut [RGBA8; LED_COUNT], last_data: &[RGBA8; LED_COUNT]) {
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

fn post_process(data: &mut [RGBA8; LED_COUNT], brightness: u8) {
  for led in data.iter_mut() {
    led.r = GAMMA8[scale8(led.r, brightness) as usize];
    led.g = GAMMA8[scale8(led.g, brightness) as usize];
    led.b = GAMMA8[scale8(led.b, brightness) as usize];
    // might need different gamma on alpha value
    led.a = GAMMA8[scale8(led.a, brightness) as usize];
  }
}
