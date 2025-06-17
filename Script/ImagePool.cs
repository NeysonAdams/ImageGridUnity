using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImagePool
{

    private List<Sprite> sprites = new List<Sprite>();
    private int currentIndex = 0;

    public ImagePool()
    {
        sprites = new List<Sprite>(Resources.LoadAll<Sprite>("Images"));

        if (sprites.Count == 0)
        {
            Debug.LogWarning("ImagePool: No sprites found in Resources/Images");
        }
    }

    public Sprite Next()
    {
        if (sprites.Count == 0) return null;

        var sprite = sprites[currentIndex];
        currentIndex = (currentIndex + 1) % sprites.Count;
        return sprite;
    }

}
