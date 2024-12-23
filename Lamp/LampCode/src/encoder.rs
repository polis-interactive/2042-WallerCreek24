
use embassy_rp::{
  gpio::Output,
  peripherals::PIO0,
  pio_programs::rotary_encoder::{
    Direction, PioEncoder
  }
};

use crate::common::{EVENT_CHANNEL, Events};


#[embassy_executor::task]
pub async fn encoder_task(mut encoder: PioEncoder<'static, PIO0, 0>, mut led: Output<'static>, flip_direction: bool) {
  let sender = EVENT_CHANNEL.sender();
  let mut count = 0;
  let cc_wise = if flip_direction { -1 } else { 1 };
  let c_wise = cc_wise * -1;
  loop {
    count += match encoder.read().await {
      Direction::CounterClockwise => {
        sender.send(Events::EncoderTurn(flip_direction)).await;
        cc_wise
      },
      Direction::Clockwise => {
        sender.send(Events::EncoderTurn(!flip_direction)).await;
        c_wise
      },
    };
    if count % 2 == 0 {
      led.set_low();
    } else {
      led.set_high();
    }
  }
}