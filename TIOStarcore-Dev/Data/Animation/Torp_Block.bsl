@BlockID "Torp_Block"
@Version 2
@Author Krynoc
@Weaponcore 0

#Valid subpart ID's of Torp_Block
#|  TorpReaperRightDoor
#|  TorpReaperLeftDoor
#|  TorpReaperTorpLift
#|  TorpReaperAZZ
#|  |  TorpReaperElevationn
#|  |  |  TorpReaperRightPod
#|  |  |  |  TorpReaperTorp1
#|  |  |  |  TorpReaperTorp2
#|  |  |  |  TorpReaperTorp3
#|  |  |  TorpReaperLeftPod
#|  |  |  |  TorpReaperTorp4
#|  |  |  |  TorpReaperTorp5
#|  |  |  |  TorpReaperTorp6

# Declarations
using Elevator as Subpart("TorpReaperTorpLift")
using Azimuth as Subpart("TorpReaperAZZ")
using Elevation as Subpart("TorpReaperElevationn") parent Azimuth

using PodLeft as Subpart("TorpReaperLeftPod") parent Elevation
using PodRight as Subpart("TorpReaperRightPod") parent Elevation

using DoorLeft as Subpart("TorpReaperLeftDoor")
using DoorRight as Subpart("TorpReaperRightDoor")

using BeaconEmissive as Emissive("Emissive")

var isWorking = true


# Reset Functions
func DoorReset() {
	API.StopDelays()
	DoorLeft.reset()
	DoorRight.reset()
	PodRight.reset()
	PodLeft.reset()
}
func DoorResetClose() {
	API.StopDelays()
	Elevator.reset().Translate([0, 9, 0], 0, Instant)
	Azimuth.reset().Translate([0, 9, 0], 0, Instant)
	PodRight.reset().Rotate([0, 0, 1], 45, 0, Instant)
	PodLeft.reset().Rotate([0, 0, 1], -45, 0, Instant)

	DoorRight.reset().Translate([0, -6.3, 0], 0, Instant).Rotate([0, 0, 1], -90, 0, Instant)
	DoorLeft.reset().Translate([0, -6.3, 0], 0, Instant).Rotate([0, 0, 1], 90, 0, Instant)
}

# Animations
func GunTurnOn() {
	if (isWorking == false) {
		DoorResetClose()
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(240).SetColor(10, 255, 0, 1.0, false)

		PodRight.delay(160).Rotate([0, 0, 1], -45, 80, Linear)
		PodLeft.delay(160).Rotate([0, 0, 1], 45, 80, Linear)

		Elevator.delay(50).Translate([0, -9, 0], 150, Linear)
		Azimuth.delay(50).Translate([0, -9, 0], 150, Linear)

		DoorRight.Rotate([0, 0, 1], 90, 100, Linear).delay(100).Translate([0, 6.3, 0], 100, Linear)
		DoorLeft.Rotate([0, 0, 1], -90, 100, Linear).delay(100).Translate([0, 6.3, 0], 100, Linear)
	}
	isWorking = true
}
func GunTurnOff() {
	if (isWorking == true) {
		DoorReset()
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(260).SetColor(255, 0, 0, 1.0, false)

		PodRight.Rotate([0, 0, 1], 45, 100, Linear)
		PodLeft.Rotate([0, 0, 1], -45, 100, Linear)

		Elevator.MoveToOrigin(50, InOutSine).delay(50).reset().Translate([0, 9, 0], 150, Linear)
		Azimuth.MoveToOrigin(50, InOutSine).delay(50).reset().Translate([0, 9, 0], 150, Linear)
		Elevation.MoveToOrigin(90, InOutSine)

		DoorRight.delay(60).Translate([0, -6.3, 0], 100, Linear).delay(100).Rotate([0, 0, 1], -90, 100, Linear)
		DoorLeft.delay(60).Translate([0, -6.3, 0], 100, Linear).delay(100).Rotate([0, 0, 1], 90, 100, Linear)
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






