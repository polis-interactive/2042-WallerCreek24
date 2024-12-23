#![no_std]

mod common;

mod button;
pub use button::{button_task, Debouncer};

mod encoder;
pub use encoder::encoder_task;

mod color;

mod store;
pub use store::reset_state;

mod lights;
pub use lights::lights_task;

mod manager;
pub use manager::manager_task;