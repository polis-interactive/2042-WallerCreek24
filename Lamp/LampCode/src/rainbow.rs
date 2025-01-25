
#[allow(unused_imports)]
use defmt::*;
use embassy_time::{Duration, Instant};
use num_traits::Float;
use smart_leds::hsv::{hsv2rgb, Hsv};

use crate::color::{duration_to_u8_pct, LampColor, RGBA8};

pub const RAINBOW_VALUE_MAX_INCLUSIVE: u8 = 11;

#[derive(PartialEq, Clone, Copy)]
pub struct RainbowValue(u8);

impl RainbowValue {
  fn to_u32(&self) -> u32 {
    self.0.into()
  }
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
pub const RAINBOW_STEP_MAX_EXCLUSIVE: u8 =  37;
// not exact spacing of color, but yeilds the best spread
pub const RAINBOW_MUL: u8 = 7;

#[derive(Default)]
struct RainbowStep(u8);

impl RainbowStep {
  fn to_u8(&self) -> u8 {
    self.0 as u8
  }

  fn increment_u8(value: u8, increment: u8) -> u8 {
    let next = value + increment;
    if next >= RAINBOW_STEP_MAX_EXCLUSIVE {
      next - RAINBOW_STEP_MAX_EXCLUSIVE
    } else {
      next
    }
  }

  fn increment(&self, increment: u8) -> u8 {
    RainbowStep::increment_u8(self.0, increment)
  }
  fn decrement(&self, decrement: i8) -> u8 {
    let next = self.0 as i8 - decrement;
    if next < 0 {
      (RAINBOW_STEP_MAX_EXCLUSIVE as i8 + next) as u8
    } else {
      next as u8
    }
  }
  fn step_next(&mut self) {
    self.0 = self.increment(1);
  }
  fn get_current_and_next(&self, decrement: u8) -> (u8, u8) {
    let this = self.decrement(decrement as i8);
    let next = RainbowStep::increment_u8(this, 1);
    (this, next)
  }
}

trait RainbowColor {
  fn from_u8(&mut self, value: u8);
}

impl RainbowColor for RGBA8 {
  fn from_u8(&mut self, value: u8) {
    let c = value.wrapping_mul(RAINBOW_MUL);
    let rgb =  hsv2rgb(Hsv {
      hue: c,
      sat: 255,
      val: 255
    });
    self.r = rgb.r;
    self.g = rgb.g;
    self.b = rgb.b;
    self.a = 0;
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
    // duration at 0: 1000ms
    // duration at 11: 50ms
    let duration_value = 1000.0 - (5.0 * value.to_f32() + 1.0).log2() * 161.0;
    let duration = Duration::from_millis(duration_value.ceil() as u64);
    // spreat at 0: 1
    // spread at 11: 5
    let spread = 1; // (1.0 + 0.364 * value.to_f32()).max(1.0).min(3.0) as u8;
    (
      duration,
      spread
    )
  }

  pub fn run_rainbow<const N: usize>(&mut self, data: &mut [RGBA8; N], value: &RainbowValue) {
    let mut a = RGBA8::default();
    let mut b = RGBA8::default();
    let (duration, spread) = Rainbow::value_to_params(value);
    if self.last_time.elapsed() >= duration {
      self.last_time = Instant::now();
      self.step.step_next();
    }
    let pct = duration_to_u8_pct(&self.last_time, &duration);
    for (idx, led) in data.iter_mut().enumerate() {
      let (this_step, next_step) = self.step.get_current_and_next(
        (idx as u8).wrapping_mul(spread)
      );
      a.from_u8(this_step);
      b.from_u8(next_step);
      led.lerp_between(&a, &b, pct);
    }
  }
}