//! This example shows the ease of debouncing a button with async rust.
//! Hook up a button or switch between pin 9 and ground.

#![no_std]
#![no_main]

use defmt::info;
use embassy_executor::Spawner;
use embassy_rp::gpio::{Input, Level, Pull, Output};
use embassy_time::{with_deadline, Duration, Instant, Timer};
use {defmt_rtt as _, panic_probe as _};

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

  pub async fn wait_high(&mut self) {
    self.input.wait_for_high().await;
  }

  pub async fn wait_low(&mut self) {
    self.input.wait_for_low().await;
  }
}

#[embassy_executor::main]
async fn main(_spawner: Spawner) {
    let p = embassy_rp::init(Default::default());
    let mut btn = Debouncer::new(Input::new(p.PIN_3, Pull::Up), Duration::from_millis(20));
    
    let mut led = Output::new(p.PIN_25, Level::Low);

    info!("Debounce Demo");

    // using a pullup
    btn.wait_high().await;

    loop {
      
      led.set_low();
        
        // button pressed
        btn.debounce().await;
        let start = Instant::now();
        info!("Button Press");
        led.set_high();

        match with_deadline(start + Duration::from_secs(3), btn.debounce()).await {
            // Button released <5s
            Ok(_) => {
                info!("Button pressed for: {}ms", start.elapsed().as_millis());
                continue;
            }
            // button held for > >5s
            Err(_) => {
              led.set_low();
              Timer::after(Duration::from_millis(100)).await;
              led.set_high();
              Timer::after(Duration::from_millis(100)).await;
              led.set_low();
              Timer::after(Duration::from_millis(100)).await;
              led.set_high();
              Timer::after(Duration::from_millis(100)).await;
              led.set_low();
              Timer::after(Duration::from_millis(100)).await;
              led.set_high();
              Timer::after(Duration::from_millis(100)).await;
              led.set_low();
              Timer::after(Duration::from_millis(100)).await;
              led.set_high();
              Timer::after(Duration::from_millis(100)).await;
            }
        }

        // wait for button release before handling another press
        btn.wait_high().await;
        info!("Button pressed for: {}ms", start.elapsed().as_millis());
    }
}