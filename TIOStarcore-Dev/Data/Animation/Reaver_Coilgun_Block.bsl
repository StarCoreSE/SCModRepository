@BlockID "Reaver_Coilgun"
@Version 2
@Author Krynoc
@Weaponcore 0


#Valid subpart ID's of Reaver_Coilgun_Block
#|  CoilgunMk2Ele
#|  |  CoilgunMk2LeftRight
#|  |  |  CoilgunMk2UpDown
#|  |  |  |  CoilgunMk2LeftBarrel
#|  |  |  |  CoilgunMk2RightBarrel
#|  |  |  |  CoilgunMk2Muzzles


# Declarations
using Elevator as Subpart("CoilgunMk2Ele")
using Azimuth as Subpart("CoilgunMk2LeftRight") parent Elevator
using Elevation as Subpart("CoilgunMk2UpDown") parent Azimuth
using BarrelLeft as Subpart("CoilgunMk2LeftBarrel") parent Elevation
using BarrelRight as Subpart("CoilgunMk2RightBarrel") parent Elevation

var isWorking = true



# Animations
func GunTurnOn() {
	if (isWorking == false) {
		Elevator.Translate([0, -2, 0], 170, Linear)
		BarrelLeft.delay(180).Translate([0, 0, 0.9], 40, Linear)
		BarrelRight.delay(180).Translate([0, 0, 0.9], 40, Linear)
	}
	isWorking = true
}
func GunTurnOff() {
	if (isWorking == true) {
		Azimuth.MoveToOrigin(50, InOutSine)
		Elevation.MoveToOrigin(90, InOutSine)

		Elevator.Translate([0, 2, 0], 170, Linear)
		BarrelLeft.Translate([0, 0, -0.9], 100, Linear)
		BarrelRight.Translate([0, 0, -0.9], 100, Linear)
	}
	isWorking = false
}


# Events
Action Block() {
	Built() {
		isWorking = true
	}
	Working() {
		GunTurnOn()
	}
	NotWorking() {
		GunTurnOff()
	}
}