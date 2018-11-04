function iplog::getfile() {
	%date = string::getsubstr( timestamp::format(), 0, 10 );
	%name = "";
	for ( %i = 0; true; %i++ ) {
		%ch = string::getsubstr( $Server::HostName, %i, 1 );
		if ( %ch == "" )
			return ( "serverlogs/iplog-" @ %name @ "-" @ %date @ ".cs" );
		
		%res = string::icompare( %ch, "z" );
		if ( ( %res >= -42 && %res <= -33 ) || ( %res >= -25 && %res <= 0 ) )
			%name = %name @ %ch;
		else
			%name = %name @ "_";
	}
}

function iplog::add( %cl ) {
	%name = Client::getName( %cl );
	%ip = Client::getTransportAddress( %cl );
	if ( %name == "" )
		return;
	
	$iplog = string::rpad( timestamp::format(), 22 ) @ " Name: " @ string::rpad( %name, 22 ) @ " IP: " @ string::rpad( %ip, 30 );
	export( "$iplog", $iplog::file, true ); // append
}

$iplog::file = iplog::getfile();

function log::add( %cl, %action, %recipient, %prefix, %type ) {
	if ( !$Server::Log[ %type ] )
		return;
	
	%action = " " @ %action;
	if ( %prefix == "" )
		%prefix = " ";

	switch( %cl ) {
		case "-1":
			%name = string::rpad("VOTERS", 16) @ "]";
			%sysname = " VOTERS";
			%ip = " {}";
			break;
		case "-2":
			%name = string::rpad("System", 16) @ "]";
			%sysname = " System";
			%ip = " {}";
			break;
		default:
			%name = string::rpad(%cl.registeredName, 16) @ "]";
			%sysname = " " @ Client::getName(%cl);
			%ip = " {" @ Client::getTransportAddress(%cl) @ "}";
			break;
	}

	if (%recipient) {
		%recipientName =	" " @ Client::getName(%recipient);
		%recipientIp = " {" @ Client::getTransportAddress(%recipient) @ "}";	  
	}

	$LogEntry = string::rpad("[" @ %prefix @ " " @ %name @ %sysname @ %action @ %recipientName, 100); 
	$LogEntry = $LogEntry @ string::rpad(":" @ %sysname @ %ip, 45); 
	$LogEntry = $LogEntry @ string::rpad("| " @ %recipientName @ %recipientIP, 45); 
	$LogEntry = "[" @ timestamp::format() @ "] " @ $LogEntry @ " : " @ $MissionName;

	export( "LogEntry", "serverlogs/" @ $Server::LogFile, true );
}