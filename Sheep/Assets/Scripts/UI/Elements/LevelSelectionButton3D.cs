public class LevelSelectionButton3D : Button3D
{
    public override void OnEnable()
    {
        base.OnEnable();
        MainTxt.gameObject.SetActive(false); 
    }

    public override void HoverStart()
    {
        base.HoverStart();
        MainTxt.gameObject.SetActive(true);
    }

    public override void HoverEnd()
    {
        base.HoverEnd();
        MainTxt.gameObject.SetActive(false);

    }
}
