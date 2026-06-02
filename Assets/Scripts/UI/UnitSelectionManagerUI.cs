using System.Collections;
using System.Collections.Generic;
using Common;
using Unity.VisualScripting;
using UnityEngine;

public class UnitSelectionManagerUI : MonoSingleton<UnitSelectionManagerUI>
{
    public Canvas canvas;

    [SerializeField]
    private RectTransform selectionAreaRectTransform;

    void Start()
    {
        UnitSelectionManager.Instance.OnSelectionStart += OnSelectionStart;
        UnitSelectionManager.Instance.OnSelectionEnd += OnSelectionEnd;
    }

    void Update()
    {
        if (selectionAreaRectTransform.gameObject.activeSelf)
        {
            UpdateVisual();
        }
    }

    void OnSelectionStart(Vector2 position)
    {
        selectionAreaRectTransform.gameObject.SetActive(true);
        selectionAreaRectTransform.position = position;
        UpdateVisual();
    }

    void OnSelectionEnd(Vector2 position)
    {
        selectionAreaRectTransform.gameObject.SetActive(false);
    }

    void UpdateVisual()
    {
        Rect selectionAreaRect = UnitSelectionManager.Instance.GetSlectionAreaRect();
        float canvasScaleFactor = canvas.transform.localScale.x;
        selectionAreaRectTransform.anchoredPosition = new Vector2(
            selectionAreaRect.x ,
            selectionAreaRect.y 
        )/canvasScaleFactor;

        selectionAreaRectTransform.sizeDelta = new Vector2(
            selectionAreaRect.width  ,
            selectionAreaRect.height  
        )/canvasScaleFactor;
    }
}
