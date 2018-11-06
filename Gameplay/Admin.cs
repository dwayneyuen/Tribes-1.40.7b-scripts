//////////////////////////////////////////////////////////////////////////////////////
// ADMIN/TAB MENUS AND FUNCTIONS
// ...FOR BASE MOD
//------------------------------//
$menu::maxitems = 7;

$vote::Topic = "";
$vote::Action = "";
$vote::Option = "";
$vote::Count = 0;

function remoteSetPassword( %cl, %password ) {
	if ( !%cl.canSetPassword )
		return;
	$Server::Password = ( %password );
	log::add( %cl, "changed the password to" @ %password, "", "", "ServerPasswords" );
}

function admins::reset() {
	deletevariables( "$admin::admins*" );
	$admin::admins = 0;
}

function admin::addadmin( %registeredname, %password, %level ) {
	$admin::admins[ %password ] = true;
	$admin::admins[ %password, level ] = adminlevel::get( %level );
	$admin::admins[ %password, name ] = %registeredname;
	
	$admin::adminlist[ $admin::admins ] = %password;
	$admin::admins++;
}

function admin::findduplicateusers( %cl ) {
	%list[ 0 ] = %cl;
	%count = 0;
	
	if ( %cl.password == "" || %cl.password == "NOPASSWORD" )
		return;

	for ( %cl2 = Client::getFirst(); %cl2 != -1; %cl2 = Client::getNext( %cl2 ) ) {
		if ( ( %cl == %cl2 ) || ( %cl.password != %cl2.password ) )
			continue;

		%list[ %count++ ] = %cl2;
	}

	if ( %count ) {
		%alert = %cl.registeredName @ "\'s password is in use by : " @ String::escapeFormatting(Client::getName(%cl));
		for ( %i = 1; %i < %count; %i++ )
			%alert = %alert @ " & " @ String::escapeFormatting(Client::getName( %list[ %i ] ));
		admin::alert( %alert );
		log::add( -2, %alert, "", "", "Admins" );
	}
}

// owner ips
deletevariables( "$admin::owner*" );
$admin::owner[ "IP:127" ] = true;
$admin::owner[ "LOOPBA" ] = true;

function remoteAdminPassword( %cl, %password ) {
	%oldLevel = %cl.adminLevel;

	%owner = $admin::owner[ string::getsubstr( Client::getTransportAddress( %cl ), 0, 6 ) ];
	%valid = ( ( %password != "" ) && ( $admin::admins[ %password ] != "" ) );
	if ( %owner && ( %password != "" ) && !%valid )
		%owner = false;

	if ( !%owner && !%valid ) {
		%cl.registeredName = "";
		%cl.password = "";
		adminlevel::grant( %cl, 0 );
		return;
	 }

	adminlevel::grant( %cl, $admin::admins[ %password, level ] );
	%cl.registeredName = $admin::admins[ %password, name ];
	%cl.password = %password;

	schedule( "admin::findduplicateusers(" @ %cl @ ");", 5 );  //wait 5 seconds so we don't override the "has logged in" message sent to Uadmins.

	if (%cl.canSeePlayerlist)
		admin::dumpplayerlist(%cl);

	// allow admin to relogin to see player list without broadcasting alert or logging
	if ( %oldLevel != %cl.adminLevel ) {
		admin::alert( String::escapeFormatting(Client::getName(%cl)) @ " has logged in as " @ adminlevel::getname( %cl.adminLevel ) @ " using " @ %cl.registeredName @ "\'s password." );
		log::add( %cl, "activated his/her " @ adminlevel::getname( %cl.adminLevel ) @ " account.", "", "+", "Admins" );
	}
}

function admin::startvote( %cl, %topic, %action, %option ) {
	if( %cl.lastVoteTime == "" )
		%cl.lastVoteTime = -$Server::MinVoteTime;

	// we want an absolute time here.
	%time = getIntegerTime(true) >> 5;
	%diff = ( %cl.lastVoteTime + $Server::MinVoteTime ) - %time;

	if ( %diff > 0 ) {
		Client::sendMessage(%cl, 0, "You can't start another vote for " @ floor(%diff) @ " seconds.");
		return;
	}
	
	if( $vote::Topic != "" ) {
		Client::sendMessage( %cl, 0, "Voting already in progress." );
		return;
	}
	
	if ( $dedicated )
		echo( "VOTE INITIATED: " @ Client::getName(%cl) @ " initiated a vote to " @ %topic );

	if( %cl.numFailedVotes )
		%time += %cl.numFailedVotes * $Server::VoteFailTime;

	%cl.lastVoteTime = %time;
	$vote::Initiator = %cl;
	$vote::Topic = %topic;
	$vote::Action = %action;
	$vote::Option = %option;
	if(%action == "kick")
		$vote::Option.kickTeam = GameBase::getTeam($vote::Option);

	$vote::Count++;
	message::bottomprintall( "<jc><f1>" @ String::escapeFormatting(Client::getName(%cl)) @ " <f0>initiated a vote to <f1>" @ $vote::Topic, 10 );

	// reset everyone's votes
	for( %cl2 = Client::getFirst(); %cl2 != -1; %cl2 = Client::getNext(%cl2) )
		%cl2.vote = "";
	
	// vote initiator votes yes!
	%cl.vote = "yes";
	for( %cl2 = Client::getFirst(); %cl2 != -1; %cl2 = Client::getNext(%cl2) )
		if( %cl2.menuMode == "options" )
			Game::menuRequest(%cl2);

	schedule( "admin::tallyvotes(" @ $vote::Count @ ");", $Server::VotingTime, 35 );
}


