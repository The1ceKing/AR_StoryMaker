using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ObjectPlacementTool : EditorWindow
{
    #region Variables
    private static ObjectPlacementTool _instance;
    private const string PrefKey = "ObjectPlacementTool_SelectedObjects";

    public string prefabsFolderPath = "Assets/Prefabs"; // Folder path where prefabs are located

    private List<GameObject> objectsToPlace = new List<GameObject>();
    private GameObject objectToPlace;
    private bool isPlacingObject;
    private int lastClickedThumbnailIndex = -1;

    private List<GameObject> placedObjects = new List<GameObject>(); // Track placed objects for undo
    public Color thumbnailBorderColor = Color.blue; // Color for thumbnail border
    private GUIStyle thumbnailButtonStyle; // Style for thumbnail button

    private float objectScale = 1f; // Scale of the object to place

    //Ghost Block Variables
    private GameObject ghostObjectPrefab; // Reference to the ghost object prefab
    private GameObject ghostObjectInstance; // Instance of the ghost object
    private GameObject ghostObject;
    private Color ghostObjectDefaultColor = new Color(0.0f, 1.0f, 1.0f, 0.5f); // Light cyan with 50% transparency
    private Color ghostObjectErrorColor = Color.red; // Red color for error indication
    private Material ghostMaterial; // Material for the ghost object
    private GameObject lastSelectedObject;

    private bool isGhostObjectActive = false;
    private LightingControlTool lightingControlTool = new LightingControlTool();

    
    private enum Tab
    {
        ObjectPlacement,
        LightSettings,
        StoryScene,
    }

    private Tab currentTab = Tab.StoryScene;
    private Tab previousTab = Tab.StoryScene; // Added to track the previous tab
    private string newSceneName = "NewScene"; // Variable to store the new scene name
    private bool showExistingSceneButton = false;

    private GameObject directionalLightObject; // Reference to the directional light object
    private Vector3 objectRotation = Vector3.zero; // Rotation of the object to place

    #endregion

    #region Editor Window Methods

    [MenuItem("Tools/Object Placement Tool")]
    public static void ShowWindow()
    {
        _instance = GetWindow<ObjectPlacementTool>();
        _instance.titleContent = new GUIContent("Object Placement Tool");
    }

   private void OnGUI()
{
    EditorGUI.BeginChangeCheck();

    if (!showExistingSceneButton)
    {
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new string[] { "Object Placement", "Settings", "StoryScene", "Scene Management" }); // Added new tab
        if (EditorGUI.EndChangeCheck())
        {
            GUI.FocusControl(null);
        }
    }
    else
    {
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new string[] { "Object Placement", "Settings", "Scene Management" }); // Updated tabs
        if (EditorGUI.EndChangeCheck())
        {
            GUI.FocusControl(null);
        }
    }

    // Automatically stop placing if the tab changes
    if (currentTab != Tab.ObjectPlacement && isPlacingObject)
    {
        StopPlacing();
    }

    switch (currentTab)
    {
        case Tab.ObjectPlacement:
            DrawObjectPlacementTab();
            break;
        case Tab.LightSettings:
            DrawSettingsTab();
            break;
        case Tab.StoryScene:
            DrawStorySceneTab();
            break;
    }

    previousTab = currentTab; // Update the previous tab

    // Check for mouse clicks to exit placement mode
    if (isPlacingObject && Event.current.type == EventType.MouseDown)
    {
        StopPlacing();
    }
    
}


    #endregion

    #region Draw Methods

