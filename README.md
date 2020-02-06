# Political Survival
Become the ruler of the server, tax all players, and call in helis for support.

# Commands
```
/rinfo or /tax - Displayes information regarding the current ruler, the tax set, and realm name
/settaxchest - While looking either at a woodenbox or toolcupboard, use this command to set it as the tax chest.
/settax <0-95>- Set the tax with this command, to charge your subjects
/heli <username> - Call a heli on an individual, please note that the heli will attack his entire team as well if in range. 
/claimruler - If there currently is no ruler, you can become ruler by using this command. 
/fnr <username> - Forces a random new ruler that is online, when a username is given, it will make that user the new ruler. 

/taxrange min max - this will set the minimum and maximum tax a ruler can set.
```

# Configuration
### Default
```json
{
  "broadcastRulerPosition": false, //Shold the rulers position be broadcasted? 
  "broadcastRulerPositionAfter": 500, //After how many seconds should it be broadcasted?
  "broadcastRulerPositionAfterPercentage": 10, //Broadcast ruler position after which tax percentage
  "heliItemCost": 13994, //The itemID of the item to charge, default is high quality metal
  "heliItemCostQty": 500, // How many should be charged of the item
  "maxHelis": 2, // maximum helis allowed in the map at once. 
  "showWelcomeMsg": false,
  "Version": "0.0.1" //please do not change this.
}
```