function admin::tallyvotes( %curvote ) {
	if ( ( %curvote != $vote::Count ) || ( $vote::Topic == "" ) )
		return;

	%for["yes"] = 1;
	%against["no"] = 1;
	%abstain[""] = 1;
	
	%for_team = %against_team = 0;
	%for = %against = %abstain = 0;
	for( %cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl) ) {
		%for += %for[ %cl.vote ];
		%against += %against[ %cl.vote ];
		%abstain += %abstain[ %cl.vote ];
		if ( GameBase::getTeam(%cl) == $vote::Option.kickTeam ) {
			%for_team += %for[ %cl.vote ];
			%against_team += %against[ %cl.vote ];
		}
   }

   %votes = ( %for + %against );
   %votes_team = ( %for_team + %against_team );
   %clients = ( %votes + %abstain );

   %minvotes = floor( $Server::MinVotesPct * %clients );
   if( %minvotes < $Server::MinVotes )
      %minvotes = $Server::MinVotes;

   if ( %votes < %minVotes ) {
      %against += ( %minVotes - %votes );
      %votes = %minVotes;
   }
   
   %margin = $Server::VoteWinMargin;
   if ( $vote::Action == "admin" ) {
      %margin = $Server::VoteAdminWinMargin;
      %votes = %for + %against + %abstain;
      if( %votes < %minVotes )
         %votes = %minVotes;
   }
   
   if( ( %for / %votes  ) >= %margin ) {
      message::all( 0, "Vote to " @ $vote::Topic @ " passed: " @ %for @ " to " @ %against @ " with " @ %abstain @ " abstentions." );
      admin::votepassed();
   } else {
   	   // recheck team votes for kicking
      if($vote::Action == "kick") {
         if( ( %votes_team >= $Server::MinVotes ) && ( ( %for_team / %votes_team ) >= $Server::VoteWinMargin ) ) {
            message::all(0, "Vote to " @ $vote::Topic @ " passed: " @ %for_team @ " to " @ %votes_team - %for_team @ ".");
            admin::votepassed();
            $vote::Topic = "";
            return;
         }
      }
      message::all(0, "Vote to " @ $vote::Topic @ " did not pass: " @ %for @ " to " @ %against @ " with " @ %abstain @ " abstentions." );
      admin::votefailed();
   }
   $vote::Topic = "";
}


function admin::votefailed() {
	$vote::Initiator.numVotesFailed++;

	if( $vote::Action == "kick" || $vote::Action == "admin" )
		$vote::Option.voteTarget = "";
}


function admin::votepassed() {
	if ( $vote::Action == "kick" ) {
		if ( $vote::Option.voteTarget )
			admin::kick( -1, $vote::Option );
	} else if ( $vote::Action == "admin" ) {
		if ( $vote::Option.voteTarget ) {
			log::add( -1, "adminned", $vote::Option, "", "Admins" );
			adminlevel::grant( $vote::Option, adminlevel::get("Public Admin") );
			$vote::Option.registeredName = "Admin by vote";

			message::all( 0, Client::getName($vote::Option) @ " has become an administrator." );
			if ( $vote::Option.menuMode == "options" )
				Game::menuRequest( $vote::Option );
		}
	
		$vote::Option.voteTarget = false;
	} else if ( $vote::Action == "changeMission" ) {
		message::all( 0, "Changing to mission " @ $vote::Option @ "." );
		ObjectiveMission::missionComplete( $vote::Option );
	} else if ( $vote::Action == "tourney" ) {
		admin::settourneymode( -1 );
	} else if ( $vote::Action == "ffa" ) {
		admin::setffamode( -1 );
	} else if ( $vote::Action == "etd" ) {
		admin::setteamdamage( -1, true );
	} else if ( $vote::Action == "dtd" ) {
		admin::setteamdamage( -1, false );
	} else if ( $vote::Option == "smatch" ) {
		admin::startmatch( -1 );
	}
}


function remoteVoteYes( %cl ) {
   %cl.vote = "yes";
   message::centerprint( %cl, "", 0 );
}

function remoteVoteNo( %cl ) {
   %cl.vote = "no";
   message::centerprint( %cl, "", 0 );
}


//////////////////////////////////////////////////////////////////////////////////////
function menu::self( %cl ) {
	menu::new( "Options", "self", %cl );
	menu::add( "Change Teams", "changeteams", %cl, (!$loadingMission) && (!$matchStarted || !$Server::TourneyMode) );
	//menu::add( "Vote to admin yourself", "vadminself", %cl, !%cl.adminLevel );
}

function processMenuSelf( %cl, %selection ) {
    if(%selection == "changeteams") {
        menu::ChangeTeams(%cl);
    } else if ( %selection == "vadminself" ) {
         %cl.voteTarget = true;
         admin::startvote(%cl, "admin " @ Client::getName(%cl), "admin", %cl);
		 Game::menuRequest(%cl);
    }
}

function menu::nonself( %cl ) {
	%sel = %cl.selClient;
	%selName = Client::getName( %sel );

	if( %cl.canBan )
		%kickMsg = "Kick or Ban ";
	else
		%kickMsg = "Kick ";

	menu::new( "Options", "nonself", %cl );
	//menu::add( "Vote to admin " @ %selName, "vadmin " @ %sel, %cl, !%cl.canMakeAdmin );
	menu::add( "Vote to kick " @ %selName, "vkick " @ %sel, %cl, !%cl.canKick );
	menu::add( %kickMsg @ %selName, "kickban " @ %sel, %cl, %cl.canKick );
	menu::add( "Message " @ %selName, "message " @ %sel, %cl, %cl.canSendWarning );
	menu::add( "Change " @ %selName @ "'s team", "fteamchange " @ %sel, %cl, %cl.canChangePlyrTeam );
	menu::add( "Admin " @ %selName, "admin " @ %sel, %cl, %cl.canMakeAdmin );
	menu::add( "Strip " @ %selName, "stradmin " @ %sel, %cl, ( %cl.canStripAdmin && %sel.adminLevel > 0 ) );
	menu::add( "Observe " @ %selName, "observe " @ %sel, %cl, ( %cl.observerMode == "observerOrbit" ) );
	menu::add( "UnMute " @ %selName, "unmute " @ %sel, %cl, %cl.muted[%sel] != "" );
	menu::add( "Mute " @ %selName, "mute " @ %sel, %cl, %cl.muted[%sel] == "" );
}


