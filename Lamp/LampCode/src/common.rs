
use defmt::Format;
use embassy_sync::{blocking_mutex::raw::CriticalSectionRawMutex, channel};

#[derive(Format, PartialEq, Eq)]
pub enum Events {
  ButtonPress(bool),
  EncoderTurn(bool),
  ModeTimeout,
  SaveStore
}

pub static EVENT_CHANNEL: channel::Channel<CriticalSectionRawMutex, Events, 10> = channel::Channel::new();