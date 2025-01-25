
use defmt::Format;
use embassy_sync::{blocking_mutex::raw::CriticalSectionRawMutex, channel};

#[derive(Format, PartialEq, Eq)]
pub enum Events {
  ButtonPress(bool),
  EncoderTurn(bool),
  SaveStore
}

pub static EVENT_CHANNEL: channel::Channel<CriticalSectionRawMutex, Events, 10> = channel::Channel::new();

pub const MODE_MAX_INCLUSIVE: u8 = 2;
pub const MODE_FADE_IN_STEP: u8 = 8;

#[derive(Format, PartialEq, Eq, Clone, Copy)]
pub enum Mode {
  Lamp,
  Walker,
  Rainbow
}

// TODO: I feel like I shouldn't need these match clauses...

impl Mode {
  pub fn to_u8(&self) -> u8 {
    match self {
        Mode::Lamp => 0,
        Mode::Walker => 1,
        Mode::Rainbow => 2
    }
  }
}

impl From<u8> for Mode {
  fn from(item: u8) -> Self {
    match item {
      0 => Mode::Lamp,
      1 => Mode::Walker,
      _ => Mode::Rainbow
    }
  }
}
