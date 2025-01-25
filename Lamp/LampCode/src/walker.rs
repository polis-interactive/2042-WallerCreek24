
use defmt::Format;
use embassy_time::{Instant, Duration};

use crate::color::{duration_to_u8_pct, lerp8, LampColor, RGBA8};

use rand::{
  distributions::{Standard, Distribution},
  Rng
};


pub const WALKER_VALUE_MAX_INCLUSIVE: u8 = 2;

#[derive(PartialEq, Clone, Copy)]
pub struct WalkerValue(u8);

impl WalkerValue {
  pub fn to_usize(&self) -> usize {
    self.0 as usize
  }
}

impl From<u8> for WalkerValue {
  fn from(item: u8) -> Self {
    if item >= WALKER_VALUE_MAX_INCLUSIVE {
      return Self(WALKER_VALUE_MAX_INCLUSIVE);
    } else {
      return Self(item);
    }
  }
}

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


#[derive(PartialEq)]
struct HoldConfig {
  hold_value: u8,
  min_low_value: u8,
  max_low_value: u8,
  min_high_value: u8,
  max_high_value: u8,
  min_hold_time: Duration,
  max_hold_time: Duration,
  min_transition_in_time: Duration,
  max_transition_in_time: Duration,
  min_pause_time: Duration,
  max_pause_time: Duration,
  min_transition_out_time: Duration,
  max_transition_out_time: Duration,
  weight_choose_high: u8
}

static HOLD_CONFIGS: [HoldConfig; (WALKER_VALUE_MAX_INCLUSIVE + 1) as usize] = [
  // solid
  HoldConfig {
    hold_value: 255,
    min_low_value: 255,
    max_low_value: 255,
    min_high_value: 255,
    max_high_value: 255,
    min_hold_time: Duration::from_millis(1_000),
    max_hold_time: Duration::from_millis(1_000),
    min_transition_in_time: Duration::from_millis(1_000),
    max_transition_in_time: Duration::from_millis(1_000),
    min_pause_time: Duration::from_millis(1_000),
    max_pause_time: Duration::from_millis(1_000),
    min_transition_out_time: Duration::from_millis(1_000),
    max_transition_out_time: Duration::from_millis(1_000),
    weight_choose_high: 128
  },
  // chill
  HoldConfig {
    hold_value: 160,
    min_low_value: 32,
    max_low_value: 96,
    min_high_value: 192,
    max_high_value: 255,
    min_hold_time: Duration::from_millis(2_000),
    max_hold_time: Duration::from_millis(3_000),
    min_transition_in_time: Duration::from_millis(1_750),
    max_transition_in_time: Duration::from_millis(2_500),
    min_pause_time: Duration::from_millis(2_250),
    max_pause_time: Duration::from_millis(2_750),
    min_transition_out_time: Duration::from_millis(1_750),
    max_transition_out_time: Duration::from_millis(2_500),
    weight_choose_high: 128
  },
  // eratic
  HoldConfig {
    hold_value: 128,
    min_low_value: 0,
    max_low_value: 64,
    min_high_value: 192,
    max_high_value: 255,
    min_hold_time: Duration::from_millis(200),
    max_hold_time: Duration::from_millis(350),
    min_transition_in_time: Duration::from_millis(750),
    max_transition_in_time: Duration::from_millis(1_000),
    min_pause_time: Duration::from_millis(100),
    max_pause_time: Duration::from_millis(200),
    min_transition_out_time: Duration::from_millis(600),
    max_transition_out_time: Duration::from_millis(1_000),
    weight_choose_high: 128
  },
];

#[derive(Clone, Copy)]
pub struct Walker<'a> {
  pub state: WalkerState,
  last_time: Instant,
  time_in_state: Duration,
  pause_value: u8,
  hold_config: &'a HoldConfig
}

impl<'a> Walker<'a> {

  pub fn new_walkers<const N: usize>(value: &WalkerValue, rng: &mut impl Rng) -> [Walker<'a>; N] {
    let mut walkers = [Walker::new(); N];
    Walker::update_walkers(&mut walkers, value, rng);
    walkers
  }
  
