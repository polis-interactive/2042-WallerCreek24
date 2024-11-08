
use embassy_rp::{
  gpio::Output,
  peripherals::PIO0,
  pio_programs::rotary_encoder::{
    Direction, PioEncoder
  }
};

use crate::common::{EVENT_CHANNEL, Events};


#[embassy_executor::task]
pub async fn encoder_task(mut encoder: PioEncoder<'static, PIO0, 0>, mut led: Output<'static>) {
  let sender = EVENT_CHANNEL.sender();
  let mut count = 0;
  loop {
    count += match encoder.read().await {
      Direction::Clockwise => {
        sender.send(Events::EncoderTurn(false)).await;
        1
      },
      Direction::CounterClockwise => {
        sender.send(Events::EncoderTurn(true)).await;
        -1
      },
    };
    if count % 2 == 0 {
      led.set_low();
    } else {
      led.set_high();
    }
  }
}