function processMenuNonSelf( %cl, %selection ) {
    %option = getWord( %selection, 0 );
    %vic = getWord( %selection, 1 );

	if (%option == "message") {
	     menu::MessagePlayer( %cl, %vic );
		 return;
	}    
    else if (%option == "admin") {
    	 menu::makeadmin(%cl, %vic);
    	 return;         
    }   
    else if (%option == "stradmin") {
    	 menu::StripAdmin(%cl, %vic);
    	 return;
	}
    else if (%option == "kickban") {
	     menu::kickban( %cl, %vic );
    	 return;
	}
	else if (%option == "fteamchange") {
    	 menu::ForceTeamChange(%cl, %vic);
    	 return;
    }
    else if (%option == "vkick") {
         %vic.voteTarget = true;
         admin::startvote(%cl, "kick " @ Client::getName(%vic), "kick", %vic);
		 Game::menuRequest( %cl );
    }
    else if (%option == "vadmin") {
         %vic.voteTarget = true;
         admin::startvote(%cl, "admin " @ Client::getName(%vic), "admin", %vic);
		 Game::menuRequest( %cl );
    }
    else if ( %option == "observe" ) {
         Observer::setTargetClient(%cl, %vic);
         return;
    } else if ( %option == "mute" ) {
         %cl.muted[%vic] = true;
    } else if ( %option == "unmute" ) {
         %cl.muted[%vic] = "";
    }

    Game::menuRequest( %cl );
}

function remoteSelectClient( %cl, %selId ) {
	if( %cl.selClient != %selId ) {
		%cl.selClient = %selId;
		Game::menuRequest( %cl );

		if ( %selId.registeredName == "" )
			%selId.registeredName = "Unknown";
		if( !%selId.adminLevel )
			%selId.adminLevel = 0;

		remoteEval( %cl, "setInfoLine", 1, "Player Info for " @ Client::getName(%selId) @ ":" );

		if( %cl.canSeePlayerSpecs ) {
			remoteEval( %cl, "setInfoLine", 2, "Admin Status: " @ adminlevel::getname( %selId.adminLevel ) );
			remoteEval( %cl, "setInfoLine", 3, "Name: " @ %selId.registeredName );
			remoteEval( %cl, "setInfoLine", 4, "IP: " @ Client::getTransportAddress(%selId) );
		} else {
			remoteEval(%cl, "setInfoLine", 2, "Real Name: " @ $Client::info[%selId, 1]);
			remoteEval(%cl, "setInfoLine", 3, "Email Addr: " @ $Client::info[%selId, 2]);
			remoteEval(%cl, "setInfoLine", 4, "Other: " @ $Client::info[%selId, 5]);
		}

		if( %cl == %selId ) {
			if ( %cl.canBroadcast ) {
				remoteEval(%cl, "setInfoLine", 5, "");
				remoteEval(%cl, "setInfoLine", 6, "CHAT now Broadcasts message.");
			}
		} else {
			remoteEval(%cl, "setInfoLine", 5, "");
			remoteEval(%cl, "setInfoLine", 6, "CHAT now /pm's " @ Client::getName(%selId) );
		}
	}
}

//////////////////////////////////////////////////////////////////////////////////////
// ADMIN MENU
function menu::admin( %cl ) {
	%waiting = $Server::TourneyMode && (!$CountdownStarted && !$matchStarted);

    menu::new( "Options", "admin", %cl );

	menu::add( "Change Teams", "changeteams", %cl, !$loadingMission );
    menu::add( "Change mission", "changeMission", %cl, %cl.canChangeMission );
	menu::add( "Disable team damage", "dtd", %cl, (%cl.canSwitchTeamDamage && $Server::TeamDamageScale == 1) );
	menu::add( "Enable team damage", "etd", %cl, (%cl.canSwitchTeamDamage && !$Server::TeamDamageScale == 1) );
	menu::add( "Change to FFA mode", "cffa", %cl, (%cl.canChangeGameMode && $Server::TourneyMode) );
	menu::add( "Start the match", "smatch", %cl, (%cl.canForceMatchStart && %waiting) );
	menu::add( "Change to Tournament mode", "ctourney", %cl, (%cl.canChangeGameMode && !$Server::TourneyMode) );
	menu::add( "Set Time Limit", "ctimelimit", %cl, %cl.canChangeTimeLimit );
	menu::add( "Announce Server Takeover", "takeovermes", %cl, %cl.canAnnounceTakeover );
	menu::add( "Server Toggles...", "serverToggles", %cl );
	menu::add( "Vote options...", "voteOptions", %cl );
}

function processMenuAdmin( %cl, %selection ) {
	if(%selection == "changeteams") {
		menu::changeteams( %cl );
		return;
	} else if(%selection == "cffa") {
        admin::setffamode(%cl);
    } else if(%selection == "ctourney") {
         admin::settourneymode(%cl);
    } else if(%selection == "smatch") {
         admin::startmatch(%cl);
    } else if(%selection == "changeMission") {
         %cl.madeVote = ""; //for admins initiating mission change votes.
         menu::ChangeMissionType(%cl, 0);
         return;         
    }
    else if(%selection == "ctimelimit") {    
		 menu::TimeLimit(%cl);
         return;		 
    }
    else if(%selection == "takeovermes") {
         menu::AnnounceServerTakeover(%cl);    
    	 return;
	}
	else if(%selection == "etd") {
         admin::setteamdamage(%cl, true);
    } else if(%selection == "dtd") {
         admin::setteamdamage(%cl, false);
    } else if(%selection == "voteOptions") { 
	     menu::Vote(%cl);
		 return;
 	} else if(%selection == "serverToggles") { 
	     menu::ServerToggles(%cl);
		 return;
 	}

    Game::menuRequest(%cl);
}


