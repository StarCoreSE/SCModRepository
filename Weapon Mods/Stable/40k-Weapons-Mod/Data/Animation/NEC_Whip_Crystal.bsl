@BlockID "MonoWhip"
@Version 2
@Author darth411

using Crystal as Subpart("MonoCrystal")

var loopDelay = 2000

func rotateCrystal() {
	if (block.isfunctional() == true) {
		Crystal.rotate([0, 1, 0], 180.0, loopDelay, Linear)
		}
		}
func bounceCrystal() {
	if (block.isfunctional() == true) {
     Crystal.translate([0,0.3,0], 120, Linear).delay(120).translate([0,-0.3,0], 120, Linear).delay(120)
}
}
	

action Block() {
	Working() {
		Crystal.setresetpos()

		API.startloop("rotateCrystal", loopDelay, -1)
		API.startloop("bounceCrystal", 240, -1)
	}

	NotWorking() {
		API.stoploop("rotateCrystal")
		API.stoploop("bounceCrystal")
	}
}