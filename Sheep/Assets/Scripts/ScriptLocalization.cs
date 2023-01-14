using UnityEngine;

namespace I2.Loc
{
	public static class ScriptLocalization
	{

		public static string campaign 		{ get{ return LocalizationManager.GetTranslation ("campaign"); } }
		public static string chapter_0 		{ get{ return LocalizationManager.GetTranslation ("chapter_0"); } }
		public static string chapter_1 		{ get{ return LocalizationManager.GetTranslation ("chapter_1"); } }
		public static string @continue 		{ get{ return LocalizationManager.GetTranslation ("continue"); } }
		public static string levelComplete 		{ get{ return LocalizationManager.GetTranslation ("levelComplete"); } }
		public static string no 		{ get{ return LocalizationManager.GetTranslation ("no"); } }
		public static string ok 		{ get{ return LocalizationManager.GetTranslation ("ok"); } }
		public static string pause 		{ get{ return LocalizationManager.GetTranslation ("pause"); } }
		public static string progress 		{ get{ return LocalizationManager.GetTranslation ("progress"); } }
		public static string quitGame 		{ get{ return LocalizationManager.GetTranslation ("quitGame"); } }
		public static string quitLevel 		{ get{ return LocalizationManager.GetTranslation ("quitLevel"); } }
		public static string restart 		{ get{ return LocalizationManager.GetTranslation ("restart"); } }
		public static string resume 		{ get{ return LocalizationManager.GetTranslation ("resume"); } }
		public static string selectChapter 		{ get{ return LocalizationManager.GetTranslation ("selectChapter"); } }
		public static string selectLevel 		{ get{ return LocalizationManager.GetTranslation ("selectLevel"); } }
		public static string settings 		{ get{ return LocalizationManager.GetTranslation ("settings"); } }
		public static string timeSpent 		{ get{ return LocalizationManager.GetTranslation ("timeSpent"); } }
		public static string yes 		{ get{ return LocalizationManager.GetTranslation ("yes"); } }
		public static string C7 		{ get{ return LocalizationManager.GetTranslation ("C7"); } }
		public static string C6 		{ get{ return LocalizationManager.GetTranslation ("C6"); } }
		public static string C5 		{ get{ return LocalizationManager.GetTranslation ("C5"); } }
		public static string C4 		{ get{ return LocalizationManager.GetTranslation ("C4"); } }
		public static string C3 		{ get{ return LocalizationManager.GetTranslation ("C3"); } }
		public static string C2 		{ get{ return LocalizationManager.GetTranslation ("C2"); } }
		public static string C1 		{ get{ return LocalizationManager.GetTranslation ("C1"); } }
		public static string C0 		{ get{ return LocalizationManager.GetTranslation ("C0"); } }
		public static string T1_Herding 		{ get{ return LocalizationManager.GetTranslation ("T1 Herding"); } }

		public static class Notifications
		{
			public static string genericConfirmation 		{ get{ return LocalizationManager.GetTranslation ("Notifications/genericConfirmation"); } }
			public static string quitConfirmation 		{ get{ return LocalizationManager.GetTranslation ("Notifications/quitConfirmation"); } }
			public static string quitTheGame 		{ get{ return LocalizationManager.GetTranslation ("Notifications/quitTheGame"); } }
			public static string quitToLvlSelection 		{ get{ return LocalizationManager.GetTranslation ("Notifications/quitToLvlSelection"); } }
		}

		public static class Settings
		{
			public static string accessabilityTab 		{ get{ return LocalizationManager.GetTranslation ("Settings/accessabilityTab"); } }
			public static string cameraAnimations 		{ get{ return LocalizationManager.GetTranslation ("Settings/cameraAnimations"); } }
			public static string generalTab 		{ get{ return LocalizationManager.GetTranslation ("Settings/generalTab"); } }
			public static string height 		{ get{ return LocalizationManager.GetTranslation ("Settings/height"); } }
			public static string language 		{ get{ return LocalizationManager.GetTranslation ("Settings/language"); } }
			public static string languageInNative 		{ get{ return LocalizationManager.GetTranslation ("Settings/languageInNative"); } }
			public static string movementMode 		{ get{ return LocalizationManager.GetTranslation ("Settings/movementMode"); } }
			public static string recalibrateHeight 		{ get{ return LocalizationManager.GetTranslation ("Settings/recalibrateHeight"); } }
			public static string rotationMode 		{ get{ return LocalizationManager.GetTranslation ("Settings/rotationMode"); } }
			public static string volume 		{ get{ return LocalizationManager.GetTranslation ("Settings/volume"); } }
			public static string movementEnabled 		{ get{ return LocalizationManager.GetTranslation ("Settings/movementEnabled"); } }
			public static string locomotionMode 		{ get{ return LocalizationManager.GetTranslation ("Settings/locomotionMode"); } }
		}

