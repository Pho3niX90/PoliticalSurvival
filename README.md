# PoliticalSurvival
Become the ruler of the server, tax all players, and call in helis for support.

# Commands
```
/rinfo or /tax - Displayes information regarding the current ruler, the tax set, and realm name
/settaxchest - While looking either at a woodenbox or toolcupboard, use this command to set it as the tax chest.
/settax <0-95>- Set the tax with this command, to charge your subjects
/heli <username> - Call a heli on an individual, please note that the heli will attack his entire team as well if in range. 
/claimruler - If there currently is no ruler, you can become ruler by using this command. 
/fnr <username> - Forces a random new ruler that is online, when a username is given, it will make that user the new ruler. 
```

# Settings
The settings can be found in the data folder, this will be corrected in a future version. 

```
{
  "taxMin": 0, // The minimum tax that can be set. 
  "taxMax": 35, // The maximum tax that can be set.
  "maxHelis": 2, // Maximum helis that can be called at a time.
  "heliItemCost": 317398316, // Heli item ID cost.
  "heliItemCostQty": 250, // The amount required of the item
  "broadcastRulerPosition": false, // Should the rulers position be broadcasted? 
  "broadcastRulerPositionAfter": 0, // Broadcast the position after how many seconds. 
  "broadcastRulerPositionAfterPercentage": 10 // Broadcast theposition when tax is more than this
}
```
