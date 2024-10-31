
use portable_atomic::{AtomicU8, AtomicU16, Ordering};

const BRIGHTNESS_INCREMENT: u8 = 8;

// hue value with 6 steps of warm white + 32 colors + just warm white at 0
pub const WHITE_STEPS: u16 = 4;
pub const WHITE_MUL: u16 = 64;
pub const COLOR_STEPS: u16 = 16;
pub const COLOR_MUL: u16 = 16;
const COLOR_MAX: u16 = WHITE_STEPS * COLOR_STEPS;

const VALUE_MAX: u16 = 1000;

#[derive(Default, Debug)]
struct AtomicStore {
  brightness: AtomicU8,
  color: AtomicU16,
  value: AtomicU16,
}

pub struct Store {
  pub brightness: u8,
  pub color: u16,
  pub value: u16
}

static STORE: AtomicStore = AtomicStore {
  brightness: AtomicU8::new(192),
  color: AtomicU16::new(0),
  value: AtomicU16::new(0),
};

pub fn reset_state() {
  STORE.brightness.store(192, Ordering::Relaxed);
  STORE.color.store(0, Ordering::Relaxed);
  STORE.value.store(0, Ordering::Relaxed);
}

pub fn get_store() -> Store {
  return Store {
    brightness: STORE.brightness.load(Ordering::Relaxed),
    color: STORE.color.load(Ordering::Relaxed),
    value: STORE.value.load(Ordering::Relaxed)
  }
}

pub fn update_store(store: &mut Store) {
  store.brightness = STORE.brightness.load(Ordering::Relaxed);
  store.color = STORE.color.load(Ordering::Relaxed);
  store.value = STORE.value.load(Ordering::Relaxed);
}

pub fn update_brightness(is_increment: bool) {
  let mut brightness = STORE.brightness.load(Ordering::Relaxed);
  brightness = if is_increment { 
    brightness.saturating_add(BRIGHTNESS_INCREMENT) 
  } else {  
    brightness.saturating_sub(BRIGHTNESS_INCREMENT)
  };
  STORE.brightness.store(brightness, Ordering::Relaxed);
}

pub fn update_color(is_increment: bool) {
  let mut color = STORE.color.load(Ordering::Relaxed);
  if is_increment {
    color = if color >= COLOR_MAX { 0 } else { color + 1 };
  } else {
    color = if color == 0 { COLOR_MAX } else { color - 1 };
  }
  STORE.color.store(color, Ordering::Relaxed);
}

pub fn update_value(is_increment: bool) {
  let mut value = STORE.value.load(Ordering::Relaxed);
  if is_increment {
    value = if value >= VALUE_MAX { 0 } else { value + 1 };
  } else {
    value = if value == 0 { VALUE_MAX - 1 } else { value - 1 };
  }
  STORE.value.store(value, Ordering::Relaxed);
}