private void DrawObjectPlacementTab()
{
    GUILayout.Label("Object Placement Tab");
    GUILayout.Label("Drag prefabs here:");

    Event evt = Event.current;
    Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
    GUI.Box(dropArea, "Drag Prefabs Here");

    switch (evt.type)
    {
        case EventType.DragUpdated:
        case EventType.DragPerform:
            if (!dropArea.Contains(evt.mousePosition))
                break;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is GameObject)
                    {
                        GameObject prefab = (GameObject)draggedObject;
                        AddPrefabToFolder(prefab);
                        AddObject(prefab);
                    }
                }
            }
            Event.current.Use();
            break;
    }

    // Ensure that thumbnailButtonStyle is initialized
    if (thumbnailButtonStyle == null)
    {
        thumbnailButtonStyle = new GUIStyle(GUI.skin.button);
        thumbnailButtonStyle.border = new RectOffset(4, 4, 4, 4); // Adjust border size for thicker highlight
        thumbnailButtonStyle.margin = new RectOffset(2, 2, 2, 2);
        thumbnailButtonStyle.padding = new RectOffset(2, 2, 2, 2);
    }

    // Automatically select the last object if no object is currently selected and placement mode is active
    if (isPlacingObject && objectToPlace == null && objectsToPlace.Count > 0)
    {
        objectToPlace = objectsToPlace[objectsToPlace.Count - 1];
        lastClickedThumbnailIndex = objectsToPlace.Count - 1; // Update the index of the last object
    }

    GUILayout.BeginHorizontal();
    int thumbnailsPerRow = Mathf.FloorToInt(position.width / 100f); // Adjusted width to accommodate padding
    int count = 0;
    foreach (var obj in objectsToPlace)
    {
        Texture2D objectThumbnail = AssetPreview.GetAssetPreview(obj);
        bool isSelected = lastClickedThumbnailIndex == objectsToPlace.IndexOf(obj);

        Rect buttonRect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));

        if (isSelected)
        {
            // Adjust rect for thicker highlight
            EditorGUI.DrawRect(new Rect(buttonRect.x - 4, buttonRect.y - 4, 72, 72), thumbnailBorderColor); 
            // Make the selected button pop out by scaling it up
            buttonRect = ScaleRect(buttonRect, 1.1f); // Scale up by 10%
        }

        if (GUI.Button(buttonRect, objectThumbnail, thumbnailButtonStyle))
        {
            lastClickedThumbnailIndex = objectsToPlace.IndexOf(obj);
            objectToPlace = obj;
            if (isPlacingObject)
            {
                UpdateGhostObjectPrefab(); // Update ghost object when prefab changes
            }
            else
            {
                StartPlacing(); // Start placing if not already in placing mode
            }
        }

        count++;
        if (count >= thumbnailsPerRow)
        {
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(10); // Add horizontal spacing between rows
            count = 0;
        }
        else
        {
            GUILayout.Space(10); // Add horizontal spacing between buttons
        }
    }
    GUILayout.EndHorizontal();

    // Sliders for adjusting size and rotation
    GUILayout.Label("Adjust Object Size and Rotation");

    GUILayout.Label("Scale");
    objectScale = EditorGUILayout.Slider(objectScale, 0.1f, 1f);
    UpdateGhostObjectScale(); // Update ghost object scale when slider changes

    GUILayout.Label("Rotation");
    objectRotation = EditorGUILayout.Vector3Field("Rotation", objectRotation);
    UpdateGhostObjectRotation(); // Update ghost object rotation when field changes

    GUILayout.BeginHorizontal();
    if (GUILayout.Button(isPlacingObject ? "Stop Placing" : "Start Placing"))
    {
        if (isPlacingObject)
            StopPlacing();
        else
            StartPlacing();
    }
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Remove Selected Object"))
    {
        RemoveSelectedObject();
    }
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Undo"))
    {
        UndoLastPlacement();
    }
    GUILayout.EndHorizontal();
}


