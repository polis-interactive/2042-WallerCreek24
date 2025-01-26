
use defmt::*;
use embassy_time::{Duration, Instant};
use smart_leds::{hsv::{hsv2rgb, Hsv}, RGBA};

pub type RGBA8 = RGBA<u8>;

// hue value with 37 colors + just warm white at 0
pub const COLOR_MAX_INCLUSIVE: u8 =  37;
// not exact spacing of color, but yeilds the best spread
pub const COLOR_MUL: u8 = 7;
// used to walk toward color; produces good results, but dependent on tick rate
pub const COLOR_EASE_STEP: f32 = 11.0f32;

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

// derived from lib8tion

pub const fn scale8(i: u8, pct: u8) -> u8 {
  (((i as u16) * (1 + pct as u16)) >> 8) as u8
}

pub fn lerp8(a: u8, b: u8, pct: u8) -> u8 {
  if b > a {
    let delta = b - a;
    let scaled = scale8(delta, pct);
    a + scaled
  } else {
    let delta = a - b;
    let scaled = scale8(delta, pct);
    a - scaled
  }
}

pub fn scale16_by_8(i: u16, pct: u8) -> u16 {
  if pct == 0 {
    0
  } else {
    (((i as u32) * (1 + pct as u32)) >> 8) as u16
  }
}

pub fn lerp16_by_8(a: u16, b: u16, pct: u8) -> u16 {
  if b > a {
    let delta = b - a;
    let scaled = scale16_by_8(delta, pct);
    a + scaled
  } else {
    let delta = a - b;
    let scaled = scale16_by_8(delta, pct);
    a - scaled
  }
}

pub fn f32_ratio_to_u8_pct(a: f32, b: f32) -> u8 {
  (255.0f32 * a as f32 / b as f32).min(255.0).max(0.0) as u8
}

pub fn duration_to_u8_pct(now: &Instant, total: &Duration) -> u8 {
  f32_ratio_to_u8_pct(now.elapsed().as_millis() as f32, total.as_millis() as f32)
}

pub trait LampColor {
  fn from_u8_white(&mut self, value: u8);
  fn from_u8(&mut self, value: u8);
  fn set_from(&mut self, other: &RGBA8);
  fn walk_toward(&mut self, other: &RGBA8);
  fn lerp_from(&mut self, other: &RGBA8, pct: u8);
  fn lerp_between(&mut self, first: &RGBA8, second: &RGBA8, pct: u8);
  fn fade_from(&mut self, other: &RGBA8, pct: u8);
  fn post_process(&mut self, other: &Self, pct: u8, with_gamma: bool, gamma: &[u8; 256]);
  #[allow(dead_code)]
  fn print_color(&self);
}

impl LampColor for RGBA8 {
  fn from_u8_white(&mut self, value: u8) {
    if value == 0 {
      self.r = 0;
      self.g = 0;
      self.b = 0;
      self.a = 255;
      return;
    }
    // let (c, w) = (value - 1).div_rem(&WHITE_STEPS);
    let c = (value - 1).wrapping_mul(COLOR_MUL);
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

  fn from_u8(&mut self, value: u8) {
    let rgb =  hsv2rgb(Hsv {
      hue: value,
      sat: 255,
      val: 255
    });
    self.r = rgb.r;
    self.g = rgb.g;
    self.b = rgb.b;
    self.a = 0;
  }

  fn set_from(&mut self, other: &RGBA8) {
    self.r = other.r;
    self.b = other.b;
    self.g = other.g;
    self.a = other.a;
  }

  fn walk_toward(&mut self, other: &Self) {
    self.r = eased_step(self.r, other.r, COLOR_EASE_STEP);
    self.g = eased_step(self.g, other.g, COLOR_EASE_STEP);
    self.b = eased_step(self.b, other.b, COLOR_EASE_STEP);
    self.a = eased_step(self.a, other.a, COLOR_EASE_STEP);
  }

  fn lerp_from(&mut self, other: &Self, pct: u8) {
    self.r = lerp8(other.r, self.r, pct);
    self.g = lerp8(other.g, self.g, pct);
    self.b = lerp8(other.b, self.b, pct);
    self.a = lerp8(other.a, self.a, pct);
  }

  fn lerp_between(&mut self, first: &RGBA8, second: &RGBA8, pct: u8) {
    self.r = lerp8(first.r, second.r, pct);
    self.g = lerp8(first.g, second.g, pct);
    self.b = lerp8(first.b, second.b, pct);
    // function is only used in wheel
    // self.a = lerp8(first.a, second.a, pct);
    self.a = 0;
  }

  fn fade_from(&mut self, other: &Self, pct: u8) {
    self.r = scale8(other.r, pct);
    self.g = scale8(other.g, pct);
    self.b = scale8(other.b, pct);
    self.a = scale8(other.a, pct);
  }

  fn post_process(&mut self, other: &Self, pct: u8, with_gamma: bool, gamma: &[u8; 256]) {
    if with_gamma {
      self.r = gamma[scale8(other.r, pct) as usize];
      self.g = gamma[scale8(other.g, pct) as usize];
      self.b = gamma[scale8(other.b, pct) as usize];
      // might need different gamma on alpha value
      self.a = gamma[scale8(other.a, pct) as usize];
    } else {
      self.r = scale8(other.r, pct);
      self.g = scale8(other.g, pct);
      self.b = scale8(other.b, pct);
      // might need different gamma on alpha value
      self.a = scale8(other.a, pct);
    }
  }


  fn print_color(&self) {
    info!("RGBA(r {:?}, g {:?}, b {:?}, a {:?})", self.r, self.g, self.b, self.a);
  }
}
