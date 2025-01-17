
use embassy_rp::gpio::{Input, Level, Output};
use embassy_time::{with_deadline, Duration, Instant, Timer};

use crate::common::{EVENT_CHANNEL, Events};


const RESET_TIMEOUT_IN_MILISECONDS: u64 = 2000;

pub struct Debouncer<'a> {
  input: Input<'a>,
  debounce: Duration,
}

impl<'a> Debouncer<'a> {
  pub fn new(input: Input<'a>, debounce: Duration) -> Self {
    Self { input, debounce }
  }

  pub async fn debounce(&mut self) -> Level {
    loop {
      let l1 = self.input.get_level();

      self.input.wait_for_any_edge().await;

      Timer::after(self.debounce).await;

      let l2 = self.input.get_level();
      if l1 != l2 {
        break l2;
      }
    }
  }

  pub fn get_level(&mut self) -> Level {
    self.input.get_level()
  }

  pub async fn wait_high(&mut self) {
    self.input.wait_for_high().await;
  }
}

#[embassy_executor::task]
pub async fn button_task(mut btn: Debouncer<'static>, mut led: Output<'static>) {
  // note; button must be a pullup

  let sender = EVENT_CHANNEL.sender();

  loop {
    // wait for the button to be let go
    btn.wait_high().await;

    // wait for a button press
    led.set_low();
    btn.debounce().await;

    let start = Instant::now();
    led.set_high();

    // check if its a long press
    match with_deadline(start + Duration::from_millis(RESET_TIMEOUT_IN_MILISECONDS), btn.debounce()).await {
      // Button released <3s
      Ok(_) => {
        sender.send(Events::ButtonPress(false)).await;
      }
      // button held for >3s
      Err(_) => {
        sender.send(Events::ButtonPress(true)).await;
      }
    }
  }
}