// Helper method to scale up the Rect
private Rect ScaleRect(Rect rect, float scale)
{
    Vector2 center = rect.center;
    Vector2 size = rect.size * scale;
    return new Rect(center - size * 0.5f, size);
}


    private void DrawLightingSettingsTab()
    {
        GUILayout.Label("Light Settings Tab");
        GUILayout.Label("Configure your light settings here.");

        GUILayout.Space(20);

        if (GUILayout.Button("Set Morning Light"))
        {
            lightingControlTool.SetDirectionalLightMorning();
        }

        if (GUILayout.Button("Set Midday Light"))
        {
            lightingControlTool.SetDirectionalLightMidday();
        }

        if (GUILayout.Button("Set Evening Light"))
        {
            lightingControlTool.SetDirectionalLightEvening();
        }

        if (GUILayout.Button("Set Night Light"))
        {
            lightingControlTool.SetDirectionalLightNight();
        }
    }

    private void DrawStorySceneTab()
    {
        GUILayout.Label("Welcome to the Story Scene tab.");
        GUILayout.Label("What would you like to do?");

        GUILayout.Space(20);

        if (GUILayout.Button("Create New Scene"))
        {
            CreateNewScene();
        }

        if (GUILayout.Button("Work in Existing Scene"))
        {
            showExistingSceneButton = true;
            currentTab = Tab.ObjectPlacement;
        }

        if (showExistingSceneButton)
        {
            currentTab = Tab.ObjectPlacement;
            showExistingSceneButton = false;
        }
    }

    #endregion

    #region Object Placement Methods

    private void AddObject(GameObject obj)
    {
        if (!objectsToPlace.Contains(obj))
        {
            objectsToPlace.Add(obj);
        }
    }

    #region Ghost Block
    private void CreateGhostObject()
    {
        if (ghostObjectPrefab != null)
        {
            if (ghostObjectInstance != null)
            {
                DestroyImmediate(ghostObjectInstance); // Destroy any existing ghost object
            }

            ghostObjectInstance = Instantiate(ghostObjectPrefab); // Instantiate the ghost object

            if (ghostMaterial == null)
            {
                ghostMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                ghostMaterial.SetFloat("_Surface", 1); // Set surface type to Transparent
                ghostMaterial.SetFloat("_Blend", 0); // Set blend mode to Alpha
                ghostMaterial.SetFloat("_ZWrite", 0);
                ghostMaterial.SetColor("_BaseColor", ghostObjectDefaultColor); // Set default color
            }

            Renderer renderer = ghostObjectInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = ghostMaterial; // Apply the material to the ghost object
            }

            ghostObjectInstance.transform.localScale = Vector3.one * objectScale;
            ghostObjectInstance.transform.eulerAngles = objectRotation;

            // Disable the collider to prevent it from affecting raycasts
            Collider collider = ghostObjectInstance.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            isGhostObjectActive = true;
        }
    }


    private void UpdateGhostObjectPosition()
    {
        if (ghostObjectInstance == null)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ghostObjectInstance.transform.position = hit.point;
            ghostObjectInstance.transform.rotation = Quaternion.identity;
            ghostObjectInstance.transform.localScale = Vector3.one * objectScale;

            // Check if the hit object has the "Story" tag
            if (hit.collider.CompareTag("Story"))
            {
                ghostMaterial.SetColor("_BaseColor", ghostObjectErrorColor); // Change color to red
            }
            else
            {
                ghostMaterial.SetColor("_BaseColor", ghostObjectDefaultColor); // Reset to default color
            }
        }
    }


    
    private void UpdateGhostObjectScale()
    {
        if (ghostObjectInstance != null)
        {
            ghostObjectInstance.transform.localScale = Vector3.one * objectScale;
        }
    }

    private void UpdateGhostObjectRotation()
    {
        if (ghostObjectInstance != null)
        {
            ghostObjectInstance.transform.eulerAngles = objectRotation;
        }
    }
    
    private void UpdateGhostObjectPrefab()
    {
        if (objectToPlace == null) return;

        if (ghostObject != null)
        {
            DestroyImmediate(ghostObject); // Remove the existing ghost object
        }

        // Instantiate the new ghost object
        ghostObject = Instantiate(objectToPlace);
        ghostObject.transform.localScale = Vector3.one * objectScale;
        ghostObject.transform.eulerAngles = objectRotation;

        // Ensure the ghost object's collider is disabled
        Collider collider = ghostObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Set the ghost object's layer
        int ghostLayer = LayerMask.NameToLayer("Ghost");
        if (ghostLayer == -1)
        {
            Debug.LogError("Layer 'Ghost' does not exist. Please add it in the Tags and Layers settings.");
            return;
        }
        ghostObject.layer = ghostLayer;

        // Apply the transparent material
        ApplyMaterialToGhostObject(ghostObject);

        SceneView.RepaintAll(); // Ensure the Scene view updates
    }





    #endregion

    private void StartPlacing()
    {
        // Ensure ghostObject is cleaned up and SceneView event is unsubscribed
        SceneView.duringSceneGui -= OnSceneGUI;
    
        if (objectToPlace == null)
        {
            Debug.LogWarning("No object selected for placement.");
            return;
        }

        if (ghostObject == null)
        {
            UpdateGhostObjectPrefab(); // Ensure the ghost object is updated
        }

        if (!isPlacingObject)
        {
            isPlacingObject = true;
            SceneView.duringSceneGui += OnSceneGUI; // Subscribe to SceneView event
            _instance?.Repaint(); // Ensure the editor window is repainted
        }
    }

    private void StopPlacing()
    {
        if (ghostObject != null)
        {
            DestroyImmediate(ghostObject);
            ghostObject = null;
        }
        isPlacingObject = false;
        lastClickedThumbnailIndex = -1;
        SceneView.duringSceneGui -= OnSceneGUI;
        _instance?.Repaint(); // Ensure the editor window is repainted
    }





    private void PlaceObject(GameObject obj, Vector3 position)
    {
        // Adjust the position to ensure the object is placed on top of the surface
        GameObject tempObject = Instantiate(obj);
        tempObject.transform.localScale = Vector3.one * objectScale;
        Renderer renderer = tempObject.GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;
        Vector3 adjustedPosition = new Vector3(position.x, position.y + bounds.extents.y, position.z);

        DestroyImmediate(tempObject);

        // Instantiate the object with the adjusted scale
        GameObject newObject = PrefabUtility.InstantiatePrefab(obj) as GameObject;
        newObject.transform.position = adjustedPosition;
        newObject.transform.localScale = Vector3.one * objectScale; // Apply the adjusted scale
        newObject.transform.eulerAngles = objectRotation;

        placedObjects.Add(newObject);

        // Apply the "Story" tag to the placed object
        newObject.tag = "Story";
    }

