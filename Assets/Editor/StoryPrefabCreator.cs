using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using TMPro; // Import TextMeshPro namespace
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryPageCreator : EditorWindow
{
    #region Fields

    private int currentPageIndex = 1;
    private int totalPages = 0;
    private string pageName = "";
    private string pageLine = "";
    private List<GameObject> storyObjects = new List<GameObject>(); // List to store story objects
    private GameObject canvasInstance; // Field to store the Canvas prefab instance
    private bool previewing = false; // Field to track if we are currently previewing

    #endregion

    #region Menu Item

    [MenuItem("Tools/Story Page Creator")]
    public static void ShowWindow()
    {
        GetWindow<StoryPageCreator>("Story Page Creator");
    }

    #endregion

    #region Initialization

    private void OnEnable()
    {
        UpdateTotalPages();
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        GUILayout.Label("Create Story Page", EditorStyles.boldLabel);

        pageName = EditorGUILayout.TextField("Page Name:", pageName);
        pageLine = EditorGUILayout.TextField("Page Line:", pageLine);

        GUILayout.Space(10);

        if (GUILayout.Button("Create Page"))
        {
            CreateStoryPage();
        }

        GUILayout.Space(10);

        if (!previewing && GUILayout.Button("Preview Pages"))
        {
            TogglePreviewMode();
        }

        if (previewing)
        {
            if (GUILayout.Button("Stop Previewing"))
            {
                TogglePreviewMode();
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Cycle Pages"))
            {
                  CyclePages();
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Play Preview"))
        {
            PlayPreview();
        }

        GUILayout.Label("Current Page Index: " + currentPageIndex);
        GUILayout.Label("Total Pages: " + totalPages);
    }

    #endregion

    #region Story Object Methods

    private void FindStoryObjects()
    {
        storyObjects.Clear();
        GameObject[] storyObjs = GameObject.FindGameObjectsWithTag("Story");
        storyObjects.AddRange(storyObjs);
    }

    #endregion

    #region Page Methods

    private void CreateStoryPage()
    {
        FindStoryObjects(); // Ensure we get the latest story objects

        if (storyObjects.Count == 0)
        {
            Debug.LogWarning("No GameObjects with tag 'Story' found.");
            return;
        }

        GameObject parentObject = new GameObject("StoryPage");

        foreach (GameObject storyObject in storyObjects)
        {
            GameObject childObject = Instantiate(storyObject, parentObject.transform);
        }

        string prefabDirectory = "Assets/Pages/PagePrefabs";
        if (!AssetDatabase.IsValidFolder(prefabDirectory))
        {
            AssetDatabase.CreateFolder("Assets/Pages", "PagePrefabs");
        }

        string prefabPath = Path.Combine(prefabDirectory, pageName + ".prefab");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(parentObject, prefabPath);

        DestroyImmediate(parentObject, false);

        prefab.tag = "Page";

        PageInformation pageInfo = ScriptableObject.CreateInstance<PageInformation>();
        pageInfo.pageLine = pageLine;
        pageInfo.pageModelScene = prefab;

        string pagesDirectory = "Assets/Pages";
        string pagePath = Path.Combine(pagesDirectory, pageName + ".asset");

        if (!AssetDatabase.IsValidFolder(pagesDirectory))
        {
            AssetDatabase.CreateFolder("Assets", "Pages");
        }

        AssetDatabase.CreateAsset(pageInfo, pagePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Page created at: " + pagePath);

        UpdateTotalPages();
    }

    private void UpdateTotalPages()
    {
        string directoryPath = "Assets/Pages";
        if (Directory.Exists(directoryPath))
        {
            string[] files = Directory.GetFiles(directoryPath, "*.asset");
            totalPages = files.Length;
        }
        else
        {
            totalPages = 0;
        }
    }

    #endregion

    #region Preview Methods

   private void TogglePreviewMode()
{
    previewing = !previewing;
    if (previewing)
    {
        foreach (GameObject obj in storyObjects)
        {
            obj.SetActive(false);
        }

        // Instantiate the first page
        CyclePages();
    }
    else
    {
        EnableStoryObjects();

        GameObject[] pageObjects = GameObject.FindGameObjectsWithTag("Page");
        foreach (GameObject pageObject in pageObjects)
        {
            DestroyImmediate(pageObject);
        }
    }
}

    private void CyclePages()
    {
        GameObject[] pageObjects = GameObject.FindGameObjectsWithTag("Page");
        foreach (GameObject pageObject in pageObjects)
        {
            DestroyImmediate(pageObject);
        }

        currentPageIndex++;
        if (currentPageIndex > totalPages)
        {
            currentPageIndex = 1;
        }

        string pagePath = "Assets/Pages/Page" + currentPageIndex + ".asset";
        PageInformation pageInfo = AssetDatabase.LoadAssetAtPath<PageInformation>(pagePath);
        if (pageInfo != null)
        {
            GameObject prefab = pageInfo.pageModelScene;
            GameObject instantiatedObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
        else
        {
            Debug.LogWarning("Page " + currentPageIndex + " does not exist.");
        }
    }

    #endregion

    #region Additional Methods

    private void PlayPreview()
    {
        // Save the current scene to ensure it reloads after exiting play mode
        string currentScenePath = SceneManager.GetActiveScene().path;

        // Enter play mode
        EditorApplication.isPlaying = true;

        // Load the first page when play mode starts
        EditorApplication.playModeStateChanged += PlayModeStateChanged;

        void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Instantiate the Canvas prefab
                InstantiateCanvasPrefab();

                // Load the first page
                LoadAndInstantiatePage(1);

                // Unsubscribe from the play mode state changed event to prevent multiple calls
                EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            }
        }
    }

    private void InstantiateCanvasPrefab()
    {
        string canvasPrefabPath = "Assets/AR Storyteller UI/CanvasPrefab.prefab";
        GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(canvasPrefabPath);
        if (canvasPrefab != null)
        {
            canvasInstance = Instantiate(canvasPrefab);

            // Find the buttons by name
            Button forwardButton = canvasInstance.transform.Find("ForwardButton")?.GetComponent<Button>();
            Button backwardButton = canvasInstance.transform.Find("BackwardButton")?.GetComponent<Button>();

            // Assign methods to button click events
            if (forwardButton != null)
            {
                forwardButton.onClick.AddListener(CycleForward);
            }

            if (backwardButton != null)
            {
                backwardButton.onClick.AddListener(CycleBackward);
            }

            // Update the page line text
            TextMeshProUGUI pageText = canvasInstance.transform.Find("PageText")?.GetComponent<TextMeshProUGUI>();
            if (pageText != null)
            {
                pageText.text = pageLine;
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component with name 'PageText' not found in Canvas prefab.");
            }
        }
        else
        {
            Debug.LogError("Canvas prefab not found at path:" + canvasPrefabPath);
        }
    }

    private void LoadAndInstantiatePage(int pageIndex)
    {
        // Destroy previously instantiated page prefabs
        DestroyPagePrefabs();

        // Load page information
        PageInformation pageInfo = LoadPageInformation(pageIndex);
        if (pageInfo == null)
        {
            Debug.LogError("Page information for index " + pageIndex + " not found.");
            return;
        }

        // Update the page line text
        TextMeshProUGUI pageLineText = canvasInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (pageLineText != null)
        {
            pageLineText.text = pageInfo.pageLine;
        }
        else
        {
            Debug.LogError("TextMeshProUGUI component not found in Canvas prefab.");
        }

        // Instantiate page model scene
        GameObject prefab = pageInfo.pageModelScene;
        if (prefab != null)
        {
            Instantiate(prefab);
        }
        else
        {
            Debug.LogError("Prefab for page " + pageIndex + " is null.");
        }
    }

    private void DestroyPagePrefabs()
    {
        // Find all page prefabs and destroy them
        GameObject[] pagePrefabs = GameObject.FindGameObjectsWithTag("Page");
        foreach (GameObject pagePrefab in pagePrefabs)
        {
            Destroy(pagePrefab);
        }
    }

    public void CycleForward()
    {
        currentPageIndex++;
        if (currentPageIndex > totalPages)
        {
            currentPageIndex = 1;
        }

        LoadAndInstantiatePage(currentPageIndex);
    }

    public void CycleBackward()
    {
        currentPageIndex--;
        if (currentPageIndex < 1)
        {
            currentPageIndex = totalPages;
        }

        LoadAndInstantiatePage(currentPageIndex);
    }

    private PageInformation LoadPageInformation(int pageIndex)
    {
        string pagePath = "Assets/Pages/Page" + pageIndex + ".asset";
        return UnityEditor.AssetDatabase.LoadAssetAtPath<PageInformation>(pagePath);
    }

    private void EnableStoryObjects()
    {
        foreach (GameObject obj in storyObjects)
        {
            obj.SetActive(true);
        }
    }

    private void DisableStoryObjects()
    {
        foreach (GameObject obj in storyObjects)
        {
            obj.SetActive(false);
        }
    }

    #endregion

  #region Scene View Focus Handling

    private void OnLostFocus()
    {
        // Exit scene view preview mode if it's active
        if (previewing)
        {
            TogglePreviewMode();
        }
    }

    #endregion
}