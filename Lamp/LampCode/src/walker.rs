
use defmt::*;
use embassy_time::Instant;

use crate::color::{f32_ratio_to_u8_pct, lerp16_by_8, lerp8, LampColor, RGBA8};

use rand::{
  distributions::{Standard, Distribution},
  Rng
};


pub const WALKER_INCREMENT: u8 = 32;
pub const WALKER_EASE_STEP: f32 = 8.0f32;
pub const WALKER_VALUE_DEFAULT: u8 = 128;

// in case we want to favor high / low
// TODO: this should come from value, but meh
const WALKER_CHOOSE_HIGH: u8 = 128;


#[derive(Format, PartialEq, Default, Clone, Copy)]
pub enum WalkerState {
  #[default]
  Holding,
  FadingInLow,
  Low,
  FadingOutLow,
  FadingInHigh,
  High,
  FadingOutHigh,
}

impl WalkerState {
  fn is_high(&self) -> bool {
    match self {
      WalkerState::FadingInHigh | WalkerState::High | WalkerState::FadingOutHigh => true,
      _ => false
    }
  }
  fn toggle_next(&self, choose_high: bool) -> WalkerState {
    match self {
        WalkerState::Holding => {
          if choose_high {
            WalkerState::FadingInHigh
          } else {
            WalkerState::FadingInLow
          }
        },
        WalkerState::FadingInLow => WalkerState::Low,
        WalkerState::Low => WalkerState::FadingOutLow,
        WalkerState::FadingOutLow => WalkerState::Holding,
        WalkerState::FadingInHigh => WalkerState::High,
        WalkerState::High => WalkerState::FadingOutHigh,
        WalkerState::FadingOutHigh => WalkerState::Holding,
    }
  }

  fn get_time_range(&self, value: u8) -> (u16, u16) {
    match self {
      WalkerState::Holding => {
        // hold range at 0: (2000, 3000)
        // hold range at 255: (200, 350)
        (lerp16_by_8(2_000, 200, value), lerp16_by_8(3000, 350, value))
      },
      WalkerState::FadingInLow | WalkerState::FadingInHigh => {
        // fade in range at 0: (1_750, 2_500)
        // fade in range at 255: (750, 1_000)
        (lerp16_by_8(1_750, 750, value), lerp16_by_8(2_500, 1_000, value))
      },
      WalkerState::Low | WalkerState::High => {
        // pause range at 0: (2_250, 2_750)
        // pause range at 255: (100, 200)
        (lerp16_by_8(2_500, 100, value), lerp16_by_8(2_750, 200, value))
      },
      WalkerState::FadingOutLow | WalkerState::FadingOutHigh => {
        // fade out range at 0: (1_750, 2_500)
        // fade out range at 255: (600, 1_000)
        (lerp16_by_8(1_750, 600, value), lerp16_by_8(2_500, 1_000, value))
      }
    }
  }
  fn get_fade_value(&self, pause_pct: u8, elapsed_pct: u8, value: u8) -> u8 {
    match self {
      WalkerState::Holding => self.get_hold_value(value),
      WalkerState::FadingInLow | WalkerState::FadingInHigh => {
        lerp8(self.get_hold_value(value), self.get_pause_value(pause_pct, value), elapsed_pct)
      },
      WalkerState::FadingOutLow | WalkerState::FadingOutHigh => {
        lerp8(self.get_pause_value(pause_pct, value), self.get_hold_value(value), elapsed_pct)
      },
      WalkerState::Low | WalkerState::High => self.get_pause_value(pause_pct, value),
    }
  }
  fn get_hold_value(&self, value: u8) -> u8 {
    // hold at 0: 160,
    // hold at 255: 128
    lerp8(160, 128, value)
  }

  fn get_pause_value(&self, pause_pct: u8, value: u8) -> u8 {
    let (min_pause_value, max_pause_value) = if self.is_high() {
      // high pause range
      // 192, 255?
      (192, 255)
    } else {
      // low pause range
      // pause range at 0: (32, 96)
      // pause at 255: (0, 48) 
      (lerp8(32, 0, value), lerp8(96, 48, value))
    };
    lerp8(min_pause_value, max_pause_value, pause_pct)
  }
}

impl Distribution<WalkerState> for Standard {
  fn sample<R: Rng + ?Sized>(&self, rng: &mut R) -> WalkerState {
      match rng.gen_range(0..=6) {
        0 => WalkerState::Holding,
        1 => WalkerState::FadingInLow,
        2 => WalkerState::Low,
        3 => WalkerState::FadingOutLow,
        4 => WalkerState::FadingInHigh,
        5 => WalkerState::High,
        _ => WalkerState::FadingOutHigh,
      }
  }
}

