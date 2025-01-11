use heapless::FnvIndexMap;

use crate::color::RGBA8;


pub const MODE_MAX: u16 = 3;

#[derive(Debug, PartialEq, Eq, Hash, Clone, Copy)]
pub enum Modes {
  Solid,
  CheckerBoard,
  Wheel
}

impl From<u16> for Modes {
  fn from(i: u16) -> Self {
    match i {
      0 => Modes::Solid,
      1 => Modes::Wheel,
      2 => Modes::CheckerBoard,
      _ => panic!("invalid mode value: {i}")
    }
  }
}

// fade time to reach the new mode
pub const MODE_STEP: u8 = 12;

#[derive(Debug, PartialEq)]
pub struct ModeSet {
  pub mode: Modes,
  pub pct: u8,
}

pub trait ModeRunnerTrait: Sized {
  fn update_mode(&mut self, color: &RGBA8);
  fn run_mode<const N: usize>(&self, timestamp: u64, data: &mut[RGBA8; N]);
}

#[derive(Debug, Default)]
pub struct SolidRunner {
  color: RGBA8
}

impl ModeRunnerTrait for SolidRunner {
  fn update_mode(&mut self, color: &RGBA8) {
    self.color.clone_from(color);
  }
  
  fn run_mode<const N: usize>(&self, _timestamp: u64, data: &mut[RGBA8; N]) {
    for led in data.iter_mut() {
      led.clone_from(&self.color);
    } 
  }
}

#[derive(Debug, Default)]
pub struct CheckerBoardRunner {
  color: RGBA8
}

impl ModeRunnerTrait for CheckerBoardRunner {
  fn update_mode(&mut self, color: &RGBA8) {
    self.color.r = color.g;
    self.color.g = color.b;
    self.color.b = color.r;
    self.color.a = color.a;
  }

  fn run_mode<const N: usize>(&self, _timestamp: u64, data: &mut[RGBA8; N]) {
    for led in data.iter_mut() {
      led.clone_from(&self.color);
    } 
  }
}

#[derive(Debug, Default)]
pub struct WheelRunner {
  color: RGBA8
}

impl ModeRunnerTrait for WheelRunner {
  fn update_mode(&mut self, color: &RGBA8) {
    self.color.r = color.b;
    self.color.g = color.r;
    self.color.b = color.g;
    self.color.a = color.a;
  }

  fn run_mode<const N: usize>(&self, _timestamp: u64, data: &mut[RGBA8; N]) {
    for led in data.iter_mut() {
      led.clone_from(&self.color);
    } 
  }
}

#[derive(Debug)]
pub enum ModeRunner {
  SolidRunner(SolidRunner),
  CheckerBoardRunner(CheckerBoardRunner),
  WheelRunner(WheelRunner)
}

impl ModeRunnerTrait for ModeRunner {
  fn update_mode(&mut self, color: &RGBA8) {
      match self {
        ModeRunner::SolidRunner(solid_runner) => solid_runner.update_mode(color),
        ModeRunner::CheckerBoardRunner(checker_board_runner) => checker_board_runner.update_mode(color),
        ModeRunner::WheelRunner(wheel_runner) => wheel_runner.update_mode(color),
      }
  }
  fn run_mode<const N: usize>(&self, timestamp: u64, data: &mut[RGBA8; N]) {
      match self {
        ModeRunner::SolidRunner(solid_runner) => solid_runner.run_mode(timestamp, data),
        ModeRunner::CheckerBoardRunner(checker_board_runner) => checker_board_runner.run_mode(timestamp, data),
        ModeRunner::WheelRunner(wheel_runner) => wheel_runner.run_mode(timestamp, data),
      }
  }
}

// for some reason, IndexMap must be power of 2; make it the largest power of 2 >= num modes, shouldn't hurt anything
pub fn get_mode_runners() -> FnvIndexMap<Modes, ModeRunner, 4> {
  let mut mode_runners = FnvIndexMap::<Modes, ModeRunner, 4>::new();
  mode_runners.insert(Modes::Solid, ModeRunner::SolidRunner(SolidRunner::default())).unwrap();
  mode_runners.insert(Modes::CheckerBoard, ModeRunner::CheckerBoardRunner(CheckerBoardRunner::default())).unwrap();
  mode_runners.insert(Modes::Wheel, ModeRunner::WheelRunner(WheelRunner::default())).unwrap();
  mode_runners
}
