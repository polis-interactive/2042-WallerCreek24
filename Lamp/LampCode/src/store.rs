
use core::ops::Range;

use defmt::*;

use embedded_storage_async::nor_flash::MultiwriteNorFlash;
use heapless::Vec;
use portable_atomic::{AtomicU8, AtomicU16, Ordering};
use sequential_storage::{cache::NoCache, map::{store_item, fetch_item}};

use crate::{
  color::{eased_step, LampColor, COLOR_MAX, EASE_STEP, RGBA8},
  mode::{ModeSet, MODE_MAX, MODE_STEP}
}; 


const BRIGHTNESS_INCREMENT: u8 = 12;

#[derive(Default, Debug)]
struct AtomicStore {
  brightness: AtomicU8,
  color: AtomicU8,
  mode: AtomicU16,
}

impl AtomicStore {
  fn to_vec(&self) -> Vec<u8, 4> {
    let brightness = STORE.brightness.load(Ordering::Relaxed);
    let color = STORE.color.load(Ordering::Relaxed);
    let mode = STORE.mode.load(Ordering::Relaxed);
    let mode_bytes: [u8; 2] = mode.to_le_bytes();
    let mut out = Vec::new();
    out.push(brightness).unwrap();
    out.push(color).unwrap();
    out.push(mode_bytes[0]).unwrap();
    out.push(mode_bytes[1]).unwrap();
    out
  }

  fn from_bytes(&self, data: &[u8]) {
    let brightness = data[0];
    let color = data[1];
    let mode = u16::from_le_bytes([data[2], data[3]]);
    self.brightness.store(brightness, Ordering::Relaxed);
    self.color.store(color, Ordering::Relaxed);
    self.mode.store(mode, Ordering::Relaxed);
  }
}


static STORE: AtomicStore = AtomicStore {
  brightness: AtomicU8::new(192),
  color: AtomicU8::new(0),
  mode: AtomicU16::new(0),
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
    if raw_store.len() == 4 {
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
  // this kept crashing my program? somehow? even when I do an early return?
  // let _ = sequential_storage::erase_all(flash, flash_range.clone()).await;
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
  STORE.mode.store(0, Ordering::Relaxed);
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

pub fn update_mode(is_increment: bool) {
  let mut mode = STORE.mode.load(Ordering::Relaxed);
  if is_increment {
    mode = if mode >= MODE_MAX { 0 } else { mode + 1 };
  } else {
    mode = if mode == 0 { MODE_MAX } else { mode - 1 };
  }
  STORE.mode.store(mode, Ordering::Relaxed);
}

#[derive(PartialEq)]
pub struct Store {
  pub brightness: u8,
  pub color: RGBA8,
  pub mode: ModeSet
}

pub fn get_store() -> Store {
  let mut color = RGBA8 {
    r: 0,
    b: 0,
    g: 0,
    a: 0
  };
  color.from_u16(STORE.color.load(Ordering::Relaxed));
  let mode = ModeSet {
    mode: STORE.mode.load(Ordering::Relaxed).into(),
    pct: 255
  };
  return Store {
    brightness: STORE.brightness.load(Ordering::Relaxed),
    color,
    mode
  }
}

pub fn update_store(store: &mut Store) {
  store.brightness = STORE.brightness.load(Ordering::Relaxed);
  store.color.from_u16(STORE.color.load(Ordering::Relaxed));
  store.mode.mode =  STORE.mode.load(Ordering::Relaxed).into();
}

#[derive(PartialEq, Eq)]
pub enum ChangeType {
  ColorChange,
  ModeChange,
  NonTrackedChange
}

pub fn step_toward_store(target_store: &Store, local_store: &mut Store) -> ChangeType {
  let mut change_type = ChangeType::NonTrackedChange;
  if target_store.brightness != local_store.brightness {
    local_store.brightness = eased_step(local_store.brightness, target_store.brightness, EASE_STEP);
  }
  if target_store.color != local_store.color {
    local_store.color.walk_toward(&target_store.color);
    change_type = ChangeType::ColorChange;
  }
  if target_store.mode.mode != local_store.mode.mode {
    local_store.mode.mode = target_store.mode.mode;
    local_store.mode.pct = MODE_STEP;
    change_type = ChangeType::ModeChange;
  } else if local_store.mode.pct < 255 {
    local_store.mode.pct = local_store.mode.pct.saturating_add(MODE_STEP);
  }
  change_type
}