using I2.Loc;
using System.Collections.Generic;
using UnityEngine;
using VR.System;

public class SettingsScreen : MenuScreen
{
    [SerializeField]
    Button3D smallButton;
    [SerializeField]
    Transform backButtonLocation;
    [SerializeField]
    Transform optionUIGroup;
    [SerializeField]
    Toggle3D toggleButton;
    [SerializeField]
    Slider3D sliderPrefab;
    [SerializeField]
    Switch3D switchPrefab;
    [SerializeField]
    LabelVButton3D labelVButtonPrefab;

    [SerializeField]
    Sprite languageIcon;

    Button3D backButton;
    Slider3D volumeSlider;
    Switch3D locomotionSwitch;
    LabelVButton3D recalibrateHeight;
    Switch3D cameraMovementSwitch;
    Switch3D cameraRotationSwitch;
    Switch3D languageSelectionSwitch;
    Toggle3D cameraAnimations;

    public override bool DoFadeOut => true;

    GameSettings gameSettings;
    Player player;
    Tabs tabs;

    public override void Init()
    {
        base.Init();

        gameSettings = GameSettings.Instance;
        player = Player.Instance;

        tabs = GetComponentInChildren<Tabs>();

        CreateOptionUI(optionUIGroup);
    }

    void CreateOptionUI(Transform optionsGroup)
    {
        backButton = smallButton.Create(
            backButtonLocation.position,
            Toggle,
            backButtonLocation
        );

        volumeSlider = sliderPrefab.Create(
            optionsGroup.transform.position,
            ScriptTerms.Settings.volume,
            GetAdjustedStartingVolume(),
            OnVolumeChanged,
            optionsGroup
        );

        locomotionSwitch = switchPrefab.Create(
            optionsGroup.position,
            ScriptTerms.Settings.locomotionMode,
            (int)gameSettings.LocomotionMode,
            OnLocomotionModeChanged,
            new string[] { ScriptTerms.Settings_LocomotionMode.roomScaleMode, ScriptTerms.Settings_LocomotionMode.joystickMode, ScriptTerms.Settings_LocomotionMode.teleportationMode },
            optionsGroup
        );

        cameraMovementSwitch = switchPrefab.Create(
            optionsGroup.position,
            ScriptTerms.Settings.movementMode,
            (int)gameSettings.CamMovementMode,
            OnMovementModeChanged,
            new string[] { ScriptTerms.Settings_MovementMode.smallJumpMovement, ScriptTerms.Settings_MovementMode.bigJumpMovement, ScriptTerms.Settings_MovementMode.smoothMovement },
            optionsGroup);

        cameraRotationSwitch = switchPrefab.Create(
            optionsGroup.position,
            ScriptTerms.Settings.rotationMode,
            (int)gameSettings.CamRotationMode,
            OnRotationModeChanged,
            new string[] { ScriptTerms.Settings_RotationMode.degrees_0, ScriptTerms.Settings_RotationMode.degrees_1, ScriptTerms.Settings_RotationMode.smoothRotation },
            optionsGroup
        );

        cameraAnimations = toggleButton.Create(
            optionsGroup.position,
            ScriptTerms.Settings.cameraAnimations,
            gameSettings.instantCameraAnimations.Value,
            OnToggleInstantCameraMovement,
            optionsGroup
        );

        int id = LocalizationManager.GetAllLanguages().FindIndex(x => x == LocalizationManager.CurrentLanguage);

        languageSelectionSwitch = switchPrefab.Create(
            optionsGroup.position,
            ScriptTerms.Settings.language,
            id,
            OnLanguageSelectionChanged,
            LocalizationManager.GetAllLanguages().ToArray(),
            optionsGroup,
            languageIcon,
            false
        );

        LocomotionMode locomotionMode = (LocomotionMode)gameSettings.locomotionMode.Value;

        SetOptionsInteractableBasedOnLocomotionMode(locomotionMode);

        if (VRSystem.Instance.NeedsHeightCalibration)
        {
            recalibrateHeight = labelVButtonPrefab.Create(
                optionsGroup.position,
                ScriptTerms.Settings.recalibrateHeight,
                ScriptTerms.Settings.height,
                CalibrateHeight,
                optionsGroup
            );

            SetCurrentHeightText();
        }

        PlaceElementsIntoTabs();
    }

