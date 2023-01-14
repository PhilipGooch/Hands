using UnityEngine;

public class GameSettings
{
    static GameSettings instance;

    public static GameSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameSettings();
            }
            return instance;
        }
    }

    public FloatStoredValue playerHeight = new FloatStoredValue("playerHeight", 0);
    public IntStoredValue masterVolume = new IntStoredValue("masterVolume", 20);
    public BooleanStoredValue instantCameraAnimations = new BooleanStoredValue("instantCameraAnimations", false);

    //IntStoredValue cameraRotationMode = new IntStoredValue("cameraRotationMode", (int)CameraRotationMode.DEGREES_25);
    //IntStoredValue cameraMovementMode = new IntStoredValue("cameraMovementMode", (int)CameraMovementMode.SmallJump);

    //public IntStoredValue locomotionMode = new IntStoredValue("locomotionMode", (int)LocomotionMode.JOYSTICK);

    //public LocomotionMode LocomotionMode
    //{
    //    get
    //    {
    //        return (LocomotionMode)locomotionMode.Value;
    //    }
    //    set
    //    {
    //        locomotionMode.Value = (int)value;
    //    }
    //}

    //public CameraRotationMode CamRotationMode
    //{
    //    get
    //    {
    //        return (CameraRotationMode)cameraRotationMode.Value;
    //    }
    //    set
    //    {
    //        cameraRotationMode.Value = (int)value;
    //    }
    //}
    //
    //public CameraMovementMode CamMovementMode
    //{
    //    get
    //    {
    //        return (CameraMovementMode)cameraMovementMode.Value;
    //    }
    //    set
    //    {
    //        cameraMovementMode.Value = (int)value;
    //    }
    //}

    public class BooleanStoredValue
    {
        string key;
        bool internalValue;

        public bool Value
        {
            get
            {
                return internalValue;
            }
            set
            {
                internalValue = value;
                PlayerPrefs.SetInt(key, internalValue ? 1 : 0);
            }
        }

        public BooleanStoredValue(string key, bool defaultValue)
        {
            this.key = key;
            internalValue = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) > 0;
        }
    }

    public class FloatStoredValue
    {
        string key;
        float internalValue;

        public float Value
        {
            get
            {
                return internalValue;
            }
            set
            {
                internalValue = value;
                PlayerPrefs.SetFloat(key, internalValue);
            }
        }

        public FloatStoredValue(string key, float defaultValue)
        {
            this.key = key;
            internalValue = PlayerPrefs.GetFloat(key, defaultValue);
        }
    }

    public class IntStoredValue
    {
        string key;
        int internalValue;

        public int Value
        {
            get
            {
                return internalValue;
            }
            set
            {
                internalValue = value;
                PlayerPrefs.SetInt(key, internalValue);
            }
        }

        public IntStoredValue(string key, int defaultValue)
        {
            this.key = key;
            internalValue = PlayerPrefs.GetInt(key, defaultValue);
        }
    }
}
