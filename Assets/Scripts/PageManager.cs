using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PageManager : MonoBehaviour
{
    public List<PageInformation> pages = new List<PageInformation>();
    public TMP_Text visualNovelText;

    private int _currentPageNumber = 0;
    private GameObject _displayedModel;

    public Transform referencePoint;
    void Start()
    {
      
    }

    void Update()
    {
        DisplayText();
        DisplayModel();
    }
    
    private void DisplayText()
    {
        visualNovelText.text = pages[_currentPageNumber].pageLine;
    }

    private void DisplayModel()
    {
        if (_displayedModel != null)
        {
            Destroy(_displayedModel);
        }

        _displayedModel = Instantiate(pages[_currentPageNumber].pageModelScene, referencePoint.position, referencePoint.transform.rotation);
    }

    public void NextPage()
    {
        if (_currentPageNumber == pages.Count - 1) return;
        
        _currentPageNumber++;
    }

    public void LastPage()
    {
        if (_currentPageNumber == 0) return;

        _currentPageNumber--;
    }
}
