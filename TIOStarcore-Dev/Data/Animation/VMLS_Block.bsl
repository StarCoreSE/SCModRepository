@BlockID "VMLS_Block"
@Version 2
@Author Killerbee77
@Weaponcore 0



# Declarations

using BeaconEmissive as Emissive("Emissive")

var isWorking = true






# Animations
func GunTurnOn() {
	if (isWorking == false) {

		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(0).SetColor(10, 255, 0, 1.0, false)

	}
	isWorking = true
}
func GunTurnOff() {
	if (isWorking == true) {
		
		BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(0).SetColor(255, 0, 0, 1.0, false)

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
func BeaconReload() { BeaconEmissive.SetColor(255, 255, 0, 1.0, false).delay(1800).SetColor(10, 255, 0, 1.0, false) }
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
		API.StartLoop("BeaconOverheat", 60, 18)
		API.StartLoop("BeaconReady", 1, 1, 1080)
	}
}

#Red	SetColor(255, 0, 0, 1.0, false)
#Yellow	SetColor(255, 255, 0, 1.0, false)
#Orange	SetColor(255, 103, 0, 1.0, false)
#Green	SetColor(10, 255, 0, 1.0, false)