private void OnSceneGUI(SceneView sceneView)
{
    Event e = Event.current;

    // Handle shortcut for toggling placement mode
    if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab && (e.control || e.command))
    {
        TogglePlacingMode(); // Toggle placement mode
        e.Use(); // Consume the event
        return; // Exit early to prevent further processing
    }

    if (!isPlacingObject || ghostObject == null)
        return;

    // Update ghost object position based on mouse movement
    if (e.type == EventType.Repaint || e.type == EventType.MouseMove)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Move the ghost object to the hit point
            Renderer renderer = ghostObject.GetComponent<Renderer>();
            Bounds bounds = renderer.bounds;
            ghostObject.transform.position = hit.point + Vector3.up * (bounds.extents.y);

            // Apply the current scale and rotation
            ghostObject.transform.localScale = Vector3.one * objectScale;
            ghostObject.transform.eulerAngles = objectRotation;

            // Check if the hit object has the "Story" tag
            if (hit.collider.CompareTag("Story"))
            {
                // Change the ghost object's material color to red
                SetGhostObjectMaterialColor(new Color(1f, 0f, 0f, 0.5f)); // Red with 50% opacity
            }
            else
            {
                // Change the ghost object's material color back to cyan
                SetGhostObjectMaterialColor(new Color(0.5f, 1f, 1f, 0.5f)); // Light cyan with 50% opacity
            }

            SceneView.RepaintAll(); // Force repaint to ensure smooth updates
        }
    }

    // Place the object when mouse button is pressed
    if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.control && !e.shift)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the hit object has the "Story" tag
            if (hit.collider.CompareTag("Story"))
            {
                Debug.LogWarning("Cannot place object on top of another object with the 'Story' tag.");
                return;
            }

            // Place the object at the hit point
            PlaceObject(objectToPlace, hit.point);
            e.Use();
        }
    }
}


// Method to toggle placement mode and update button status

    private void TogglePlacingMode()
    {
        isPlacingObject = !isPlacingObject;

        // Automatically select the last object if no object is currently selected
        if (isPlacingObject && objectToPlace == null && lastSelectedObject != null)
        {
            objectToPlace = lastSelectedObject;
            Debug.Log("No object selected. Automatically selecting the last object.");
        }

        // Update the button status
        if (isPlacingObject)
        {
            Debug.Log("Entering Placement Mode");
        }
        else
        {
            Debug.Log("Exiting Placement Mode");
        }

        // Repaint the editor window
        _instance?.Repaint();
    }





