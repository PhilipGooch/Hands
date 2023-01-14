/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttrubuteTest : MonoBehaviour
{
    [Group("Group A")]
    [Required]
    public GameObject go;

    [Group("Group B")]
    public bool showMore;

    [ShowIf("showMore")]
    [Group("Group B")]
    [ReadOnly]
    public float PI = Mathf.PI;
    [Group("Group B")]
    [ShowIf("showMore")]

    [InfoBox("This is info box")]
    public float mustBeMoreThanOne;

    [Group("Group B")]
    [ShowIf("showMore", "moreThanOneCondition")]
    [MinMaxSlider(-50, 50)]
    public Vector2 minMaxSlider = new Vector2(-30, 30);

    bool moreThanOneCondition()
    {
        return mustBeMoreThanOne > 1;
    }

}
*/