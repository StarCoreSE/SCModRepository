@BlockID "Priest_Block"
@Version 2
@Author Krynoc
@Weaponcore 0

#Valid subpart ID's of Priest_Block
#|  AZ
#|  |  UpDown
#|  |  |  EZ
#|  |  |  |  BottomFrontBarrel
#|  |  |  |  |  BottomMiddleBarrel
#|  |  |  |  |  |  BottomEndBarrel
#|  |  |  |  RightFrontBarrel
#|  |  |  |  |  RightMiddleBarrel
#|  |  |  |  |  |  RightEndBarrel
#|  |  |  |  Muzzles
#|  |  |  |  LeftFrontBarrel
#|  |  |  |  |  LeftMiddleBarrel
#|  |  |  |  |  |  LeftEndBarrel
#|  |  Spin
#|  |  LeftDoor
#|  |  RightDoor


# Declarations
using Azimuth as Subpart("AZ")
using Elevator as Subpart("UpDown") parent Azimuth
using Elevation as Subpart("EZ") parent Elevator

using BottomBarrelFront as subpart("BottomFrontBarrel") parent Elevation
using BottomBarrelMiddle as subpart("BottomMiddleBarrel") parent BottomBarrelFront
using BottomBarrelEnd as subpart("BottomEndBarrel") parent BottomBarrelMiddle

using RightBarrelFront as subpart("RightFrontBarrel") parent Elevation
using RightBarrelMiddle as subpart("RightMiddleBarrel") parent RightBarrelFront
using RightBarrelEnd as subpart("RightEndBarrel") parent RightBarrelMiddle

using LeftBarrelFront as subpart("LeftFrontBarrel") parent Elevation
using LeftBarrelMiddle as subpart("LeftMiddleBarrel") parent LeftBarrelFront
using LeftBarrelEnd as subpart("LeftEndBarrel") parent LeftBarrelMiddle

using Spinner as Subpart("Spin") parent Azimuth
using DoorLeft as Subpart("LeftDoor") parent Azimuth
using DoorRight as Subpart("RightDoor") parent Azimuth

var isWorking = true

# Animations
func GunTurnOn() {
	if (isWorking == false) {
		API.StopDelays()

		Azimuth.reset().Translate([0, 1, 0], 0, Instant).delay(100).Translate([0, -1, 0], 100, Linear)
		Elevation.reset().Rotate([1, 0, 0], -90, 0, Instant).delay(200).Rotate([1, 0, 0], 90, 100, Linear)

		Elevator.reset().Translate([0, 7, 0], 0, Instant).delay(100).Translate([0, -7, 0], 100, Linear)
		Spinner.reset().delay(100).Spin([0, 1, 0], -1, 150)

		RightBarrelFront.reset().Translate([0, 0, -0.6], 0, Instant).delay(160).Translate([0, 0, 0.6], 50, Linear)
		RightBarrelMiddle.reset().Translate([0, 0, -0.4], 0, Instant).delay(160).Translate([0, 0, 0.4], 50, Linear)
		RightBarrelEnd.reset().Translate([0, 0, -0.8], 0, Instant).delay(160).Translate([0, 0, 0.8], 50, Linear)

		BottomBarrelFront.reset().Translate([0, 0, -0.6], 0, Instant).delay(200).Translate([0, 0, 0.6], 50, Linear)
		BottomBarrelMiddle.reset().Translate([0, 0, -0.4], 0, Instant).delay(200).Translate([0, 0, 0.4], 50, Linear)
		BottomBarrelEnd.reset().Translate([0, 0, -0.8], 0, Instant).delay(200).Translate([0, 0, 0.8], 50, Linear)

		LeftBarrelFront.reset().Translate([0, 0, -0.6], 0, Instant).delay(230).Translate([0, 0, 0.6], 50, Linear)
		LeftBarrelMiddle.reset().Translate([0, 0, -0.4], 0, Instant).delay(230).Translate([0, 0, 0.4], 50, Linear)
		LeftBarrelEnd.reset().Translate([0, 0, -0.8], 0, Instant).delay(230).Translate([0, 0, 0.8], 50, Linear)

		DoorLeft.reset().Translate([-1.25, -1.4, 0], 0, Instant).Translate([0, 1.4, 0], 50, Linear).delay(50).Translate([1.25, 0, 0], 50, Linear)
		DoorRight.reset().Translate([1.25, -1.4, 0], 0, Instant).Translate([0, 1.4, 0], 50, Linear).delay(50).Translate([-1.25, 0, 0], 50, Linear)
	}
	isWorking = true
}
func GunTurnOff() {
	if (isWorking == true) {
		API.StopDelays()

		Azimuth.MoveToOrigin(60, InOutSine).delay(60).reset().delay(100).Translate([0, 1, 0], 150, Linear)
		Elevation.MoveToOrigin(70, InOutSine).delay(70).reset().Rotate([1, 0, 0], -90, 100, Linear)

		Elevator.reset().delay(170).Translate([0, 7, 0], 150, Linear)
		Spinner.reset().delay(150).Spin([0, 1, 0], 1, 150)

		RightBarrelFront.reset().Translate([0, 0, -0.6], 35, Linear)
		RightBarrelMiddle.reset().Translate([0, 0, -0.4], 35, Linear)
		RightBarrelEnd.reset().Translate([0, 0, -0.8], 35, Linear)

		BottomBarrelFront.reset().delay(30).Translate([0, 0, -0.6], 35, Linear)
		BottomBarrelMiddle.reset().delay(30).Translate([0, 0, -0.4], 35, Linear)
		BottomBarrelEnd.reset().delay(30).Translate([0, 0, -0.8], 35, Linear)

		LeftBarrelFront.reset().delay(60).Translate([0, 0, -0.6], 35, Linear)
		LeftBarrelMiddle.reset().delay(60).Translate([0, 0, -0.4], 35, Linear)
		LeftBarrelEnd.reset().delay(60).Translate([0, 0, -0.8], 35, Linear)

		DoorLeft.reset().delay(300).Translate([-1.25, 0, 0], 35, Linear).delay(35).Translate([0, -1.4, 0], 35, Linear)
		DoorRight.reset().delay(300).Translate([1.25, 0, 0], 35, Linear).delay(35).Translate([0, -1.4, 0], 35, Linear)
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