// Method to update the ghost object's material color
private void SetGhostObjectMaterialColor(Color color)
{
    if (ghostObject != null)
    {
        Renderer renderer = ghostObject.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // Use sharedMaterial instead of material to avoid creating new material instances
            renderer.sharedMaterial.color = color;
        }
    }
}





    private void RemoveSelectedObject()
    {
        if (lastClickedThumbnailIndex >= 0 && lastClickedThumbnailIndex < objectsToPlace.Count)
        {
            GameObject objToRemove = objectsToPlace[lastClickedThumbnailIndex];
            objectsToPlace.RemoveAt(lastClickedThumbnailIndex);
            lastClickedThumbnailIndex = -1;

            string prefabName = objToRemove.name + ".prefab";
            string fullPath = "Assets/Story Assets" + "/" + prefabName;

            if (AssetDatabase.DeleteAsset(fullPath))
            {
                Debug.Log("Prefab deleted from Story Assets folder: " + fullPath);
            }
            else
            {
                Debug.LogWarning("Failed to delete prefab from Story Assets folder: " + fullPath);
            }
        }
        else
        {
            Debug.LogWarning("No object selected to remove!");
        }
    }

    private void UndoLastPlacement()
    {
        if (placedObjects.Count > 0)
        {
            GameObject lastPlacedObject = placedObjects[placedObjects.Count - 1];
            placedObjects.RemoveAt(placedObjects.Count - 1);
            DestroyImmediate(lastPlacedObject);
            Debug.Log("Undo last placement successful.");
        }
        else
        {
            Debug.LogWarning("No placed objects to undo.");
        }
    }

    #endregion

    #region Scene Management Methods

    private void CreateNewScene()
    {
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        string folderPath = "Assets/Scenes";
        string sceneName = newSceneName + ".unity";
        string scenePath = System.IO.Path.Combine(folderPath, sceneName);

        EditorSceneManager.SaveScene(newScene, scenePath);
        AssetDatabase.Refresh();

        EditorSceneManager.OpenScene(scenePath);
    }

    #endregion
    

    private void SaveSelectedObjects()
    {
        string[] prefabPaths = new string[objectsToPlace.Count];
        for (int i = 0; i < objectsToPlace.Count; i++)
        {
            if (objectsToPlace[i] != null)
            {
                prefabPaths[i] = AssetDatabase.GetAssetPath(objectsToPlace[i]);
            }
        }
        PlayerPrefs.SetString(PrefKey, string.Join("|", prefabPaths));
    }

    private void LoadSelectedObjects()
    {
        objectsToPlace.Clear();
        if (PlayerPrefs.HasKey(PrefKey))
        {
            string[] prefabPaths = PlayerPrefs.GetString(PrefKey).Split('|');
            foreach (string prefabPath in prefabPaths)
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (obj != null)
                {
                    objectsToPlace.Add(obj);
                }
            }
        }
    }

    private void LoadPrefabsFromFolder(string folderPath)
    {
        objectsToPlace.Clear(); // Clear existing objects

        // Get all prefabs in the specified folder
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        foreach (string prefabGUID in prefabGUIDs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                objectsToPlace.Add(prefab);
            }
        }
    }

    private void AddPrefabToFolder(GameObject prefab)
    {
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        string folderPath = "Assets/Story Assets";

        // Create the target folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Story Assets");
        }

        // Copy the prefab to the target folder
        string prefabName = prefab.name + ".prefab";
        string newPath = folderPath + "/" + prefabName;
        AssetDatabase.CopyAsset(prefabPath, newPath);
        AssetDatabase.ImportAsset(newPath);

        Debug.Log("Prefab copied to Story Assets folder: " + newPath);
    }

    private void OnEnable()
    {
        _instance = this;
        LoadSelectedObjects(); // Load selected objects when the tool is enabled
        LoadPrefabsFromFolder("Assets/Story Assets"); // Load prefabs from "Story Assets" folder
    }

    private void OnDisable()
    {
        SaveSelectedObjects(); // Save selected objects when the tool is disabled
    }

    private void OnDestroy()
    {
        StopPlacing(); // Ensure object placement stops when the window is closed
    }
    
    private Material CreateTransparentMaterial()
    {
        // Create a new material with the URP Lit shader
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        // Set the material's Surface Type to Transparent
        material.SetFloat("_Surface", 1); // 1 for Transparent

        // Set the material's Blend Mode to Alpha
        material.SetFloat("_Blend", 0); // 0 for Alpha Blend

        // Set the material's Base Map alpha to 50% transparency
        Color baseColor = new Color(0.5f, 1f, 1f, 0.5f); // Light cyan color with 50% transparency
        material.SetColor("_BaseColor", baseColor);

        // Ensure the material is properly rendered as transparent
        material.SetFloat("_ZWrite", 0); // Disable ZWrite to avoid depth buffer issues
        material.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        // Adjust render queue if necessary
        material.renderQueue = 3000; // Render queue for transparent objects

        return material;
    }

    
    private void ApplyMaterialToGhostObject(GameObject ghostObject)
    {
        if (ghostObject == null)
            return;

        // Create or get the material
        Material transparentMaterial = CreateTransparentMaterial();

        // Get the Renderer component and apply the material
        Renderer renderer = ghostObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = transparentMaterial;
        }
    }

  

}
