function CommandGui::onOpen()
{
	//initialize the commander buttons

	if ($pref::mapFilter & 0x0001) Control::setValue(IDCTG_CMDO_PLAYERS, "TRUE");
	else Control::setValue(IDCTG_CMDO_PLAYERS, "FALSE");

	if ($pref::mapFilter & 0x0002) Control::setValue(IDCTG_CMDO_TURRETS, "TRUE");
	else Control::setValue(IDCTG_CMDO_TURRETS, "FALSE");

	if ($pref::mapFilter & 0x0004) Control::setValue(IDCTG_CMDO_ITEMS, "TRUE");
	else Control::setValue(IDCTG_CMDO_ITEMS, "FALSE");

	if ($pref::mapFilter & 0x0008) Control::setValue(IDCTG_CMDO_OBJECTS, "TRUE");
	else Control::setValue(IDCTG_CMDO_OBJECTS, "FALSE");

	if (String::ICompare($pref::mapSensorRange, "TRUE") == 0) Control::setValue(IDCTG_CMDO_RADAR, "TRUE");
	else Control::setValue(IDCTG_CMDO_RADAR, "FALSE");

	if (String::ICompare($pref::mapNames, "TRUE") == 0) Control::setValue(IDCTG_CMDO_EXTRA, "TRUE");
	else Control::setValue(IDCTG_CMDO_EXTRA, "FALSE");

   Control::setValue("TVButton", false);
}

function remoteIssueCommand(%commander, %cmdIcon, %command, %wayX, %wayY,
      %dest1, %dest2, %dest3, %dest4, %dest5, %dest6, %dest7, %dest8, %dest9, %dest10, %dest11, %dest12, %dest13, %dest14)
{
   if($dedicated)
      echo("COMMANDISSUE: " @ %commander @ " \"" @ String::Escape(%command) @ "\"");
   // issueCommandI takes waypoint 0-1023 in x,y scaled mission area
   // issueCommand takes float mission coords.
   for(%i = 1; %dest[%i] != ""; %i = %i + 1) {
      if(!%dest[%i].muted[%commander])
         issueCommandI(%commander, %dest[%i], %cmdIcon, %command, %wayX, %wayY);
   }
}

function remoteIssueTargCommand(%commander, %cmdIcon, %command, %targIdx, 
      %dest1, %dest2, %dest3, %dest4, %dest5, %dest6, %dest7, %dest8, %dest9, %dest10, %dest11, %dest12, %dest13, %dest14)
{
   if($dedicated)
      echo("COMMANDISSUE: " @ %commander @ " \"" @ String::Escape(%command) @ "\"");
   for(%i = 1; %dest[%i] != ""; %i = %i + 1) {
   	  %dest = %dest[%i];
      if(!%dest.muted[%commander])
         issueTargCommand(%commander, %dest, %cmdIcon, %command, %targIdx);
   }
}

function remoteCStatus(%clientId, %status, %message)
{
   // setCommandStatus returns false if no status was changed.
   // in this case these should just be team says.
   if(setCommandStatus(%clientId, %status, %message))
   {
      if($dedicated)
         echo("COMMANDSTATUS: " @ %clientId @ " \"" @ String::Escape(%message) @ "\"");
   }
   else
      remoteSay(%clientId, true, %message);
}


function Client::takeControl(%clientId, %objectId) {
   // remote control
   if(%objectId == -1)
   {
      //echo("objectId = " @ %objectId);
      return;
   }

	%pl = Client::getOwnedObject(%clientId);
	// If mounted to a vehicle then can't mount any other objects
	if(%pl.driver != "" || %pl.vehicleSlot != "")
		return;

   if(GameBase::getTeam(%objectId) != Client::getTeam(%clientId))
   {
      //echo(GameBase::getTeam(%objectId) @ " " @ Client::getTeam(%clientId));
      return;
   }
   if(GameBase::getControlClient(%objectId) != -1)
   {
      echo("Ctrl Client = " @ GameBase::getControlClient(%objectId));
      return;
   }
	%name = GameBase::getDataName(%objectId);
	if(%name != CameraTurret && %name != DeployableTurret)
   {
	   if(!GameBase::isPowered(%objectId)) 
		{
	      // echo("Turret " @ %objectId @ " not powered.");
	      return;
		}
   }
   if(!(Client::getOwnedObject(%clientId)).CommandTag && GameBase::getDataName(%objectId) != CameraTurret &&
      !$TestCheats) {
		Client::SendMessage(%clientId,0,"Must be at a Command Station to control turrets");
   		return;
   }
   if(GameBase::getDamageState(%objectId) == "Enabled") {
   	Client::setControlObject(%clientId, %objectId);
   	Client::setGuiMode(%clientId, $GuiModePlay);
	}
}

function remoteCmdrMountObject(%clientId, %objectIdx) {
   Client::takeControl(%clientId, getObjectByTargetIndex(%objectIdx));
}

function checkControlUnmount(%clientId) {
   %ownedObject = Client::getOwnedObject(%clientId);
   %ctrlObject = Client::getControlObject(%clientId);
   if(%ownedObject != %ctrlObject)
   {
      if(%ownedObject == -1 || %ctrlObject == -1)
         return;
      if(getObjectType(%ownedObject) == "Player" && Player::getMountObject(%ownedObject) == %ctrlObject)
         return;
      Client::setControlObject(%clientId, %ownedObject);
   }
}

