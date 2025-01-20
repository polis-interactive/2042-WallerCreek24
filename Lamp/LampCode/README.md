## Flashing

Insure the RPI Debug Probe is connected to your computer, and to the board

### Desk Lamp

```shell
cargo run --bin desk --release
```

### Module

```shell
cargo run --bin module --release
```

NOTE: if the module encoder direction is backwards
- CALL BROOSE
- try swapping a_pin / b_pin in src/bin/module.rs, PIN_10 and PIN_11 respectively
- try switching encoder_task, flip_direction (third argument) from false to true in src/bin/module.rs
- permutations of the above 