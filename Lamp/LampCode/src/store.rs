

use portable_atomic::{AtomicU8, AtomicU16, Ordering};

use crate::color::{eased_step, LampColor, COLOR_MAX, EASE_STEP, RGBA8}; 

const BRIGHTNESS_INCREMENT: u8 = 12;

const VALUE_MAX: u16 = 1000;

#[derive(Default, Debug)]
struct AtomicStore {
  brightness: AtomicU8,
  color: AtomicU8,
  value: AtomicU16,
}

static STORE: AtomicStore = AtomicStore {
  brightness: AtomicU8::new(192),
  color: AtomicU8::new(0),
  value: AtomicU16::new(0),
};

pub fn reset_state() {
  STORE.brightness.store(192, Ordering::Relaxed);
  STORE.color.store(0, Ordering::Relaxed);
  STORE.value.store(0, Ordering::Relaxed);
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
    color = if color > COLOR_MAX { 0 } else { color + 1 };
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

#[derive(PartialEq)]
pub struct Store {
  pub brightness: u8,
  pub color: RGBA8,
  pub value: u16
}

pub fn get_store() -> Store {
  let mut color = RGBA8 {
    r: 0,
    b: 0,
    g: 0,
    a: 0
  };
  color.from_u16(STORE.color.load(Ordering::Relaxed));
  return Store {
    brightness: STORE.brightness.load(Ordering::Relaxed),
    color: color,
    value: STORE.value.load(Ordering::Relaxed)
  }
}

pub fn update_store(store: &mut Store) {
  store.brightness = STORE.brightness.load(Ordering::Relaxed);
  store.color.from_u16(STORE.color.load(Ordering::Relaxed));
  store.value = STORE.value.load(Ordering::Relaxed);
}

pub fn step_toward_store(target_store: &Store, local_store: &mut Store) {
  if target_store.brightness != local_store.brightness {
    local_store.brightness = eased_step(local_store.brightness, target_store.brightness, EASE_STEP);
  }
  if target_store.color != local_store.color {
    local_store.color.walk_toward(&target_store.color);
  }
  if target_store.value != local_store.value {
    local_store.value = target_store.value;
  }
}