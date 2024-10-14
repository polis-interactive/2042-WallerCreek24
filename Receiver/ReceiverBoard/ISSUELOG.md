ISSUELOG
- v0.1
	- Schematic Issues
		- No resistors on the outputs of the buffer chip
		- Only accept 24v; need a 24v to 5v onboard converter - TSR 1-2450E 
		- maybe use jumper pins to allow switching 5v from converter usb?
		- 16 LED Outputs
	- Part Issues
		- Only use named parts for everything (namely headers and capacitors); current setup is quite jank
	- PCB Issues	
		- Ethernet jack is too close to the usb header; left shift it
		- Forgot to hook up 5v to a pin on the buffer chip
		- Didn't wire up the led?
		- crossed TX and RX... how?
		- Route all traces from leds from left side to left buffer chip, right side to right buffer chip; make it easier to route power
- v0.2
	- Schematic Issues
		- Need decoupling caps on all outputs
		- Need spot for bulk decoupling cap
	- Part issues
		- See if you can't use a resitor network x8
	- PCB Issues
		- too hard to solder cap near ethernet
		- scoot ethernet closer to the edge, hard to disconnect
		- the headers are too close together (dumbass)
		- What the hell was up with you decoupling cap on the south buffer chip
		- move 5v rail next to buffer chips
		- just use a ground pour for the primary + trace
		- can we place leds on the data lines?

Notes:
- teensy pins
	- 24 each side at 2.54mm spacing (6.09 mm?)
	- 3x2 for ethernet at 2.0mm spacing
ToDo:
- measure the teensy socket so we can send them to JLC https://www.pjrc.com/store/socket_24x1.html