//////////////////////////////////////////////////////////////////////////////////////
// GIVE CLIENT ADMIN
function menu::makeadmin( %cl, %vic ) {
    menu::new("Bestow Admin", "makeadmin", %cl);

	for ( %i = 1; ( %i < $admin::levels ) && ( %i < %cl.adminLevel ); %i++ )
		menu::add( adminlevel::getname( %i ) @ " " @ Client::getName(%vic), "admin" @ " " @ %i @ " " @ %vic, %cl );
	menu::add( "Cancel ", "cancel " @ %vic, %cl );
}

function processMenuMakeAdmin( %cl, %opt ) {
	%action = getWord( %opt, 0 );
	%level = getWord( %opt, 1 );
	%vic = getWord( %opt, 2 );

	if ( %cl == %vic )
		return;

	if ( ( %action == "admin" ) && ( %cl.adminLevel > %level ) && ( %vic.adminLevel != %level ) ) {
		%vic.password = "NOPASSWORD";
		adminlevel::grant( %vic, %level );

		log::add( %cl, "Adminned", %vic, "", "Admins" );
		%adminabbrev = String::getSubStr( adminlevel::getname( %level ), 0, 1 ) @ "A";
		%vic.registeredName = %adminabbrev @ "." @ %cl.registeredName;

		%recipientMessage = "You are now an admin, courtesy of " @ Client::getName(%cl);
		%adminMessage = "Sent to " @ Client::getName(%vic) @ ": " @ %recipientMessage;

		message::bottomprint(%vic, "<jc>" @ %recipientMessage);
		message::bottomprint(%cl, "<jc>" @ %adminMessage);
		Client::sendMessage(%vic, $MSGTypeSystem, %recipientMessage);
	}

	Game::menuRequest(%cl);
}

//////////////////////////////////////////////////////////////////////////////////////
// SERVER TOGGLES
function menu::servertoggles( %cl ) {
	menu::new( "Server Options", "servertoggle", %cl );
	
	menu::add("Enable Anti-Rape", "ear", %cl, !$Server::AntiRape && %cl.canAntiRape );
	menu::add("Disable Anti-Rape", "dar", %cl, $Server::AntiRape && %cl.canAntiRape );

	menu::add("Enable No-Repair", "enr", %cl, !$Server::NoRepair && %cl.canAntiRepair );
	menu::add("Disable No-Repair", "dnr", %cl, $Server::NoRepair && %cl.canAntiRepair );

	menu::add("Enable Pickup Mode", "epm", %cl, !$Server::PickupMode && %cl.canPickup );
	menu::add("Disable Pickup Mode", "dpm", %cl, $Server::PickupMode && %cl.canPickup );
}

function processMenuServerToggle( %cl, %sel ) {
	if ((%sel == "ear") && %cl.canAntiRape) {
		$Server::AntiRape = true;
		AntiRape::Check();
	} else if ((%sel == "dar") && %cl.canAntiRape) {
		$Server::AntiRape = false;
		AntiRape::Check();
	} else if ((%sel == "enr") && %cl.canAntiRepair) {
		$Server::NoRepair = true;
		AntiRepair::Check();
	} else if ((%sel == "dnr") && %cl.canAntiRepair) {
		$Server::NoRepair = false;
		AntiRepair::Check();
	} else if ((%sel == "epm") && %cl.canPickup) {
		$Server::PickupMode = true;
		$Server::Password = "pickup";
		OverflowCycle(getNumClients());
		
		message::all(0, Client::getName(%cl) @ " ENABLED Pickup Mode! Server Password='pickup'");
		
		log::add( %cl, "enabled PICKUP mode", "", "", "Pickups" );
	} else if ((%sel == "dpm") && %cl.canPickup) {
		$Server::PickupMode = false;
		OverflowCycle(getNumClients());
		
		message::all(0, Client::getName(%cl) @ " DISABLED Pickup Mode.");
		
		log::add( %cl, "disabled PICKUP mode", "", "Pickups" );
	}
	
	menu::servertoggles( %cl );
}


//////////////////////////////////////////////////////////////////////////////////////
// SET TIME LIMIT
function menu::timelimit( %cl ) {
	menu::new("Change Time Limit", "changeTimeLimit", %cl);

	menu::add( "10 minutes", 10, %cl );
	menu::add( "15 minutes", 15, %cl );
	menu::add( "20 minutes", 20, %cl );
	menu::add( "25 minutes", 25, %cl );
	menu::add( "30 minutes", 30, %cl );
	menu::add( "45 minutes", 45, %cl );
	menu::add( "60 minutes", 60, %cl );
	menu::add( "No time limit", 0, %cl );	
}

function processMenuChangeTimeLimit( %cl, %opt ) {
	remoteSetTimeLimit( %cl, %opt );
}

function remoteSetTimeLimit( %cl, %time ) {
	%time = floor( %time );
	if( %time == $Server::timeLimit || (%time != 0 && %time < 1) )
		return;

	if ( !%cl.canChangeTimeLimit )
		return;

	log::add( %cl, "changed time limit to " @ %time, "", "", "TimeChanges" );
	
	$Server::timeLimit = %time;
	if(%time)
		message::all(0, Client::getName(%cl) @ " changed the time limit to " @ %time @ " minute(s).");
	else
		message::all(0, Client::getName(%cl) @ " disabled the time limit.");
}


