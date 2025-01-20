@BlockID "MBA57Bofors_Block"
@Version 2
@Author Killerbee77
@Weaponcore 0


#Valid subpart ID's of MBA57Bofors_Block
#|  MBA57Bofors_Elevator
#|  |  MBA57Bofors_Azimuth
#|  |  |  MBA57Bofors_Elevation
#|  |  |  |  MBA57Bofors_Barrels
#|  MBA57Bofors_RightDoor
#|  MBA57Bofors_LeftDoor


#Declarations of subparts and dummies in the model
using Elevator as SubPart("MBA57Bofors_Elevator")
using CannonAZ as SubPart("MBA57Bofors_Azimuth") parent Elevator
using CannonEL as Subpart("MBA57Bofors_Elevation") parent CannonAZ
using Barrel as Subpart("MBA57Bofors_Barrels") parent CannonEL
using LeftDoor as Subpart("MBA57Bofors_LeftDoor")
using RightDoor as Subpart("MBA57Bofors_RightDoor")

using BeaconEmissive as Emissive("Emissive")

var isWorking = true


# Position Resets
func DoorReset() {
	API.StopDelays()
	RightDoor.reset()
	LeftDoor.reset()
	Elevator.reset()
	Barrel.Reset()
}
func DoorResetClose() {
	DoorReset()
	RightDoor.Translate([0, -0.98, 0], 1, Instant).Rotate([0, 0, 1], -90, 1, Instant)
	LeftDoor.Translate([0, -0.98, 0], 1, Instant).Rotate([0, 0, 1], 90, 1, Instant)
	Elevator.Translate([0, 1.5, 0], 0, Instant)
}


# Animations
func GunTurnOn() {
	if (isWorking == false) {
		DoorResetClose()
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(250).SetColor(10, 255, 0, 1.0, false)

		RightDoor.delay(10).Rotate([0, 0, 0.98], 90, 90, Linear).delay(90).Translate([0, 1, 0], 60, Linear)
		LeftDoor.delay(30).Rotate([0, 0, 0.98], -90, 90, Linear).delay(90).Translate([0, 1, 0], 60, Linear)

		Elevator.delay(100).Translate([0, -1.5, 0], 120, Linear)
	}
	isWorking = true
}
func GunTurnOff() {
	if (isWorking == true) {
		DoorReset()
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(250).SetColor(255, 0, 0, 1.0, false)

		RightDoor.delay(40).Translate([0, -0.98, 0], 100, Linear).delay(100).Rotate([0, 0, 1], -90, 90, Linear)
		LeftDoor.delay(60).Translate([0, -0.98, 0], 100, Linear).delay(100).Rotate([0, 0, 1], 90, 90, Linear)

		CannonAZ.MoveToOrigin(120, InOutSine)
		CannonEL.MoveToOrigin(120, InOutSine)
		Elevator.delay(80).Translate([0, 1.5, 0], 100, Linear)
	}
	isWorking = false
}


#Events
Action Block() {
	Built() {
		isWorking = true
	}
	Working() {
		API.Log("Working")
		GunTurnOn()
	}
	NotWorking(){
		API.Log("NotWorking")
		GunTurnOff()
	}
}

func BeaconReady() { BeaconEmissive.SetColor(10, 255, 0, 1.0, false) }
func BeaconReload() { BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(60).SetColor(10, 255, 0, 1.0, false) }
func BeaconNoMags() { BeaconEmissive.SetColor(255, 0, 0, 1.0, false) }
func BeaconOverheat() { BeaconEmissive.SetColor(255, 103, 0, 1.0, false).delay(30).SetColor(255, 255, 0, 1.0, false) }

Action Weaponcore() {
	Reloading() {
		BeaconReload()
	}
	NoMagsToLoad() {
		BeaconNoMags()
	}
	Overheated() {
		API.StartLoop("BeaconOverheat", 60, 26)
		API.StartLoop("BeaconReady", 1, 1, 1560)
	}
}

#Red	SetColor(255, 0, 0, 1.0, false)
#Yellow	SetColor(255, 255, 0, 1.0, false)
#Orange	SetColor(255, 103, 0, 1.0, false)
#Green	SetColor(10, 255, 0, 1.0, false)
