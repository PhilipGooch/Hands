using System.Collections.Generic;
using UnityEngine;

public abstract class PaginatedSelectionScreen : MenuScreen
{
    [SerializeField]
    protected Button3D defaultSelectionButtonPrefab;
    [SerializeField]
    Transform buttonGroup;
    [SerializeField]
    float baseRot;
    [SerializeField]
    Button3D backButton;

    HorizontalNavigationButtons horizontalNavigation;

    List<Button3D> currentButtons = new List<Button3D>();
    int currentPage = 0;
    int pageCount = 0;
    int buttonCount = 1;
    int buttonsPerPage;

    List<Button3D> buttonPrefabs;

    Button3D leftMost;
    Button3D rightMost;

    protected void Initialize(HorizontalNavigationButtons horizontalNavigation, List<Button3D> buttonPrefabs, int buttonCount, int buttonsPerPage = 4)
    {
        this.buttonsPerPage = buttonsPerPage;
        this.buttonCount = buttonCount;
        this.buttonPrefabs = buttonPrefabs;
        this.horizontalNavigation = horizontalNavigation;

        currentPage = 0;
        pageCount = buttonCount / buttonsPerPage + (buttonCount % buttonsPerPage > 0 ? 1 : 0);

    

        foreach (Transform child in buttonGroup)
        {
            Destroy(child.gameObject);
        }

        currentButtons.Clear();
        CreateUI();
        DisplayCurrentPage();
    }

    private void OnEnable()
    {
        backButton.ClearOnClick();
        backButton.onClick += Toggle;

        UpdatePageNavigation();
        AdjustBackButton();

    }

    private void OnDisable()
    {
        horizontalNavigation.Reset();
    }

    public override void OnBecomeActive()
    {
        base.OnBecomeActive();
        backButton.Show();
        horizontalNavigation.Show();

    }

    public override void OnBecomeInactive()
    {
        base.OnBecomeInactive();
        backButton.Hide();
        horizontalNavigation.Hide();

    }

    void ChangePage(int amount)
    {
        currentPage += amount;
        DisplayCurrentPage();
        UpdatePageNavigation();
        AdjustBackButton();

    }

    void UpdatePageNavigation()
    {
        horizontalNavigation.Reset();
        if (gameObject.activeInHierarchy)
        {
            if (currentPage > 0)
            {
                horizontalNavigation.SetupLeftButton(leftMost, () => { ChangePage(-1); });
            }
            if (currentPage < pageCount - 1)
            {
                horizontalNavigation.SetupRightButton(rightMost, () => { ChangePage(1); });
            }
        }

    }

    void AdjustBackButton()
    {
        backButton.PlaceNextToAnotherButton(leftMost, ButtonPlacementSide.LEFT);
    }

    void DisplayCurrentPage()
    {
        for (int p = 0; p < pageCount; p++)
        {
            int buttonsToDisplay = Mathf.Min(buttonCount - p * buttonsPerPage, buttonsPerPage);

            for (int i = 0; i < buttonsToDisplay; i++)
            {
                var index = i + p * buttonsPerPage;
                var buttonInstance = currentButtons[index];

                if (p == currentPage)
                {
                    buttonInstance.gameObject.SetActive(true);

                    if (i == 0)
                        leftMost = buttonInstance;
                    if (i + 1 == buttonsToDisplay)
                        rightMost = buttonInstance;
                }
                else
                    buttonInstance.gameObject.SetActive(false);

            }
        }

    }

    protected void CreateUI()
    {
        for (int p = 0; p < pageCount; p++)
        {
            int buttonsToDisplay = Mathf.Min(buttonCount - p * buttonsPerPage, buttonsPerPage);

            for (int i = 0; i < buttonsToDisplay; i++)
            {
                var index = i + p * buttonsPerPage;

                Button3D prefab = buttonPrefabs[index];
                var buttonInstance = prefab.Create(buttonGroup.rotation, buttonGroup);
                currentButtons.Add(buttonInstance);

                var buttonSize = buttonInstance.GetBoxBounds().size;
                var position = UIUtilities.GetHorizontalPositionForButton(buttonSize, buttonGroup, i, buttonsToDisplay, 0);

                buttonInstance.transform.position = position;

                var finalRotY = buttonInstance.transform.localPosition.x * baseRot;
                buttonInstance.transform.localRotation = Quaternion.Euler(0, finalRotY, 0);

                buttonInstance.gameObject.SetActive(false);

                SetupButton(buttonInstance, index);
            }
        }

    }

    protected abstract void SetupButton(Button3D target, int index);
}