//////////////////////////////////////////////////////////////////////////////////////
// CHANGE CLIENT TEAM
function menu::forceteamchange( %cl, %vic ) {
    %cl.ptc = %vic;
	
	menu::new( "Force Team Change", "forceteamchange", %cl );

    menu::add( "Observer", -2, %cl );
	if ( $Game::MissionType == "Rabbit" ) {
		menu::add( getTeamName(0), 0, %cl );
	} else {
		menu::add( "Automatic", -1, %cl );

		for( %i = 0; %i < getNumTeams(); %i++ )
			menu::add( getTeamName(%i), %i, %cl );
	}
}

function processMenuForceTeamChange( %cl, %team ) {
	%ptc = %cl.ptc;
    if( %cl.canChangePlyrTeam && %cl.adminlevel >= %ptc.adminLevel )
		processMenuChangeTeams( %ptc, %team, %cl );
    
    %cl.ptc = "";
}

function menu::changeteams( %cl ) {
    menu::new( "Change Teams", "changeteams", %cl );

	menu::add("Observer", -2, %cl);
	menu::add("Automatic", -1, %cl);
	
	if ( $Game::MissionType != "Rabbit" ) {
		for( %i = 0; %i < getNumTeams(); %i++ )
	   		menu::add( getTeamName(%i), %i, %cl );
	}
}

function processMenuChangeTeams( %cl, %team, %admincl ) {
	if ( $loadMission )
		return;

	checkPlayerCash( %cl );

    if ( ( %team != -1 && ( %team == Client::getTeam(%cl) || %team >= getNumTeams() ) ) )
         return;
    
    if ( $Game::MissionType == "Rabbit" && ( %team > -1 ) )
    	return;

    if( %cl.observerMode == "justJoined" ) {
         %cl.observerMode = "";
         message::centerprint(%cl, "");
    }

	if( ( !$matchStarted || !$Server::TourneyMode || %admincl ) && %team == -2 ) {
		if ( Observer::enterObserverMode(%cl) ) {
			%cl.notready = "";

			if(%admincl == "") 
				message::all(0, Client::getName(%cl) @ " became an observer.");
			else
				message::all(0, Client::getName(%cl) @ " was forced into observer mode by " @ Client::getName(%admincl) @ ".");

			Game::resetScores(%cl);	
			Game::refreshClientScore(%cl);
			ObjectiveMission::refreshTeamScores();
		}
		
		return;
	}

	//automatic team
	if (%team == -1) {
		%origteam = Client::getTeam( %cl );
		Game::assignClientTeam( %cl );
		%team = Client::getTeam( %cl );
		if ( %team == %origteam )
			return;
	}

    %player = Client::getOwnedObject( %cl );
	if ( %player != -1 && getObjectType(%player) == "Player" && !Player::isDead(%player) ) {
		playNextAnim( %cl );
		Player::kill( %cl );
	}

    %cl.observerMode = "";

    if(%admincl == "")
         message::all(0, Client::getName(%cl) @ " changed teams.");
    else
         message::all(0, Client::getName(%cl) @ " was teamchanged by " @ Client::getName(%admincl) @ ".");

    GameBase::setTeam( %cl, %team );
    Event::Trigger( eventServerClientJoinTeam, %cl, %team );
    %cl.teamEnergy = 0;
	Client::clearItemShopping( %cl );
	if ( Client::getGuiMode(%cl) != 1 )
		 Client::setGuiMode( %cl,1 );
	Client::setControlObject( %cl, -1 );

    Game::playerSpawn( %cl, false );
	%team = Client::getTeam( %cl );
	if ( $TeamEnergy[%team] != "Infinite" )
		 $TeamEnergy[%team] += $InitialPlayerEnergy;
    if ( $Server::TourneyMode && !$CountdownStarted ) {
         message::bottomprint(%cl, "<f1><jc>Press FIRE when ready.", 0);
         %cl.notready = true;
    }

    ObjectiveMission::refreshTeamScores();
}


//////////////////////////////////////////////////////////////////////////////////////
// KICK/BAN CLIENT
function menu::kickban( %cl, %vic ) {
    menu::new( "Boot " @ Client::getName(%vic), "kick", %cl );

	menu::add( "Kick " @ Client::getName(%vic), "kick " @ %vic, %cl, %cl.canKick );
	menu::add( "Ban " @ Client::getName(%vic), "ban " @ %vic, %cl, %cl.canBan );
	menu::add( "PermBan " @ admin::parseip(%vic, 4, 18, true), "fullIP " @ %vic, %cl, %cl.canPermanentBan );
	menu::add( "PermBan " @ admin::parseip(%vic, 3, 14, true), "threeOctet " @ %vic, %cl, %cl.canPermanentBan );
	menu::add( "PermBan " @ admin::parseip(%vic, 2, 10, true), "twoOctet " @ %vic, %cl, %cl.canPermanentBan );
	menu::add( "Cancel ", "cancel", %cl );
}

function processMenuKick( %cl, %opt ) {
	%action = getWord( %opt, 0 );
	%vic = getWord( %opt, 1 );
	 
	if (%action == "cancel") {
	   Game::menuRequest( %cl );
	   return;
	}
		
	menu::new( "Boot " @ Client::getName(%vic) @ ", you sure?", "kick4real", %cl );

	menu::add( "Kick " @ Client::getName(%vic), %opt @ " yes", %cl, %action == "kick" );
	menu::add( "Ban " @ Client::getName(%vic), %opt @ " yes", %cl, %action == "ban" );
	menu::add( "PermBan " @ admin::parseip(%vic, 4, 18, true), %opt @ " yes", %cl, %action == "fullIP" );
	menu::add( "PermBan " @ admin::parseip(%vic, 3, 14, true), %opt @ " yes", %cl, %action == "threeOctet" );
	menu::add( "PermBan " @ admin::parseip(%vic, 2, 10, true), %opt @ " yes", %cl, %action == "twoOctet" );
	menu::add( "Cancel ", %opt @ " cancel", %cl );
}