		public static class Settings_LocomotionMode
		{
			public static string joystickMode 		{ get{ return LocalizationManager.GetTranslation ("Settings/LocomotionMode/joystickMode"); } }
			public static string roomScaleMode 		{ get{ return LocalizationManager.GetTranslation ("Settings/LocomotionMode/roomScaleMode"); } }
			public static string teleportationMode 		{ get{ return LocalizationManager.GetTranslation ("Settings/LocomotionMode/teleportationMode"); } }
		}

		public static class Settings_MovementMode
		{
			public static string bigJumpMovement 		{ get{ return LocalizationManager.GetTranslation ("Settings/MovementMode/bigJumpMovement"); } }
			public static string smallJumpMovement 		{ get{ return LocalizationManager.GetTranslation ("Settings/MovementMode/smallJumpMovement"); } }
			public static string smoothMovement 		{ get{ return LocalizationManager.GetTranslation ("Settings/MovementMode/smoothMovement"); } }
		}

		public static class Settings_RotationMode
		{
			public static string degrees_0 		{ get{ return LocalizationManager.GetTranslation ("Settings/RotationMode/degrees_0"); } }
			public static string degrees_1 		{ get{ return LocalizationManager.GetTranslation ("Settings/RotationMode/degrees_1"); } }
			public static string smoothRotation 		{ get{ return LocalizationManager.GetTranslation ("Settings/RotationMode/smoothRotation"); } }
		}
	}

    public static class ScriptTerms
	{

		public const string campaign = "campaign";
		public const string chapter_0 = "chapter_0";
		public const string chapter_1 = "chapter_1";
		public const string @continue = "continue";
		public const string levelComplete = "levelComplete";
		public const string no = "no";
		public const string ok = "ok";
		public const string pause = "pause";
		public const string progress = "progress";
		public const string quitGame = "quitGame";
		public const string quitLevel = "quitLevel";
		public const string restart = "restart";
		public const string resume = "resume";
		public const string selectChapter = "selectChapter";
		public const string selectLevel = "selectLevel";
		public const string settings = "settings";
		public const string timeSpent = "timeSpent";
		public const string yes = "yes";
		public const string C7 = "C7";
		public const string C6 = "C6";
		public const string C5 = "C5";
		public const string C4 = "C4";
		public const string C3 = "C3";
		public const string C2 = "C2";
		public const string C1 = "C1";
		public const string C0 = "C0";
		public const string T1_Herding = "T1 Herding";

		public static class Notifications
		{
		    public const string genericConfirmation = "Notifications/genericConfirmation";
		    public const string quitConfirmation = "Notifications/quitConfirmation";
		    public const string quitTheGame = "Notifications/quitTheGame";
		    public const string quitToLvlSelection = "Notifications/quitToLvlSelection";
		}

		public static class Settings
		{
		    public const string accessabilityTab = "Settings/accessabilityTab";
		    public const string cameraAnimations = "Settings/cameraAnimations";
		    public const string generalTab = "Settings/generalTab";
		    public const string height = "Settings/height";
		    public const string language = "Settings/language";
		    public const string languageInNative = "Settings/languageInNative";
		    public const string movementMode = "Settings/movementMode";
		    public const string recalibrateHeight = "Settings/recalibrateHeight";
		    public const string rotationMode = "Settings/rotationMode";
		    public const string volume = "Settings/volume";
		    public const string movementEnabled = "Settings/movementEnabled";
		    public const string locomotionMode = "Settings/locomotionMode";
		}

		public static class Settings_LocomotionMode
		{
		    public const string joystickMode = "Settings/LocomotionMode/joystickMode";
		    public const string roomScaleMode = "Settings/LocomotionMode/roomScaleMode";
		    public const string teleportationMode = "Settings/LocomotionMode/teleportationMode";
		}

		public static class Settings_MovementMode
		{
		    public const string bigJumpMovement = "Settings/MovementMode/bigJumpMovement";
		    public const string smallJumpMovement = "Settings/MovementMode/smallJumpMovement";
		    public const string smoothMovement = "Settings/MovementMode/smoothMovement";
		}

		public static class Settings_RotationMode
		{
		    public const string degrees_0 = "Settings/RotationMode/degrees_0";
		    public const string degrees_1 = "Settings/RotationMode/degrees_1";
		    public const string smoothRotation = "Settings/RotationMode/smoothRotation";
		}
	}
}