  pub fn update_walkers<const N: usize>(walkers: &mut [Walker; N], value: &WalkerValue, rng: &mut impl Rng) {
    // info!("Updating walkers with intensity: {:?}", intensity.to_usize());
    for (idx, walker) in walkers.iter_mut().enumerate() {
      walker.update_walker(value, rng, idx);
    }
  }
  
  pub fn run_walkers<const N: usize>(data: &mut [RGBA8; N], walkers: &mut [Walker; N], color: &RGBA8, rng: &mut impl Rng) {
    for (walker, led) in walkers.iter_mut().zip(data.iter_mut()) { 
      walker.run_walker(led, color, rng);
    }
  }

  fn new() -> Self {
    Self {
      state: WalkerState::default(),
      last_time: Instant::now(),
      time_in_state: Duration::default(),
      hold_config: &HOLD_CONFIGS[0],
      pause_value: 0,
    }
  }
  
  fn set_pause_value(&mut self, rng: &mut impl Rng) {
    let config = self.hold_config;
    // this isn't the most accurate but good enough for our purposes
    let range = if self.state.is_high() {
      config.min_high_value..=config.max_high_value
    } else {
      config.min_low_value..=config.max_low_value
    };
    self.pause_value = rng.gen_range(range);
  }

  fn update_walker(&mut self, value: &WalkerValue, rng: &mut impl Rng, idx: usize) {
    self.hold_config = &HOLD_CONFIGS[value.to_usize()];
    self.state = if idx == 0 {
      // holding means it will transition to  low
      WalkerState::FadingInLow
    } else {
      // otherwise, let it be somewhere random
      rng.gen()
    };
    // may not be needed but meh
    self.set_pause_value(rng);
    self.transition_state(rng);
    // info!("Started Walker: {:?}; in state: {:?}", idx + 1, self.state);
  }

  fn run_walker(&mut self, led: &mut RGBA8, color: &RGBA8, rng: &mut impl Rng) {
    if self.last_time.elapsed() > self.time_in_state {
      self.transition_state(rng);
    }
    let fade_pct = duration_to_u8_pct(&self.last_time, &self.time_in_state);
    let pct = match self.state {
        WalkerState::Holding => self.hold_config.hold_value,
        WalkerState::FadingInLow | WalkerState::FadingInHigh => {
          lerp8(self.hold_config.hold_value, self.pause_value, fade_pct)
        },
        WalkerState::FadingOutLow | WalkerState::FadingOutHigh => {
          lerp8(self.pause_value, self.hold_config.hold_value, fade_pct)
        },
        WalkerState::Low | WalkerState::High => self.pause_value,
    };
    led.fade_from(color, pct);
  }

  fn transition_state(&mut self, rng: &mut impl Rng) {
    let config = self.hold_config;
    let min_millis;
    let max_millis;
    match self.state {
      WalkerState::Holding => {
        self.state = match rng.gen::<u8>() >= config.weight_choose_high {
          true => {
            WalkerState::FadingInHigh
          },
          false => {
            WalkerState::FadingInLow
          },
        };
        self.set_pause_value(rng);
        min_millis = config.min_transition_in_time.as_millis();
        max_millis = config.max_transition_in_time.as_millis();
      },
      WalkerState::FadingInLow => {
        self.state = WalkerState::Low;
        min_millis = config.min_pause_time.as_millis();
        max_millis = config.max_pause_time.as_millis();
      },
      WalkerState::FadingInHigh => {
        self.state = WalkerState::High;
        min_millis = config.min_pause_time.as_millis();
        max_millis = config.max_pause_time.as_millis();
      }
      WalkerState::Low => {
        self.state = WalkerState::FadingOutLow;
        min_millis = config.min_transition_out_time.as_millis();
        max_millis = config.max_transition_out_time.as_millis();
      },
      WalkerState::High => {
        self.state = WalkerState::FadingOutHigh;
        min_millis = config.min_transition_out_time.as_millis();
        max_millis = config.max_transition_out_time.as_millis();
      }
      WalkerState::FadingOutLow | WalkerState::FadingOutHigh => {
        self.state = WalkerState::Holding;
        min_millis = config.min_hold_time.as_millis();
        max_millis = config.max_hold_time.as_millis();
      }
    };
    self.time_in_state = Duration::from_millis(rng.gen_range(min_millis..=max_millis));
    self.last_time = Instant::now();
  }
}