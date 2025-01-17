
pub const WALKER_INTENSITY_MAX: u8 = 5;

#[derive(PartialEq, Clone, Copy)]
pub struct WalkerIntensity(u8);

impl WalkerIntensity {
  pub fn to_u8(&self) -> u8 {
    self.0
  }
}

impl From<u8> for WalkerIntensity {
  fn from(item: u8) -> Self {
    if item >= WALKER_INTENSITY_MAX {
      return Self(WALKER_INTENSITY_MAX);
    } else {
      return Self(item);
    }
  }
}

pub const WALKER_FADE_IN_STEP: u8 = 8;

#[derive(PartialEq)]
pub struct WalkerSetting {
  pub intensity: WalkerIntensity,
  pub pct: u8
}

enum WalkerState {

}