function processMenuKick4Real( %cl, %opt ) {
	%action = getWord( %opt, 0 );
	%recipient = getWord( %opt, 1 );
	%affirm = getWord( %opt, 2 );

	if ( %affirm != "yes" ) {
		Game::menuRequest(%cl);
		return;
	}

	if (%action == "kick")
		admin::kick( %cl, %recipient, false );
	else if(%action == "ban")
		admin::kick( %cl, %recipient, true );
	else if (%action == "fullIP")
		banlist::permaban( %cl, %recipient, 4, 18, false );
	else if (%action == "threeOctet")	   
		banlist::permaban( %cl, %recipient, 3, 14, false );
	else if (%action == "twoOctet")
		banlist::permaban( %cl, %recipient, 2, 10, false );

	Game::menuRequest(%cl);
}


//////////////////////////////////////////////////////////////////////////////////////
// CHANGE MISSION MENU
function menu::changemissiontype( %cl, %listStart ) {
	menu::new("Pick Mission Type", "changeMissionType", %cl);

	for ( %mTypeIndex = %listStart; %mTypeIndex < $MLIST::TypeCount; %mTypeIndex++ ) {
		if ( %lineNum++ > $menu::maxitems ) {
			menu::add("More mission types...", "moreTypes " @ %mTypeIndex, %cl );
			break;
		}
		else if ($MLIST::Type[%mTypeIndex] != "Training") {
			menu::add($MLIST::Type[%mTypeIndex], %mTypeIndex @ " 0", %cl );
		}
	}
}


function processMenuChangeMissionType( %cl, %option ) {
	%type = getWord(%option, 0);
	%index = getWord(%option, 1);

	if (%type == "moreTypes") {
		menu::ChangeMissionType(%cl, %index);   
	} else {
		menu::new("Change Mission", "changeMission", %cl);

		for(%i = 0; (%misIndex = getWord($MLIST::MissionList[%type], %index + %i)) != -1; %i++) {
			if ((%i + 1) > $menu::maxitems) {
				menu::add("More missions...", "more " @ %index + %i @ " " @ %type, %cl );
				break;
			}
			menu::add($MLIST::EName[%misIndex], %misIndex @ " " @ %type, %cl);
		}
	}
}

function processMenuChangeMission( %cl, %option ) {
	if ( getWord(%option, 0) == "more" ) {
		%first = getWord(%option, 1);
		%type = getWord(%option, 2);
		processMenuChangeMissionType(%cl, %type @ " " @ %first);

		return;
	}

	%mi = getWord(%option, 0);
	%mt = getWord(%option, 1);

	%misName = $MLIST::EName[%mi];
	%misType = $MLIST::Type[%mt];

	// verify that this is a valid mission:
	if( %misType == "" || %misType == "Training" )
		return;

	for( %i = 0; true; %i++ ) {
		%misIndex = getWord($MLIST::MissionList[%mt], %i);
		if ( %misIndex == %mi )
			break;
		if ( %misIndex == -1 )
			return;
	}

	if( %cl.canChangeMission && !%cl.madeVote ) {
		log::add( %cl, "changed mission to " @ %misName, "", "", "MissionChange" );

		message::all(0, Client::getName(%cl) @ " changed the mission to " @ %misName @ " (" @ %misType @ ")");
		ObjectiveMission::missionComplete( %misName );
	} else {
		%cl.madeVote = "";
		admin::startvote(%cl, "change the mission to " @ %misName @ " (" @ %misType @ ")", "changeMission", %misName);
		Game::menuRequest(%cl);
	}
}


//////////////////////////////////////////////////////////////////////////////////////
// STRIP ADMIN FROM PLAYER
function menu::stripadmin( %cl, %vic ) {
	menu::new( "Strip Adminship", "stripadmin", %cl );

	menu::add( "Strip " @ Client::getName(%vic), "strip " @ %vic, %cl );
	menu::add( "Cancel", "no", %cl );
}

function processMenuStripAdmin( %cl, %opt ) {
	%action = getWord(%opt, 0);
	%vic = getWord(%opt, 1);

	if(%action == "strip") {
		if ( %cl.adminLevel > %vic.adminLevel ) {
			if ( %vic.adminLevel ) {
				adminlevel::grant( %vic, 0 );
				log::add( %cl, "Stripped Admin from", %vic, "", "Admins" );

				%vic.registeredName = "Stripped by " @ %cl.registeredName;	     
			}
		}
		else {
			log::add( %cl, "tried to strip Admin from", %vic, "", "Admins" );
			Client::sendMessage(%cl, $MSGTypeSystem, "You do not have the power to strip " @ Client::getName(%vic) @ ".");
			Client::sendMessage(%vic, $MSGTypeGame, Client::getName(%cl) @ " tried to strip your adminship.");
		}
	}
	Game::menuRequest(%cl);
}

//////////////////////////////////////////////////////////////////////////////////////
// VOTING
function menu::vote(%cl) {
	%waiting = $Server::TourneyMode && (!$CountdownStarted && !$matchStarted);
	
	menu::new("Options", "vote", %cl);
	menu::add("Change Teams", "changeteams", %cl, (!$loadingMission) && (!$matchStarted || !$Server::TourneyMode) );
	menu::add("Vote to change mission", "vChangeMission", %cl);
	menu::add("Vote to enter FFA mode", "vcffa", %cl, $Server::TourneyMode );
	menu::add("Vote to start the match", "vsmatch", %cl, %waiting );
	menu::add("Vote to enter Tournament mode", "vctourney", %cl, !$Server::TourneyMode );
	menu::add("Admin Options...", "adminoptions", %cl, (%cl.adminLevel > 0) );
}

