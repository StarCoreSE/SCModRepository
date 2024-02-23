@BlockID "Type21_Artillery"
@Version 2
@Author Krynoc
@Weaponcore 0

#Valid subpart ID's of Type21_Artillery_Block
#|  MK2_Elevator
#|  |  MK2_AZ
#|  |  |  MK2EL
#|  |  |  |  MK2_Barrels
#|  |  |  |  Mk2_Camera
#|  MK2_RightDoor
#|  MK2_LeftDoor
#|  MK2_FrontDoor
#|  MK2_BackDoor

#Declarations of subparts and dummies in the model
using Elevator as Subpart("MK2_Elevator")
using CannonAZ as SubPart("MK2_AZ") parent Elevator
using CannonEL as Subpart("MK2EL") parent CannonAZ
using Barrel as SubPart("MK2_Barrels") parent CannonEL
using Camera as Subpart("Mk2_Camera") parent CannonEL
using LeftDoor as Subpart("MK2_LeftDoor")
using RightDoor as Subpart("MK2_RightDoor")
using BackDoor as Subpart("MK2_BackDoor")
using FrontDoor as Subpart("MK2_FrontDoor")

using BeaconEmissive as Emissive("Emissive")

var isWorking = true


# Position Resets
func DoorReset() {
	API.StopDelays()
	FrontDoor.reset()
	RightDoor.reset()
	BackDoor.reset()
	LeftDoor.reset()
	Barrel.Reset()
	Camera.Reset()
}
func DoorResetClose() {
	DoorReset()
	FrontDoor.Translate([0, -2.2, 0], 1, Instant).Rotate([1, 0, 0], -90, 1, Instant)
	RightDoor.Translate([0, -3.44644, 0], 1, Instant).Rotate([0, 0, 1], -90, 1, Instant)
	BackDoor.Translate([0, -2.2, 0], 1, Instant).Rotate([1, 0, 0], 90, 1, Instant)
	LeftDoor.Translate([0, -3.44644, 0], 1, Instant).Rotate([0, 0, 1], 90, 1, Instant)
	Barrel.Translate([0, 0, -1.7], 0, Instant)
	Camera.Rotate([ 1, 0, 0], 10, 0, Instant)
}


# Animations
func GunTurnOn() {
	if (isWorking == false) {
		#DoorResetClose()
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(250).SetColor(10, 255, 0, 1.0, false)

		FrontDoor.delay(0).Rotate([1, 0, 0], 90, 90, Linear).delay(90).Translate([0, 3.0, 0], 60, Linear)
		RightDoor.delay(10).Rotate([0, 0, 1], 90, 90, Linear).delay(90).Translate([0, 5.5, 0], 60, Linear)
		BackDoor.delay(20).Rotate([1, 0, 0], -90, 90, Linear).delay(90).Translate([0, 3.0, 0], 60, Linear)
		LeftDoor.delay(30).Rotate([0, 0, 1], -90, 90, Linear).delay(90).Translate([0, 5.5, 0], 60, Linear)

		Elevator.delay(80).Translate([0, -7, 0], 100, Linear)
		Barrel.delay(160).Translate([0, 0, 1.7], 90, Linear)
		Camera.delay(160).Rotate([ 1, 0, 0], -10, 90, Linear)
	}
	isWorking = true
}
func GunTurnOff() {
	if (isWorking == true) {
		#DoorReset()
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(250).SetColor(255, 0, 0, 1.0, false)

		FrontDoor.delay(30).Translate([0, -3.0, 0], 100, Linear).delay(100).Rotate([1, 0, 0], -90, 90, Linear)
		RightDoor.delay(40).Translate([0, -5.5, 0], 100, Linear).delay(100).Rotate([0, 0, 1], -90, 90, Linear)
		BackDoor.delay(50).Translate([0, -3.0, 0], 100, Linear).delay(100).Rotate([1, 0, 0], 90, 90, Linear)
		LeftDoor.delay(60).Translate([0, -5.5, 0], 100, Linear).delay(100).Rotate([0, 0, 1], 90, 90, Linear)

		CannonAZ.MoveToOrigin(120, InOutSine)
		CannonEL.MoveToOrigin(120, InOutSine)
		Elevator.delay(80).Translate([0, 7, 0], 100, Linear)
		Barrel.Translate([0, 0, -1.7], 60, Linear)
		Camera.Rotate([ 1, 0, 0], 10, 90, Linear)
	}
	isWorking = false
}


#Events
Action Block() {
	Built() {
		isWorking = true
	}
	Working() {
		GunTurnOn()
	}
	NotWorking(){
		GunTurnOff()
	}
}

