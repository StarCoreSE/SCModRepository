@BlockID "APE_Strong"
@Version 2
@Author Krynoc
@Weaponcore 0

#Valid subpart ID's of APE_Strong
#|  APESAZZ
#|  |  APESElevationn
#|  APES_Door3F
#|  APES_Door3B
#|  APES_Door2S
#|  APES_Door1S

# Declarations
using Azimuth as Subpart("APESAZZ")
using Elevation as Subpart("APESElevationn") parent Azimuth

using DoorFront as Subpart("APES_Door3F")
using DoorBack as Subpart("APES_Door3B")
using DoorSide1 as Subpart("APES_Door1S")
using DoorSide2 as Subpart("APES_Door2S")

using BeaconEmissive as Emissive("Emissive")

var isWorking = true


# Reset Functions
func DoorReset() {
	API.StopDelays()
	DoorFront.reset()
	DoorBack.reset()
	DoorSide1.reset()
	DoorSide2.reset()
}
func DoorResetClose() {
	API.StopDelays()	
	DoorFront.reset().Translate([0, -1.7, 0], 1, Instant).Rotate([1, 0, 0], -90, 1, Instant)
	DoorBack.reset().Translate([0, -1.7, 0], 1, Instant).Rotate([1, 0, 0], 90, 1, Instant)
	DoorSide1.reset().Translate([0, -3.13, 0], 1, Instant).Rotate([0, 0, 1], 90, 1, Instant)
	DoorSide2.reset().Translate([0, -3.13, 0], 1, Instant).Rotate([0, 0, 1], -90, 1, Instant)
}

# Animations
func GunTurnOn() {
	if (isWorking == false) {
		DoorResetClose()
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(150).SetColor(10, 255, 0, 1.0, false)

		Azimuth.reset().Translate([0, 8.1, 0], 0, Instant).delay(30).Translate([0, -8.1, 0], 120, Linear)

		DoorFront.Rotate([1, 0, 0], 90, 90, Linear).delay(90).Translate([0, 1.7, 0], 60, Linear)
		DoorBack.Rotate([1, 0, 0], -90, 90, Linear).delay(90).Translate([0, 1.7, 0], 60, Linear)
		DoorSide1.Rotate([0, 0, 1], -90, 90, Linear).delay(90).Translate([0, 3.13, 0], 60, Linear)
		DoorSide2.Rotate([0, 0, 1], 90, 90, Linear).delay(90).Translate([0, 3.13, 0], 60, Linear)
	}
	isWorking = true
}
func GunTurnOff() {
	if (isWorking == true) {
		DoorReset()
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(250).SetColor(255, 0, 0, 1.0, false)

		Azimuth.MoveToOrigin(50, InOutSine)
		Elevation.MoveToOrigin(90, InOutSine)

		Azimuth.delay(50).reset().Translate([0, 8.1, 0], 100, Linear)

		DoorFront.delay(50).Translate([0, -1.7, 0], 100, Linear).delay(100).Rotate([1, 0, 0], -90, 100, Linear)
		DoorBack.delay(50).Translate([0, -1.7, 0], 100, Linear).delay(100).Rotate([1, 0, 0], 90, 100, Linear)
		DoorSide1.delay(50).Translate([0, -3.13, 0], 100, Linear).delay(100).Rotate([0, 0, 1], 90, 100, Linear)
		DoorSide2.delay(50).Translate([0, -3.13, 0], 100, Linear).delay(100).Rotate([0, 0, 1], -90, 100, Linear)
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


func BeaconReady() { BeaconEmissive.SetColor(10, 255, 0, 1.0, false) }
func BeaconReload() { BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(30).SetColor(10, 255, 0, 1.0, false) }
func BeaconNoMags() { BeaconEmissive.SetColor(255, 0, 0, 1.0, false) }
func BeaconOverheat() { BeaconEmissive.SetColor(255, 103, 0, 1.0, false).delay(30).SetColor(255, 255, 0, 1.0, false) }

Action Weaponcore() {
	Reloading(){
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



