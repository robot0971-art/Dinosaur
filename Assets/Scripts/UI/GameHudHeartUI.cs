using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameHudHeartUI : MonoBehaviour
{
    [Header("Heart Settings")]
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private int maxHearts = 4;
    [SerializeField] private int currentHearts = 3;
    [SerializeField] private bool hideEmptyHearts = true;

    [Header("Heart Events")]
    [SerializeField] private UnityEvent<int> onHeartsChanged;
    [SerializeField] private UnityEvent onHeartsEmpty;

    private void Awake()
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            heartImages = FindSiblingHeartImages();
        }

        maxHearts = Mathf.Max(4, maxHearts);
        EnsureHeartImageCapacity();
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
    }

    private void Start()
    {
        SetHearts(currentHearts);
    }

    public void SetHearts(int hearts)
    {
        currentHearts = Mathf.Clamp(hearts, 0, maxHearts);
        UpdateHeartDisplay();
        onHeartsChanged?.Invoke(currentHearts);

        if (currentHearts == 0)
        {
            onHeartsEmpty?.Invoke();
        }
    }

    public void AddHeart()
    {
        SetHearts(currentHearts + 1);
    }

    public void RemoveHeart()
    {
        SetHearts(currentHearts - 1);
    }

    public bool TryRemoveHeart()
    {
        if (currentHearts <= 0)
        {
            return false;
        }

        RemoveHeart();
        return currentHearts > 0;
    }

    public int GetCurrentHearts()
    {
        return currentHearts;
    }

    public int GetMaxHearts()
    {
        return maxHearts;
    }

    public bool IsDead()
    {
        return currentHearts <= 0;
    }

    private void UpdateHeartDisplay()
    {
        if (heartImages == null)
        {
            return;
        }

        for (var i = 0; i < heartImages.Length; i++)
        {
            var heartImage = heartImages[i];
            if (heartImage == null)
            {
                continue;
            }

            if (i < currentHearts)
            {
                if (fullHeartSprite != null)
                {
                    heartImage.sprite = fullHeartSprite;
                }

                heartImage.enabled = true;
                continue;
            }

            if (hideEmptyHearts)
            {
                heartImage.enabled = false;
            }
            else if (emptyHeartSprite != null)
            {
                heartImage.sprite = emptyHeartSprite;
                heartImage.enabled = true;
            }
            else
            {
                heartImage.enabled = false;
            }
        }
    }

    private Image[] FindSiblingHeartImages()
    {
        var images = new List<Image>();
        var searchRoot = transform.parent != null ? transform.parent : transform;
        for (var i = 0; i < searchRoot.childCount; i++)
        {
            var child = searchRoot.GetChild(i);
            if (!IsHeartObjectName(child.name))
            {
                continue;
            }

            var image = child.GetComponent<Image>() ?? child.GetComponentInChildren<Image>(true);
            if (image != null)
            {
                images.Add(image);
            }
        }

        if (images.Count == 0)
        {
            images.AddRange(GetComponentsInChildren<Image>(true));
        }

        return images.ToArray();
    }

    private void EnsureHeartImageCapacity()
    {
        if (heartImages == null || heartImages.Length == 0 || heartImages.Length >= maxHearts)
        {
            return;
        }

        var images = new List<Image>(heartImages);
        var template = images[images.Count - 1];
        var step = GetHeartStep(images);
        while (images.Count < maxHearts)
        {
            var clone = Instantiate(template, template.transform.parent);
            clone.name = $"Heart UI ({images.Count})";
            clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + images.Count);

            if (clone.transform is RectTransform cloneRect && template.transform is RectTransform templateRect)
            {
                cloneRect.anchoredPosition = templateRect.anchoredPosition + step * (images.Count - heartImages.Length + 1);
            }

            images.Add(clone);
        }

        heartImages = images.ToArray();
    }

    private static Vector2 GetHeartStep(IReadOnlyList<Image> images)
    {
        if (images.Count >= 2
            && images[images.Count - 1].transform is RectTransform last
            && images[images.Count - 2].transform is RectTransform previous)
        {
            var step = last.anchoredPosition - previous.anchoredPosition;
            if (step.sqrMagnitude > 0.001f)
            {
                return step;
            }
        }

        return new Vector2(72f, 0f);
    }

    private static bool IsHeartObjectName(string objectName)
    {
        return objectName == "Heart UI" || objectName.StartsWith("Heart UI (", System.StringComparison.Ordinal);
    }
}
