

use core::ops::Range;

use embassy_executor::Spawner;
use embassy_futures::select::{select, Either};
use embassy_rp::{flash::{Async, Flash}, gpio::Output, peripherals::FLASH};
use embassy_sync::{blocking_mutex::raw::CriticalSectionRawMutex, signal};
use embassy_time::{Duration, Timer};

use crate::{
  common::{Events, EVENT_CHANNEL},
  store::{write_store, reset_state, update_brightness, update_color, update_mode}
};

// 3.5 minutes, meh
const MODE_TIMEOUT_IN_SECONDS: u64 = 60 * 3 + 30;
const SAVE_TIMEOUT_IN_MILISECONDS: u64 = 1000;

// can't get around having this defined twice... wish I knew rust better
const FLASH_SIZE: usize = 2 * 1024 * 1024;

enum ManagerStates {
  Brightness,
  Mode,
  Color
}

fn transition_manager_state(current_state: ManagerStates) -> ManagerStates {
  match current_state {
      ManagerStates::Brightness => {
        ManagerStates::Mode
      }
      ManagerStates::Mode => {
        ManagerStates::Color
      }
      ManagerStates::Color => {
        ManagerStates::Brightness
      }
  }
}

#[derive(PartialEq, Eq)]
enum ModeCommands {
  Start,
  Reset,
  Stop
}

static MODE_SIGNAL: signal::Signal<CriticalSectionRawMutex, ModeCommands> = signal::Signal::new();

#[derive(PartialEq, Eq)]
enum SaveCommands {
  Save,
}

static SAVE_SIGNAL: signal::Signal<CriticalSectionRawMutex, SaveCommands> = signal::Signal::new();

#[embassy_executor::task]
pub async fn manager_task(
  spawner: Spawner,
  mut led: Output<'static>,
  mut flash: Flash<'static, FLASH, Async, FLASH_SIZE>,
  flash_range: Range<u32>
) {
  spawner.must_spawn(mode_timeout_task());
  spawner.must_spawn(save_task());
  let receiver = EVENT_CHANNEL.receiver();
  let mut manager_state = ManagerStates::Brightness;
  let mut count = 0;
  let mut data_buffer = [0; 32];
  loop {
    let event = receiver.receive().await;
    match event {
      Events::SaveStore => {
        write_store(&mut flash, flash_range.clone(), &mut data_buffer).await;
      }
      Events::ModeTimeout => {
        manager_state = ManagerStates::Brightness;
      }
      // long press
      Events::ButtonPress(true) => {
        manager_state = ManagerStates::Brightness;
        MODE_SIGNAL.signal(ModeCommands::Stop);
        SAVE_SIGNAL.signal(SaveCommands::Save);
        reset_state();
      }
      // short press
      Events::ButtonPress(false) => {
        manager_state = transition_manager_state(manager_state);
        MODE_SIGNAL.signal(ModeCommands::Start);
      }
      // encoder turn
      Events::EncoderTurn(is_increment) => {
        MODE_SIGNAL.signal(ModeCommands::Reset);
        SAVE_SIGNAL.signal(SaveCommands::Save);
        match manager_state {
            ManagerStates::Brightness => update_brightness(is_increment),
            ManagerStates::Mode => update_mode(is_increment),
            ManagerStates::Color => update_color(is_increment),
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
async fn mode_timeout_task() {
  let sender = EVENT_CHANNEL.sender();
  loop {
    // wait for an initial signal
    let sig = MODE_SIGNAL.wait().await;
    // we don't care about resets / stops; only start the timer if we had a button push
    if sig != ModeCommands::Start {
      continue;
    }
    // afterwards, loop until we timeout; then we reset and wait for another one
    loop {
      let futures = select(
        Timer::after(Duration::from_secs(MODE_TIMEOUT_IN_SECONDS)),
        MODE_SIGNAL.wait()
      ).await;
      match futures {
        // let the manager know we timed out; go back to wating
        Either::First(_) => {
          sender.send(Events::ModeTimeout).await;
          break;
        }
        // manager already reset state; go back to waiting
        Either::Second(ModeCommands::Stop) => {
          break;
        },
        // user has taken some action; keep waiting for timeout
        _ => {
          continue;
        }
      }
    }
  }
}

#[embassy_executor::task]
async fn save_task() {
  let sender = EVENT_CHANNEL.sender();
  loop {
    // wait for an initial signal
    SAVE_SIGNAL.wait().await;
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