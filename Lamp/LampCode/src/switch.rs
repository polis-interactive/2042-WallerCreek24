
use embassy_rp::gpio::{Input, Level, Output};
use embassy_time::{with_deadline, Duration, Instant, Timer};

use crate::common::{EVENT_CHANNEL, Events};

use crate::button::Debouncer;


#[embassy_executor::task]
pub async fn switch_task(mut switch: Debouncer<'static>, mut led: Output<'static>) {
  // note; switch must be a pullup

  let sender = EVENT_CHANNEL.sender();

  let mut level = switch.get_level();
  sender.send(Events::SwitchToggle(level == Level::Low)).await;

  loop {
    led.set_level(if level == Level::Low { Level::High } else { Level::Low });
    level = switch.debounce().await;
    sender.send(Events::SwitchToggle(level == Level::Low)).await;
  }
}