

use core::ops::Range;

use embassy_executor::Spawner;
use embassy_futures::select::{select, Either};
use embassy_rp::{flash::{Async, Flash}, gpio::Output, peripherals::FLASH};
use embassy_sync::{blocking_mutex::raw::CriticalSectionRawMutex, signal};
use embassy_time::{Duration, Timer};

use crate::{
  common::{Events, Mode, EVENT_CHANNEL},
  store::{
    reset_state, load_mode, update_brightness, update_color,
    update_mode, update_rainbow_value, update_walker_value, write_store
  }
};

const SAVE_TIMEOUT_IN_MILISECONDS: u64 = 1000;

// can't get around having this defined twice... wish I knew rust better
const FLASH_SIZE: usize = 2 * 1024 * 1024;

enum ManagerStates {
  Brightness,
  Color,
  Walker,
  Rainbow
}

#[derive(PartialEq, Eq)]
enum SaveCommands {
  Save,
}

static SAVE_SIGNAL: signal::Signal<CriticalSectionRawMutex, SaveCommands> = signal::Signal::new();

fn load_manager_state() -> ManagerStates {
  match load_mode() {
    Mode::Lamp => ManagerStates::Brightness,
    Mode::Walker => ManagerStates::Walker,
    Mode::Rainbow => ManagerStates::Rainbow,
  }
}

#[embassy_executor::task]
pub async fn manager_task(
  spawner: Spawner,
  mut led: Output<'static>,
  mut flash: Flash<'static, FLASH, Async, FLASH_SIZE>,
  flash_range: Range<u32>
) {
  spawner.must_spawn(save_task());
  let receiver = EVENT_CHANNEL.receiver();
  let mut manager_state = load_manager_state();
  let mut count = 0;
  let mut data_buffer = [0; 32];
  loop {
    let event = receiver.receive().await;
    match event {
      Events::SaveStore => {
        write_store(&mut flash, flash_range.clone(), &mut data_buffer).await;
      }
      // long press
      Events::ButtonPress(true) => {
        manager_state = ManagerStates::Brightness;
        SAVE_SIGNAL.signal(SaveCommands::Save);
        reset_state();
      }
      // short press
      Events::ButtonPress(false) => {
        manager_state = match manager_state {
          ManagerStates::Brightness => {
            ManagerStates::Color
          }
          ManagerStates::Color => {
            update_mode(Mode::Walker);
            SAVE_SIGNAL.signal(SaveCommands::Save);
            ManagerStates::Walker
          }
          ManagerStates::Walker => {
            update_mode(Mode::Rainbow);
            SAVE_SIGNAL.signal(SaveCommands::Save);
            ManagerStates::Rainbow
          }
          ManagerStates::Rainbow => {
            update_mode(Mode::Lamp);
            SAVE_SIGNAL.signal(SaveCommands::Save);
            ManagerStates::Brightness
          }
        }
      }
      // encoder turn
      Events::EncoderTurn(is_increment) => {
        SAVE_SIGNAL.signal(SaveCommands::Save);
        match manager_state {
            ManagerStates::Brightness => update_brightness(is_increment),
            ManagerStates::Color => update_color(is_increment),
            ManagerStates::Walker => update_walker_value(is_increment),
            ManagerStates::Rainbow => update_rainbow_value(is_increment),
        }
      }
    }
    // used for debug
    count += 1;
    if count % 2 == 0 {
      led.set_low();
    } else {
      led.set_high();
    }
  }
}

#[embassy_executor::task]
async fn save_task() {
  let sender = EVENT_CHANNEL.sender();
  loop {
    // wait for an initial signal
    SAVE_SIGNAL.wait().await;
    // debounce the signal
    loop {
      let futures = select(
        Timer::after(Duration::from_millis(SAVE_TIMEOUT_IN_MILISECONDS)),
        SAVE_SIGNAL.wait()
      ).await;
      match futures {
        // save the state, go back to waiting
        Either::First(_) => {
          sender.send(Events::SaveStore).await;
          break;
        }
        // user has taken some action; keep waiting for timeout
        _ => {
          continue;
        }
      }
    }
  }
}