#[derive(Clone, Copy)]
pub struct Walker {
  state: WalkerState,
  last_time: Instant,
  elapsed_move: u8,
  move_pct: u8,
  pause_pct: u8,
}

impl Walker {

  pub fn new_walkers<const N: usize>(rng: &mut impl Rng) -> [Walker; N] {
    let mut walkers = [Walker::new(); N];
    // info!("Printing time ranges");
    // for idx in [0, 128, 255] {
    //   info!("Holding range ({:?}): {:?}", idx, WalkerState::Holding.get_time_range(idx));
    //   info!("In range ({:?}): {:?}", idx, WalkerState::FadingInHigh.get_time_range(idx));
    //   info!("Pausing range ({:?}): {:?}", idx, WalkerState::High.get_time_range(idx));
    //   info!("Out range ({:?}): {:?}", idx, WalkerState::FadingOutHigh.get_time_range(idx));
    // }
    // info!("Printing values");
    // for idx in [0, 255] {
    //   info!("Holding Value ({:?}): {:?}", idx, WalkerState::Holding.get_hold_value(idx));
    //   info!(
    //     "Low Range ({:?}): ({:?}, {:?})", idx,
    //     WalkerState::Low.get_pause_value(0, idx),
    //     WalkerState::Low.get_pause_value(255, idx)
    //   );
    //   info!(
    //     "High Range ({:?}): ({:?}, {:?})", idx,
    //     WalkerState::High.get_pause_value(0, idx),
    //     WalkerState::High.get_pause_value(255, idx)
    //   );
    // }
    Walker::setup_walkers(&mut walkers, rng);
    walkers
  }

  pub fn setup_walkers<const N: usize>(walkers: &mut [Walker; N], rng: &mut impl Rng) {
    // info!("Updating walkers with intensity: {:?}", intensity.to_usize());
    for (idx, walker) in walkers.iter_mut().enumerate() {
      walker.setup_walker(idx, rng);
    }
  }
  
  pub fn run_walkers<const N: usize>(
    data: &mut [RGBA8; N], walkers: &mut [Walker; N], value: u8, color: &RGBA8, rng: &mut impl Rng
  ) {
    for (walker, led) in walkers.iter_mut().zip(data.iter_mut()) { 
      walker.run_walker(led, value, color, rng);
    }
  }

  fn new() -> Self {
    Self {
      state: WalkerState::default(),
      last_time: Instant::now(),
      elapsed_move: u8::default(),
      move_pct: u8::default(),
      pause_pct: u8::default(),
    }
  }
  
  fn set_pause_pct(&mut self, rng: &mut impl Rng) {
    self.pause_pct = rng.gen();
  }

  fn setup_walker(&mut self, idx: usize, rng: &mut impl Rng) {
    self.state = if idx == 0 {
      // fading in low means it will start out low
      WalkerState::FadingInLow
    } else {
      // otherwise, let it be somewhere random
      rng.gen()
    };
    // may not be needed but meh
    self.set_pause_pct(rng);
    self.transition_state(rng);
    // info!("Started Walker: {:?}; in state: {:?}", idx + 1, self.state);
  }

  fn run_walker(&mut self, led: &mut RGBA8, value: u8, color: &RGBA8, rng: &mut impl Rng) {
    let (min_time, max_time) = self.state.get_time_range(value);
    let time_in_state_in_ms = lerp16_by_8(min_time, max_time, value);
    let elapsed_pct = f32_ratio_to_u8_pct(
      self.last_time.elapsed().as_millis() as f32,
      time_in_state_in_ms as f32
    );
    if elapsed_pct > 0 {
      self.elapsed_move = self.elapsed_move.saturating_add(elapsed_pct);
      if self.elapsed_move == 255 {
        self.transition_state(rng);
      } else {
        self.last_time = Instant::now();
      }
    }
    let pct = self.state.get_fade_value(self.pause_pct, self.elapsed_move, value);
    led.fade_from(color, pct);
  }

  fn transition_state(&mut self, rng: &mut impl Rng) {
    if self.state == WalkerState::Holding {
      // about to transition, update pct it will walk toward
      self.set_pause_pct(rng);
    }
    self.state = self.state.toggle_next(rng.gen::<u8>() >= WALKER_CHOOSE_HIGH);
    self.move_pct = rng.gen();
    self.last_time = Instant::now();
    self.elapsed_move = 0;
  }
}