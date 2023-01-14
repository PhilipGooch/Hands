using UnityEngine;

public class UIRendererElement : UIElement
{
    new MeshRenderer renderer;
    MeshRenderer Renderer
    {
        get
        {
            if(renderer == null)
            {
                renderer = GetComponent<MeshRenderer>();
                if (renderer == null)
                    Debug.LogError("GameObject does not contain a MeshRenderer component", gameObject);
                else
                    originalColor = renderer.material.color;
            }

            return renderer;
        }
    }

    public override void SetInteractable(bool interactable, bool changeChildren = true)
    {
        base.SetInteractable(interactable, changeChildren);
        Renderer.material.color = interactable ? originalColor : GameParameters.Instance.disabledUIElementsColor;
    }
}
