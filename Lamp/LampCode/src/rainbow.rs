
#[allow(unused_imports)]
use defmt::*;
use embassy_time::{Duration, Instant};
use num_traits::Float;

use crate::color::{duration_to_u8_pct, LampColor, RGBA8};

pub const RAINBOW_VALUE_MAX_INCLUSIVE: u8 = 11;
pub const RAINBOW_VALUE_DEFAULT: u8 = 6;

#[derive(PartialEq, Clone, Copy)]
pub struct RainbowValue(u8);

impl RainbowValue {
  fn to_f32(&self) ->f32 {
    self.0.into()
  }
}

impl From<u8> for RainbowValue {
  fn from(item: u8) -> Self {
    if item >= RAINBOW_VALUE_MAX_INCLUSIVE {
      return Self(RAINBOW_VALUE_MAX_INCLUSIVE);
    } else {
      return Self(item);
    }
  }
}

// right now, this is just our 37 colors; may change though
pub const RAINBOW_STEP_MAX_EXCLUSIVE: u8 =  64;
// not exact spacing of color, but yeilds the best spread
pub const RAINBOW_MUL: u8 = 4;
pub const RAINBOW_MUL_F32: f32 = 4.0f32;


const RAINBOW_START_STEP: u8 = 0;

struct RainbowStep(u8);

impl RainbowStep {

  fn step_next(&mut self) {
    self.0 = (self.0 + 1) % RAINBOW_STEP_MAX_EXCLUSIVE;
  }

  fn get_current_and_next(&self, idx: u8, spread: u8) -> (u8, u8) {
    let base = self.0.wrapping_mul(RAINBOW_MUL);
    let current = base.wrapping_sub(idx * spread);
    let next = base.wrapping_sub(idx * spread - RAINBOW_MUL);
    (current, next)
  }
}

impl Default for RainbowStep {
  fn default() -> Self {
      Self { 0: RAINBOW_START_STEP }
  }
}

// using ticks to omit frame rate from the calculation
pub struct Rainbow {
  last_time: Instant,
  step: RainbowStep
}

impl Rainbow {

  pub fn new() -> Self {
    Rainbow {
      last_time: Instant::now(),
      step: RainbowStep::default()
    }
  }

  fn value_to_params(value: &RainbowValue) -> (Duration, u8) {
    // duration at 0: 500ms
    // duration at 11: 50ms
    let duration_value = 750.0 - (5.0 * value.to_f32() + 1.0).log2() * 121.0;
    let duration = Duration::from_millis(duration_value.ceil() as u64);
    // spreat at 0: 2 * RAINBOW_MUL
    // spread at 11: 5 * RAINBOW_MUL
    let spread = (
      RAINBOW_MUL_F32 * (2.0 + 3.0 * value.to_f32() / 11.0)
    ).max(2.0 * RAINBOW_MUL_F32).min(RAINBOW_MUL_F32 * 5.0).ceil() as u8; 
    (
      duration,
      spread
    )
  }

  pub fn run_rainbow<const N: usize>(&mut self, data: &mut [RGBA8; N], value: &RainbowValue) {
    let mut a = RGBA8::default();
    let mut b = RGBA8::default();
    let (duration, spread) = Rainbow::value_to_params(value);
    // let mut print = false;
    if self.last_time.elapsed() >= duration {
      self.last_time = Instant::now();
      self.step.step_next();
      // print = true;
      // info!("Now on step: {:?}", self.step.0);
    }
    let pct = duration_to_u8_pct(&self.last_time, &duration);
    for (idx, led) in data.iter_mut().enumerate() {
      let (current_step, next_step) = self.step.get_current_and_next(idx as u8, spread);
      a.from_u8(current_step);
      b.from_u8(next_step);
      led.lerp_between(&a, &b, pct);
      // if next_step.wrapping_sub(current_step) != 7 {
      //   info!(
      //     "found a weird one: step: {:?}; idx: {:?}; current: {:?}; next: {:?}",
      //     self.step.0, idx, current_step, next_step
      //   );
      // }
      // if print {
      //   info!("step: {:?}; idx: {:?}; current: {:?}; next: {:?}", self.step.0, idx, current_step, next_step);
      //   print = false;
      // }
    }
  }
}