# Political Survival
Become the ruler of the server, tax all players, and call in helis for support.

# Commands
```
/rinfo or /tax - Displayes information regarding the current ruler, the tax set, and realm name
/settaxchest - While looking either at a woodenbox or toolcupboard, use this command to set it as the tax chest.
/rplayers - Gives current online players
/settax <0-95>- Set the tax with this command, to charge your subjects
/heli <username> - Call a heli on an individual, please note that the heli will attack his entire team as well if in range. 
/claimruler - If there currently is no ruler, you can become ruler by using this command. 
/fnr <username> - Forces a random new ruler that is online, when a username is given, it will make that user the new ruler. (You need to be admin, or the current ruler with the setting enabled in config)

/taxrange min max - this will set the minimum and maximum tax a ruler can set.
```

# Configuration
### Default
```json
{
  "Version": "0.0.2", //please do not change this.
  "showWelcomeMsg": false,
  "maxHelis": 2, // maximum helis allowed in the map at once. 
  "heliItemCost": 13994, //The itemID of the item to charge, default is high quality metal
  "heliItemCostQty": 500, // How many should be charged of the item
  "broadcastRulerPosition": false, //Shold the rulers position be broadcasted? 
  "broadcastRulerPositionAfter": 60, //After how many seconds should it be broadcasted?
  "broadcastRulerPositionAfterPercentage": 10, //Broadcast ruler position after which tax percentage
  "taxMin": 0,
  "taxMax": 35,
  "taxSource": { // the below sets if the specific source should be taxed
    "DispenserGather": true,
    "CropGather": true,
    "DispenserBonus": true,
    "QuarryGather": true,
    "ExcavatorGather": true,
    "CollectiblePickup": true,
    "SurveyGather": true,
	"worldSize": 3500,
    "chooseNewRulerOnDisconnect": true,
    "chooseNewRulerOnDisconnectMinutes": 60,
    "rulerCanChooseAnotherRuler": true
  }
}
```
