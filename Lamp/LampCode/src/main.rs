#![no_std]
#![no_main]

mod common;

mod button;
use button::{button_task, Debouncer};

mod switch;
use switch::switch_task;

mod encoder;
use encoder::encoder_task;

mod color;

mod store;
use store::reset_state;

mod lights;
use lights::lights_task;

mod manager;
use manager::manager_task;


use embassy_executor::Spawner;
use embassy_time::{Duration, Timer};
use embassy_rp::bind_interrupts;
use embassy_rp::gpio::{Input, Level, Output, Pull};
use embassy_rp::peripherals::{self, PIO0};
use embassy_rp::pio::{InterruptHandler, Pio};
use embassy_rp::pio_programs::ws2812::{PioWs2812, PioWs2812Program};
use embassy_rp::pio_programs::rotary_encoder::{PioEncoder, PioEncoderProgram};
use embassy_rp::watchdog::Watchdog;

use {defmt_rtt as _, panic_probe as _};


use assign_resources::assign_resources;

assign_resources! {
  encoder: EncoderResources {
    a_pin: PIN_0,
    b_pin: PIN_1,
    led_pin: PIN_2  
  }
  button: ButtonResources {
    pin: PIN_3,
    led_pin: PIN_4
  }
  manager: ManagerResources {
    led_pin: PIN_25
  }
  switch: SwitchResources {
    pin: PIN_14,
    led_pin: PIN_13
  }
  led: LedResources {
    led_pin: PIN_15,
    dma_chan: DMA_CH0,
  }
}

bind_interrupts!(struct Irqs {
  PIO0_IRQ_0 => InterruptHandler<PIO0>;
});

#[embassy_executor::main]
async fn main(spawner: Spawner) {
  let p = embassy_rp::init(Default::default());
  let r = split_resources! {p};

  let mut watchdog = Watchdog::new(p.WATCHDOG);
  watchdog.start(Duration::from_millis(1_050));

  // initialize state
  reset_state();

  // intialize switch
  let switch = Debouncer::new(Input::new(r.switch.pin, Pull::Up), Duration::from_millis(20));  
  let btn_led = Output::new(r.switch.led_pin, Level::Low);
  spawner.must_spawn(switch_task(switch, btn_led));

  // initialize button
  let btn = Debouncer::new(Input::new(r.button.pin, Pull::Up), Duration::from_millis(20));  
  let btn_led = Output::new(r.button.led_pin, Level::Low);
  spawner.must_spawn(button_task(btn, btn_led));

  // encoder / leds
  
  let Pio {
    mut common, sm0, sm1, ..
  } = Pio::new(p.PIO0, Irqs);

  // initialize encoder
  let enc_prg = PioEncoderProgram::new(&mut common);
  let enc = PioEncoder::new(&mut common, sm0, r.encoder.a_pin, r.encoder.b_pin, &enc_prg);
  let enc_led = Output::new(r.encoder.led_pin, Level::Low);
  spawner.must_spawn(encoder_task(enc, enc_led));

  // initialize leds
  let lts_prg = PioWs2812Program::new(&mut common);
  let lts = PioWs2812::new(&mut common, sm1, r.led.dma_chan, r.led.led_pin, &lts_prg);
  spawner.must_spawn(lights_task(lts));

  // initialize state
  let mng_led = Output::new(r.manager.led_pin, Level::Low);
  spawner.must_spawn(manager_task(spawner, mng_led));

  // run watchdog
  loop {
    Timer::after_secs(1).await;
    watchdog.feed();
  }
}