    private void OnLanguageSelectionChanged(int id)
    {
        LocalizationManager.CurrentLanguage = LocalizationManager.GetAllLanguages()[id];
    }

    public void PlaceElementsIntoTabs()
    {

        var generalTabElements = new List<UIElement>();
        generalTabElements.Add(languageSelectionSwitch);
        generalTabElements.Add(volumeSlider);
        if (recalibrateHeight != null)
            generalTabElements.Add(recalibrateHeight);

        var locomotionTabElements = new List<UIElement>();
        locomotionTabElements.Add(locomotionSwitch);
        locomotionTabElements.Add(cameraAnimations);
        locomotionTabElements.Add(cameraMovementSwitch);
        locomotionTabElements.Add(cameraRotationSwitch);

        tabs.AddTab(ScriptTerms.Settings.generalTab, generalTabElements, true);
        tabs.AddTab(ScriptTerms.Settings.accessabilityTab, locomotionTabElements, false);

    }

    float GetAdjustedStartingVolume()
    {
        return gameSettings.masterVolume.Value * 5f / 100f;
    }

    void CalibrateHeight()
    {
        player.CalibrateHeight();

        if (gameSettings.locomotionMode.Value == (int)LocomotionMode.JOYSTICK)
            player.AdjustPlayerHeight();

        recalibrateHeight.SetTexts(ScriptTerms.Settings.recalibrateHeight, ScriptTerms.Settings.height);
        SetCurrentHeightText();
    }

    void OnVolumeChanged(float newValue)
    {
        var adjusted = (int)(newValue * 100f / 5f);
        gameSettings.masterVolume.Value = adjusted;
        AudioManager.instance?.SetMasterLevel(adjusted);
    }

    void OnToggleInstantCameraMovement(bool newValue)
    {
        gameSettings.instantCameraAnimations.Value = newValue;
        player.SetInstantCameraAnimations(newValue);
    }

    void OnRotationModeChanged(int mode)
    {
        var camMode = (CameraRotationMode)mode;
        gameSettings.CamRotationMode = camMode;
        player.SetCameraRotationMode(camMode);
    }

    void OnMovementModeChanged(int mode)
    {
        var camMode = (CameraMovementMode)mode;
        gameSettings.CamMovementMode = camMode;
        player.SetCameraMovementMode(camMode);
    }

    void SetCurrentHeightText()
    {
        recalibrateHeight.SetParameterInText("HEIGHT", Mathf.RoundToInt(gameSettings.playerHeight.Value).ToString());
    }

    void OnLocomotionModeChanged(int mode)
    {
        var locomotionMode = (LocomotionMode)mode;
        gameSettings.LocomotionMode = locomotionMode;
        SetOptionsInteractableBasedOnLocomotionMode(locomotionMode);
    }

    void SetOptionsInteractableBasedOnLocomotionMode(LocomotionMode locomotionMode)
    {
        switch (locomotionMode)
        {
            case LocomotionMode.ROOM_SCALE:
                player.SetTeleportationEnabled(false);
                player.SetControllerMovementEnabled(false);
                player.SetControllerRotationEnabled(false);
                cameraMovementSwitch?.SetInteractable(false);
                cameraRotationSwitch?.SetInteractable(false);
                cameraAnimations?.SetInteractable(false);
                break;
            case LocomotionMode.JOYSTICK:
                player.SetTeleportationEnabled(false);
                player.SetControllerMovementEnabled(true);
                player.SetControllerRotationEnabled(true);
                cameraMovementSwitch?.SetInteractable(true);
                cameraRotationSwitch?.SetInteractable(true);
                cameraAnimations?.SetInteractable(true);
                break;
            case LocomotionMode.TELEPORTATION:
                player.SetTeleportationEnabled(true);
                player.SetControllerMovementEnabled(false);
                player.SetControllerRotationEnabled(true);
                cameraMovementSwitch?.SetInteractable(false);
                cameraRotationSwitch?.SetInteractable(true);
                cameraAnimations?.SetInteractable(true);
                break;
        }
    }
}