function processMenuVote( %cl, %selection ) {
	if(%selection == "changeteams") {
         menu::changeteams( %cl );
		 return;
	} else if(%selection == "vsmatch") {
         admin::startvote(%cl, "start the match", "smatch", 0);
    } else if(%selection == "vetd") {
         admin::startvote(%cl, "enable team damage", "etd", 0);
    } else if(%selection == "vdtd") {
         admin::startvote(%cl, "disable team damage", "dtd", 0);
    } else if(%selection == "vcffa") {
         admin::startvote(%cl, "change to Free For All mode", "ffa", 0);
    } else if(%selection == "vctourney") {
         admin::startvote(%cl, "change to Tournament mode", "tourney", 0);
    } else if(%selection == "vChangeMission") {
         %cl.madeVote = true;
         menu::changemissiontype( %cl, 0 );
         return;
    } else if(%selection == "adminoptions") {
	   //no need to add, falls through to Game::menu request anyway
    }

	Game::menuRequest(%cl);
}

function menu::votepending( %cl ) {
    menu::new( "Vote in progress", "votepending", %cl );

	menu::add( "Vote YES to " @ $vote::Topic, "voteYes " @ $vote::Count, %cl, %cl.vote == "" );
	menu::add( "Vote No to " @ $vote::Topic, "voteNo " @ $vote::Count, %cl, %cl.vote == "" );
	menu::add( "VETO Vote to " @ $vote::Topic, "veto", %cl, %cl.canCancelVote );
	menu::add( "Admin Options...", "adminoptions", %cl, (%cl.adminLevel > 0) );
}

function processMenuVotePending( %cl, %sel ) {
	%selection = getWord( %sel, 0 );
	%value = getWord( %sel, 1 );

	if ( ( %selection == "voteYes" ) && ( %value == $vote::Count ) ) {
         %cl.vote = "yes";
         message::centerprint( %cl, "", 0 );
    } else if ( ( %selection == "voteNo") && ( %value == $vote::Count ) ) {
         %cl.vote = "no";
         message::centerprint( %cl, "", 0 );
    } else if ( %selection == "veto" && ( %cl.canCancelVote ) ) {
	    message::all(0, "Vote to " @ $vote::Topic @ " was VETO'd by an Admin.");
		message::bottomprintall( "", 0 );
		$vote::Topic = "";
      	admin::votefailed();
    } else if( %selection == "adminoptions" ) {
	   menu::admin(%cl);
	   return;
	}
	
	Game::menuRequest(%cl);
}

function admin::startmatch( %cl ) {
	if ( ( %cl != -1 ) && ( !%cl.canForceMatchStart ) )
		return;
	
	if( $CountdownStarted || $matchStarted )
		return;
	
	if( %cl == -1 )
		message::all( 0, "Match start countdown forced by vote." );
	else
		message::all( 0, "Match start countdown forced by " @ Client::getName(%cl) );

	Game::ForceTourneyMatchStart();
}


function admin::setteamdamage( %cl, %enabled ) {
	if( %cl != -1 && !%cl.canSwitchTeamDamage )
		return;
	
	if ( %enabled ) {
		$Server::TeamDamageScale = 1;
		%status = "ENABLED";
	} else {
		$Server::TeamDamageScale = 0;
		%status = "DISABLED";
	}

	if ( %cl == -1 ) {
		message::all(0, "Team damage set to " @ %status @ " by consensus.");
	} else {
		message::all( 0, Client::getName(%cl) @ " " @ %status @ " team damage." );
		log::add( %cl, %status @ " Team Damage", "", "", "TeamDamage" );
	}
}

function admin::kick( %cl, %tgt, %ban ) {
	if ( ( %cl == %tgt ) || ( %cl != -1 && !%cl.adminLevel ) )
		return;
	
	%name = Client::getName(%tgt);
	%ip = Client::getTransportAddress(%tgt);
	if(%ip == "")
		return;

	if ( %ban && !%cl.canBan )
		return;
         
	if(%ban) {
		%word = "banned";
		%cmd = "BAN: ";
		%desc = " ban ";
	} else {
		%word = "kicked";
		%cmd = "KICK: ";
		%desc = " kick ";
	}

	if ( %tgt.adminLevel > 0 ) {
		if ( %cl == -1 && ( %tgt.adminLevel > adminlevel::get("Public Admin") ) ) {
			// only public admins can be kicked by vote
			message::all( 0, Client::getName(%tgt) @ "is an admin and can't be " @ %word @ " by vote." );
			return;
		} else if ( %cl.adminLevel <= %tgt.adminLevel ) {
			//you must be higher level than the other admin to kick/ban him
			Client::sendMessage( %cl, $MSGTypeSystem, "You do not have the power to" @ %desc @ Client::getName(%tgt)@"." );
			Client::sendMessage( %tgt, $MSGTypeGame, Client::getName(%cl) @ " just tried to" @ %desc @ "you." );
			log:add( %cl, "attempted to" @ %desc, %tgt, "", "KickBan" );
			return;
		}
	}

	BanList::add( %ip, ( %ban ) ? $Server::BanTime : $Server::KickTime );

    log::add( %cl, %word, %tgt, "", "KickBan" );
	log::add( %cl, %word, %tgt, "@", "KickBan" );

	if ( %cl == -1 ) {
		message::all(0, %name @ " was " @ %word @ " from vote.");
		Net::kick(%tgt, "You were " @ %word @ " by consensus.");
	} else {
		message::all(0, %name @ " was " @ %word @ " by " @ Client::getName(%cl) @ ".");
		Net::kick(%tgt, "You were " @ %word @ " by " @ Client::getName(%cl));
	}
}

