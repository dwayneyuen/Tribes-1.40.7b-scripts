$MissionTypePath = "Gameplay";

// Find & Retrieve
function MissionType::Load::FANDR() {
exec($MissionTypePath@"/game");
exec($MissionTypePath@"/towerswitch");
exec($MissionTypePath@"/flag-original");
exec("events/stats-default");
}

// Deathmatch
function MissionType::Load::DM() {
exec($MissionTypePath@"/game");
exec($MissionTypePath@"/DM");
}

// Capture the Flag
function MissionType::Load::CTF() {
exec($MissionTypePath@"/game");
exec($MissionTypePath@"/towerswitch");
exec($MissionTypePath@"/flag-ctf");
exec("events/stats-ctf");
}

// Defend & Destroy
function MissionType::Load::DnD() {
exec($MissionTypePath@"/towerswitch");
exec($MissionTypePath@"/staticshapes");
exec("events/stats-default");
}

// Capture & Hold
function MissionType::Load::CandH() {
exec($MissionTypePath@"/game");
exec($MissionTypePath@"/towerswitch");
exec("events/stats-default");
}

// Multiple Team
function MissionType::Load::MT() {
exec($MissionTypePath@"/game");
exec($MissionTypePath@"/towerswitch");
exec($MissionTypePath@"/flag-original");
exec("events/stats-default");
}