#![no_std]
#![no_main]

use embassy_rp::flash::{Async, Flash, ERASE_SIZE};
use lamp::{load_store, Debouncer, button_task, encoder_task, lights_task, manager_task};

use defmt::*;

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
  boot: BootResources {
    led_pin: PIN_24
  }
  encoder: EncoderResources {
    a_pin: PIN_10,
    b_pin: PIN_11,
    led_pin: PIN_4  
  }
  button: ButtonResources {
    pin: PIN_6,
    led_pin: PIN_5
  }
  manager: ManagerResources {
    led_pin: PIN_3
  }
  led: LedResources {
    data_pin: PIN_12,
    dma_chan: DMA_CH1,
    en_pin: PIN_17,
    en_led_pin: PIN_2
  },
  flash: FlashResources {
    dma_chan: DMA_CH0
  }
}

bind_interrupts!(struct Irqs {
  PIO0_IRQ_0 => InterruptHandler<PIO0>;
});

const ADDR_OFFSET: u32 = 0x100000;
const FLASH_SIZE: u32 = 2 * 1024 * 1024;
const _FLASH_SIZE: usize = FLASH_SIZE as usize;

#[embassy_executor::main]
async fn main(spawner: Spawner) {

  info!("Running main");

  
  info!("Initializing embassy");

  let p = embassy_rp::init(Default::default());
  let r = split_resources! {p};

  info!("Showing signs of life");

  let mut boot_led = Output::new(r.boot.led_pin, Level::High);
  
  info!("Start Watchdog");

  let mut watchdog = Watchdog::new(p.WATCHDOG);
  watchdog.start(Duration::from_millis(2_000));


  info!("Initialize State");

  let mut flash = Flash::<_, Async, _FLASH_SIZE>::new(p.FLASH, r.flash.dma_chan);
  let flash_range_start = (flash.capacity() - 4 as usize * ERASE_SIZE) as u32;
  let flash_range_end = flash.capacity() as u32;
  let map_flash_range = flash_range_start..flash_range_end;
  load_store(&mut flash, map_flash_range.clone()).await;

  info!("Initialize, start up button");

  let btn = Debouncer::new(Input::new(r.button.pin, Pull::Up), Duration::from_millis(20));  
  let btn_led = Output::new(r.button.led_pin, Level::Low);
  spawner.must_spawn(button_task(btn, btn_led));

  // encoder / leds
  
  let Pio {
    mut common, sm0, sm1, ..
  } = Pio::new(p.PIO0, Irqs);

  info!("Initialize, start up encoder");

  let enc_prg = PioEncoderProgram::new(&mut common);
  let enc = PioEncoder::new(&mut common, sm0, r.encoder.a_pin, r.encoder.b_pin, &enc_prg);
  let enc_led = Output::new(r.encoder.led_pin, Level::Low);
  spawner.must_spawn(encoder_task(enc, enc_led, false));

  info!("Initialize, start up leds");

  let lts_prg = PioWs2812Program::new(&mut common);
  let lts = PioWs2812::new(&mut common, sm1, r.led.dma_chan, r.led.data_pin, &lts_prg);
  let en = Output::new(r.led.en_pin, Level::Low);
  let en_led = Output::new(r.led.en_led_pin, Level::Low);
  spawner.must_spawn(lights_task(lts, en, en_led));

  info!("Initialize, start manager");

  let mng_led = Output::new(r.manager.led_pin, Level::Low);
  spawner.must_spawn(manager_task(spawner, mng_led, flash, map_flash_range));

  info!("Main task finished; feeding watchdog");

  loop {
    Timer::after_secs(1).await;
    watchdog.feed();
    boot_led.toggle();
  }
}