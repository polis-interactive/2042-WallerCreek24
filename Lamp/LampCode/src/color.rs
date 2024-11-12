
use num_integer::Integer;
use smart_leds::{hsv::{hsv2rgb, Hsv}, RGBA};

pub type RGBA8 = RGBA<u8>;

// hue value with 6 steps of warm white + 32 colors + just warm white at 0
pub const WHITE_STEPS: u8 = 6;
pub const WHITE_MUL: u8 = 51;
pub const COLOR_STEPS: u8 = 32;
pub const COLOR_MUL: u8 = 8;
// could add 1 for this, but just do exclusive checks
pub const COLOR_MAX: u8 =  COLOR_STEPS;
pub const EASE_STEP: f32 = 10.0f32;

pub fn eased_step(current: u8, target: u8, factor: f32) -> u8 {
  let delta = (target as i16) - (current as i16);
  if delta == 0 {
    return target;
  }
  let delta_abs = delta.abs() as f32;
  let step = (delta_abs / factor).max(1.0).min(delta_abs) as u8;
  if delta > 0 {
    return current.saturating_add(step);
  } else {
    return current.saturating_sub(step);
  }
}

pub trait LampColor {
  fn from_u16(&mut self, value: u8);
  fn walk_toward(&mut self, other: &RGBA<u8>);
}

impl LampColor for RGBA8 {
  fn from_u16(&mut self, value: u8) {
    if value == 0 {
      self.r = 0;
      self.g = 0;
      self.b = 0;
      self.a = 255;
      return;
    }
    // let (c, w) = (value - 1).div_rem(&WHITE_STEPS);
    let c = (value - 1).saturating_mul(COLOR_MUL);
    let rgb =  hsv2rgb(Hsv {
      hue: c,
      sat: 255,
      val: 255
    });
    self.r = rgb.r;
    self.g = rgb.g;
    self.b = rgb.b;
    self.a = 0; // (w as u8).saturating_mul(WHITE_MUL);
  }
  fn walk_toward(&mut self, other: &RGBA<u8>) {
      self.r = eased_step(self.r, other.r, EASE_STEP);
      self.g = eased_step(self.g, other.g, EASE_STEP);
      self.b = eased_step(self.b, other.b, EASE_STEP);
      self.a = eased_step(self.a, other.a, EASE_STEP);
  }
}