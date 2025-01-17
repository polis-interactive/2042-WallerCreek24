
use core::ops::Range;

use defmt::*;

use embedded_storage_async::nor_flash::MultiwriteNorFlash;
use heapless::Vec;
use portable_atomic::{AtomicU8, Ordering};
use sequential_storage::{
  cache::NoCache,
  map::{store_item, fetch_item},
  erase_all
};

use crate::{
  color::{eased_step, LampColor, COLOR_MAX, RGBA8},
  walker::{WalkerSetting, WALKER_FADE_IN_STEP, WALKER_INTENSITY_MAX}
}; 

const BRIGHTNESS_INCREMENT: u8 = 16;
const BRIGHTNESS_EASE_STEP: f32 = 16.0f32;

#[derive(Default, Debug)]
struct AtomicStore {
  brightness: AtomicU8,
  color: AtomicU8,
  value: AtomicU8,
}

impl AtomicStore {
  fn to_vec(&self) -> Vec<u8, 4> {
    let brightness = STORE.brightness.load(Ordering::Relaxed);
    let color = STORE.color.load(Ordering::Relaxed);
    let value = STORE.value.load(Ordering::Relaxed);
    let mut out = Vec::new();
    out.push(brightness).unwrap();
    out.push(color).unwrap();
    out.push(value).unwrap();
    out
  }

  fn from_bytes(&self, data: &[u8]) {
    let brightness = data[0];
    let color = data[1].max(0).min(COLOR_MAX);
    let value = data[2].max(0).min(WALKER_INTENSITY_MAX);
    self.brightness.store(brightness, Ordering::Relaxed);
    self.color.store(color, Ordering::Relaxed);
    self.value.store(value, Ordering::Relaxed);
  }
}


static STORE: AtomicStore = AtomicStore {
  brightness: AtomicU8::new(255),
  color: AtomicU8::new(0),
  value: AtomicU8::new(0),
};

pub async fn load_store<E: defmt::Format>(
  flash: &mut impl MultiwriteNorFlash<Error = E>,
  flash_range: Range<u32>,
) {

  let mut data_buffer = [0; 32];
  let fetched = fetch_item::<u8, &[u8], _>(
    flash,
    flash_range.clone(),
    &mut NoCache::new(),
    &mut data_buffer,
    &0u8,
  ).await;
  if let Ok(Some(raw_store)) = fetched {
    if raw_store.len() == 3 {
      STORE.from_bytes(raw_store);
      return;
    } else {
      warn!("Persisted store is either the wrong format or corrupted");
    }
  } else if let Err(e) = fetched {
    error!("Persisted store is corrupted: {:?}", e);
  } else {
    warn!("No data in the persisted store");
  }
  reset_state();
  let _ = erase_all(flash, flash_range.clone()).await;
}

pub async fn write_store<E: defmt::Format>(
  flash: &mut impl MultiwriteNorFlash<Error = E>,
  flash_range: Range<u32>,
  data_buffer: &mut [u8]
) {
  let to_store = STORE.to_vec();
  let stored = store_item(
    flash,
    flash_range.clone(),
    &mut NoCache::new(),
    data_buffer,
    &0u8,
    &to_store.as_slice(),
  ).await;
  if let Err(e) = stored {
    error!("Failed to persist store to disk with err: {:?}", e);
  }
}

pub fn reset_state() {
  STORE.brightness.store(255, Ordering::Relaxed);
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
  let old_color = STORE.color.load(Ordering::Relaxed);
  let new_color = if is_increment {
    if old_color >= COLOR_MAX { 0 } else { old_color + 1 }
  } else {
    if old_color == 0 { COLOR_MAX } else { old_color - 1 }
  };
  // info!("Old Color: {:?}; New Color: {:?}", old_color, new_color);
  STORE.color.store(new_color, Ordering::Relaxed);
}

pub fn update_value(is_increment: bool) {
  let old_value = STORE.value.load(Ordering::Relaxed);
  let new_value = if is_increment {
    if old_value >= WALKER_INTENSITY_MAX { 0 } else { old_value + 1 }
  } else {
    if old_value == 0 { WALKER_INTENSITY_MAX } else { old_value - 1 }
  };
  // info!("Old value: {:?}; new value: {:?}", old_value, new_value);
  STORE.value.store(new_value, Ordering::Relaxed);
}

#[derive(PartialEq)]
pub struct Store {
  pub brightness: u8,
  pub color: RGBA8,
  pub value: WalkerSetting
}

pub fn get_store() -> Store {
  let mut color = RGBA8 {
    r: 0,
    b: 0,
    g: 0,
    a: 0
  };
  color.from_u16(STORE.color.load(Ordering::Relaxed));
  let value = WalkerSetting {
    intensity: STORE.value.load(Ordering::Relaxed).into(),
    pct: 255
  };
  return Store {
    brightness: STORE.brightness.load(Ordering::Relaxed),
    color: color,
    value: value
  }
}

pub fn update_store(store: &mut Store) {
  store.brightness = STORE.brightness.load(Ordering::Relaxed);
  store.color.from_u16(STORE.color.load(Ordering::Relaxed));
  store.value.intensity = STORE.value.load(Ordering::Relaxed).into();
}

// returns true if the mode changed
pub fn step_toward_store(target_store: &Store, local_store: &mut Store) -> bool {
  if target_store.brightness != local_store.brightness {
    let new_brightness = eased_step(
      local_store.brightness, target_store.brightness, BRIGHTNESS_EASE_STEP
    );
    // info!(
    //   "Old Brightness: {:?}; New Brightness: {:?}; Target Brightness: {:?}",
    //   local_store.brightness, new_brightness, target_store.brightness
    // );
    local_store.brightness = new_brightness;
  }
  if target_store.color != local_store.color {
    // target_store.color.print_color();
    local_store.color.walk_toward(&target_store.color);
  }
  if target_store.value.intensity != local_store.value.intensity {
    local_store.value.intensity = target_store.value.intensity;
    local_store.value.pct = WALKER_FADE_IN_STEP;
    return true;
  } else if local_store.value.pct < 255 {
    local_store.value.pct = local_store.value.pct.saturating_add(WALKER_FADE_IN_STEP);
  }
  return false;
}