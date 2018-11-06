// ====================================================================================================== //
// Things must be executed in this order. -Dr.Arsenic
//
// System Functions
// Gameplay Functions
//
// Datablocks:
//	Sound Datablocks
//	Explosion Datablocks
//	Projectile Datablocks
//	Static Shapes, Turrets, & Armors
//

function LoadServerData()
{
	LoadFolder("Systems");
	LoadFolder("Gameplay");
	LoadFolder("Datablock");

	deleteVariables("$FileLoaded[*");
}

LoadServerData();


// ====================================================================================================== //

banlist::load( $Server::BanFile );
banlist::loadexclusions( $Server::ExclusionFile );