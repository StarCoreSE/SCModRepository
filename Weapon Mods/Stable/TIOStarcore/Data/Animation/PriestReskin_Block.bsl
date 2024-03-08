@BlockID "PriestReskin_Block"
@Version 2
@Author Krynoc
@Weaponcore 0

#Valid subpart ID's of PriestReskin_Block
#|  PreistReskin_Elevator
#|  |  PreistReskin_AZ
#|  |  |  PreistReskin_EL
#|  |  |  |  PreistReskin_Barrels
#|  PriestReskin_RightDoor
#|  PriestReskin_LeftDoor
#|  PriestReskin_FrontDoor
#|  PriestReskin_BackDoor


# Declarations
	using Elevator as subpart("PreistReskin_Elevator")
	using Azimuth as subpart("PreistReskin_AZ") parent Elevator
	using Elevation as subpart("PreistReskin_EL") parent Azimuth
	using Barrels as subpart("PreistReskin_Barrels") parent Elevation
	using DoorRight as subpart("PriestReskin_RightDoor")
	using DoorLeft as subpart("PriestReskin_LeftDoor")
	using DoorFront as subpart("PriestReskin_FrontDoor")
	using DoorBack as subpart("PriestReskin_BackDoor")

	using BeaconEmissive as emissive("Emissive")

	var isWorking = true


# Animations
func GunTurnOn() {
	if (isWorking == false) {
		API.StopDelays()

		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(250).SetColor(10, 255, 0, 1.0, false)

		Barrels.reset().Translate([0, 0, -2.5], 0, Instant).delay(120).Translate([0, 0, 2.5], 60, InOutSine)
		Azimuth.reset()
		Elevation.reset().Rotate([1, 0, 0], -20, 0, Instant).delay(120).Rotate([1, 0, 0], 20, 60, InOutSine)

		Elevator.reset().Translate([0, 3.5, 0], 0, Instant).delay(90).Translate([0, -3.5, 0], 90, InOutSine)

		DoorLeft.reset().Rotate([0, 0, 1], 90, 0, Instant).Translate([0, -3.3, 0], 0, Instant).delay(0).Rotate([0, 0, 1], -90, 90, InOutSine).delay(90).Translate([0, 3.3, 0], 60, OutSine)
		DoorRight.reset().Rotate([0, 0, 1], -90, 0, Instant).Translate([0, -3.3, 0], 0, Instant).delay(0).Rotate([0, 0, 1], 90, 90, InOutSine).delay(90).Translate([0, 3.3, 0], 60, OutSine)
		DoorFront.reset().Rotate([1, 0, 0], -90, 0, Instant).Translate([0, -1.2, 0], 0, Instant).delay(15).Rotate([1, 0, 0], 90, 90, InOutSine).delay(90).Translate([0, 1.2, 0], 30, OutSine)
		DoorBack.reset().Rotate([1, 0, 0], 90, 0, Instant).Translate([0, -1.2, 0], 0, Instant).delay(15).Rotate([1, 0, 0], -90, 90, InOutSine).delay(90).Translate([0, 1.2, 0], 30, OutSine)
	}
	isWorking = true
}
func GunTurnOff() {
	if (isWorking == true) {
		API.StopDelays()
		
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(250).SetColor(255, 0, 0, 1.0, false)

		Barrels.reset().delay(45).Translate([0, 0, -2.5], 60, InOutSine)
		Azimuth.MoveToOrigin(60, InOutSine).delay(60).reset()
		Elevation.MoveToOrigin(45, InOutSine).delay(45).reset().Rotate([1, 0, 0], -20, 60, InOutSine)

		Elevator.reset().delay(60).Translate([0, 3.5, 0], 90, InOutSine)

		DoorLeft.reset().delay(75).Translate([0, -3.3, 0], 60, InSine).delay(60).Rotate([0, 0, 1], 90, 90, InOutSine)
		DoorRight.reset().delay(75).Translate([0, -3.3, 0], 60, InSine).delay(60).Rotate([0, 0, 1], -90, 90, InOutSine)
		DoorFront.reset().delay(90).Translate([0, -1.2, 0], 30, InSine).delay(30).Rotate([1, 0, 0], -90, 90, InOutSine)
		DoorBack.reset().delay(90).Translate([0, -1.2, 0], 30, InSine).delay(30).Rotate([1, 0, 0], 90, 90, InOutSine)
	}
	isWorking = false
}

func BeaconReady() { BeaconEmissive.SetColor(10, 255, 0, 1.0, false) }
func BeaconOverheat() { BeaconEmissive.SetColor(255, 103, 0, 1.0, false).delay(30).SetColor(255, 255, 0, 1.0, false) }



# Events
Action Block() {
	Built() {
		isWorking = true
		BeaconEmissive.SetColor(10, 255, 0, 1.0, false)
	}
	Working() {
		GunTurnOn()
	}
	NotWorking() {
		API.StopLoop("BeaconOverheat")
		API.StopLoop("BeaconReady")
		GunTurnOff()
	}
}

Action Weaponcore() {
	Overheated() {
		API.StartLoop("BeaconOverheat", 60, 18)
		API.StartLoop("BeaconReady", 1, 1, 1080)
	}
}