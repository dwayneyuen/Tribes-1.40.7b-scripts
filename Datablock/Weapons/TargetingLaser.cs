$AutoUse[TargetingLaser] = False;

//----------------------------------------------------------------------------

ItemImageData TargetingLaserImage
{
	shapeFile = "paintgun";
	mountPoint = 0;

	weaponType = 2; // Sustained
	projectileType = targetLaser;
	accuFire = true;
	minEnergy = 5;
	maxEnergy = 15;
	reloadTime = 1.0;

	lightType   = 3;  // Weapon Fire
	lightRadius = 1;
	lightTime   = 1;
	lightColor  = { 0.25, 1, 0.25 };

	sfxFire     = SoundFireTargetingLaser;
	sfxActivate = SoundPickUpWeapon;
};

ItemData TargetingLaser
{
	description   = "Targeting Laser";
	className     = "Tool";
	shapeFile     = "paintgun";
	hudIcon       = "targetlaser";
   	heading = "bWeapons";
	shadowDetailMask = 4;
	imageType     = TargetingLaserImage;
	price         = 50;
	showWeaponBar = false;
};
