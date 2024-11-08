
use embassy_sync::{blocking_mutex::raw::CriticalSectionRawMutex, channel};

#[derive(PartialEq, Eq)]
pub enum Events {
  ButtonPress(bool),
  EncoderTurn(bool),
  SwitchToggle(bool),
  ModeTimeout,
}

pub static EVENT_CHANNEL: channel::Channel<CriticalSectionRawMutex, Events, 10> = channel::Channel::new();