function admin::setffamode( %cl ) {
	if ( !$Server::TourneyMode || ( %cl != -1 && !%cl.canChangeGameMode ) )
		return;

	if( %cl == -1 ) {
		message::all( 0, "Server switched to Free-For-All Mode." );
	} else {
		message::all( 0, "Server switched to Free-For-All Mode by " @ Client::getName(%cl) @ "." );
		log::addlog( %cl, "switched to FFA Mode.", "", "", "ModeChange" );
	}
	
	$Server::TourneyMode = ( false );
	message::centerprintall(); // clear the messages
	if( !$matchStarted && !$countdownStarted ) {
		if ( $Server::warmupTime )
			Server::Countdown($Server::warmupTime);
		else   
			Game::startMatch();
	}
}


function admin::settourneymode( %cl ) {
	if ( $Server::TourneyMode || ( %cl != -1 && !%cl.canChangeGameMode ) )
		return;

	$Server::TeamDamageScale = 1;
	
	if( %cl == -1 ) {
		message::all( 0, "Server switched to Tournament Mode." );
	} else {
		message::all(0, "Server switched to Tournament Mode by " @ Client::getName(%cl) @ ".");
		log::add( %cl, "switched to Tournament Mode.", "", "", "ModeChange" );
	}

	$Server::TourneyMode = true;
	Server::nextMission();
}

function admin::alert( %message ) {
	for ( %cl = Client::getFirst(); %cl != -1; %cl = Client::getNext(%cl) ) {
		if ( %cl.canReceiveAlerts )
			message::bottomprint( %cl, "<jc>" @ %message );
	}
}

function admin::formatip( %ip, %numWords, %stringSize, %fillEmptySlots ) {
	%words = %chars = 0;

	if ( String::findSubStr(%ip, "LOOPBACK") != -1 )
		return "LOOPBACK";

	%fmtip = "";
	while ( ( %words <= %numWords ) && ( %chars <= %stringSize ) ) {
		%char = String::getSubStr( %ip, %chars, 1 );
		if ( ( %char == "." ) || ( %char == ":" ) )
			%words++;
		
		%chars++;
	}
	%fmtip = String::getSubStr( %ip, 0, %chars );

	if ( %fillEmptySlots ) {
		%slots = ( 4 - %words );
		for ( %slot = 0; %slot <= %slots; %slot++ ) {
			%fmtip = %fmtip @ "xxx";
			if ( %slot < %slots )
				%fmtip = %fmtip @ ".";
		}
	}
	
	return ( %fmtip );
}

function admin::parseip( %cl, %numWords, %stringSize, %fillEmptySlots ) {
	return ( admin::formatip( Client::getTransportAddress(%cl), %numWords, %stringSize, %fillEmptySlots ) );
}

function admin::dumpplayerlist( %cl ) {
	%separator = String::Dup( "_", 70 );
	if ( %cl )
		Client::sendMessage( %cl, $MSGTypeCommand, %separator );
	else
		echo( %separator );

	for ( %cl2 = Client::getFirst(); %cl2 != -1; %cl2 = Client::getNext(%cl2) ) {
		if ( %cl2.adminLevel < 1 ) { 
			%admin = "##";
			%smurf = "";
		} else { 
			%admin = String::getSubStr(adminlevel::getname(%cl2.adminLevel), 0, 1) @ "A";
			%smurf = "/" @ %cl2.registeredName;
		}

		%clId = string::rpad(%cl2, 6);
		%admin = string::rpad(%admin, 4);
		%score = string::rpad("Score: " @ %cl2.score, 12);
		%tks = string::rpad("TKs: " @ %cl2.TKs, 9); 
		%ip = string::rpad(admin::parseip(%cl2, 4, 18, false), 19);
		%name = Client::getName(%cl2) @ %smurf;

		if ( %cl )
			Client::sendMessage( %cl, $MSGTypeCommand, %clId @ %admin @ %tks @ %score @ %ip @ %name );
		else
			echo( %admin @ %tks @ %score @ %ip @ %name );
	}

	if( %cl )
		Client::sendMessage( %cl , $MSGTypeCommand, %separator );
	else
		echo( %separator );
}

function adminlevel::reset() {
	deletevariables( "$admin::level*" );
	$admin::levels = 0;
	$admin::levelpermissions = 0;
	$admin::levelname[ "" ] = "(Undefined)";
	$admin::level[ "" ] = "(Undefined)";
}

function adminlevel::add( %desc ) {
	$admin::level[ %desc ] = $admin::levels;
	$admin::levelname[ $admin::levels ] = %desc;
	$admin::levels++;
}

function adminlevel::get( %levelname ) {
	return ( $admin::level[ %levelname ] );
}

function adminlevel::getname( %level ) {
	if ( %level < 0 || %level >= $admin::levels )
		return "(Undefined)";
	return ( $admin::levelname[ %level ] );
}

function adminlevel::addpermission( %name, %levelname ) {
	$admin::levelpermissions[ %name ] = adminlevel::get( %levelname );
	
	$admin::levelpermissionlist[ $admin::levelpermissions ] = %name;
	$admin::levelpermissions++;
}

function adminlevel::access( %cl, %name ) {
	return ( $admin::levelpermissions[ %name ] <= %cl.adminLevel );
}

function adminlevel::grant( %cl, %level ) {
	if ( %level >= $admin::levels )
		%level = ( $admin::levels - 1 );
	if ( %level < 0 )
		%level = 0;

	%cl.adminLevel = ( %level );
	%cl.isAdmin = ( %level > 0 );
	
	for ( %i = 0; %i < $admin::levelpermissions; %i++ ) {
		%name = $admin::levelpermissionlist[%i];
		eval( %cl @ "." @ %name @ " = " @ adminlevel::access( %cl, %name ) @ ";" );
	}
}

