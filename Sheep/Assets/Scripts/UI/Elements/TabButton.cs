using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabButton : Button3D
{
    [SerializeField]
    Material activeMaterial;

    bool active = false;

    const float kActiveButtonZOffset = -0.15f;

    public override void HoverStart()
    {
        if (active)
            return;

        base.HoverStart();
    }

    public override void HoverEnd()
    {
        if (active)
            return;

        base.HoverEnd();
    }

    public override void OnDisable()
    {

    }

    void Repaint()
    {
        Vector3 pos = transform.localPosition;

        if (active)
        {
            transform.localPosition = new Vector3(pos.x, pos.y, kActiveButtonZOffset);

            renderer.material = activeMaterial;
        }
        else
        {
            transform.localPosition = new Vector3(pos.x, pos.y, 0);

            renderer.material = originalMaterial;
        }
    }

    public void SetActive(bool state)
    {
        active = state;
        Repaint();
    }

}
