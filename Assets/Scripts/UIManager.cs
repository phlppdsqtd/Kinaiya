using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuUI;
    public GameObject controlsUI;
    public GameObject pauseUI;
    public GameObject taskUI;
    public GameObject notebookUI;

    [Header("VR Input Actions (Level1 Only)")]
    public InputActionReference pauseButton;    // Bind to Y Button
    public InputActionReference taskListButton; // Bind to B Button
    public InputActionReference notebookButton; // Bind to A Button

    [Header("Player Settings")]
    public Transform headCamera; // Assign your Main Camera here in the Inspector
    public GameObject locomotionSystem;

    [Header("Game Managers")]
    public ToolResetManager toolResetManager;

    private bool isPaused = false;

    private void Start()
    {
        // Automatically position the Main Menu in front of the player when the scene loads
        if (mainMenuUI != null && mainMenuUI.activeSelf)
        {
            PositionUIInFrontOfPlayer(mainMenuUI);
        }
    }

    private void OnEnable()
    {
        // Subscribe to VR button presses if they are assigned
        if (pauseButton != null)
            pauseButton.action.performed += Context => TogglePause();
        
        if (taskListButton != null)
            taskListButton.action.performed += Context => ToggleTaskUI();
            
        if (notebookButton != null)
            notebookButton.action.performed += Context => ToggleNotebookUI();
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (pauseButton != null)
            pauseButton.action.performed -= Context => TogglePause();
            
        if (taskListButton != null)
            taskListButton.action.performed -= Context => ToggleTaskUI();
            
        if (notebookButton != null)
            notebookButton.action.performed -= Context => ToggleNotebookUI();
    }

    // --- HELPER METHOD TO CENTER UI ---

    private void PositionUIInFrontOfPlayer(GameObject uiElement)
    {
        if (uiElement == null || headCamera == null) return;

        // Get the direction the camera is facing, but flatten the Y axis so the menu stays vertically level
        Vector3 forward = headCamera.forward;
        forward.y = 0; 
        forward.Normalize();

        // Snap exactly 1.5 meters in front of the headset
        uiElement.transform.position = headCamera.position + (forward * 1.5f);
        
        // Rotate to face the user
        uiElement.transform.LookAt(headCamera);
        uiElement.transform.Rotate(0, 180, 0); // UI naturally faces backwards with LookAt, so we flip it
    }

    // --- MAIN MENU & GENERAL SCENE LOGIC ---

    public void PlayGame()
    {
        Time.timeScale = 1f; // Ensure time is normal when loading new scene
        SceneManager.LoadScene("Level1");
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("_MainMenu");
    }

    // --- CONTROLS UI ---

    public void OpenControls()
    {
        if (controlsUI != null) 
        {
            controlsUI.SetActive(true);
            PositionUIInFrontOfPlayer(controlsUI);
        }

        // Hide underlying menus so their buttons cannot be clicked
        if (pauseUI != null) pauseUI.SetActive(false);
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
    }

    public void CloseControls()
    {
        if (controlsUI != null) controlsUI.SetActive(false);

        // Re-enable the correct menu based on which scene we are in
        if (isPaused && pauseUI != null) 
        {
            pauseUI.SetActive(true);
            PositionUIInFrontOfPlayer(pauseUI);
        }
        else if (mainMenuUI != null) 
        {
            mainMenuUI.SetActive(true);
            PositionUIInFrontOfPlayer(mainMenuUI);
        }
    }

    // --- PAUSE MENU LOGIC ---

    public void TogglePause()
    {
        if (pauseUI == null) return;

        isPaused = !isPaused;
        pauseUI.SetActive(isPaused);
        
        if (isPaused)
        {
            PositionUIInFrontOfPlayer(pauseUI);

            if (taskUI != null) taskUI.SetActive(false);
            if (notebookUI != null) notebookUI.SetActive(false);
        }
        
        // Freeze game if paused, resume if not
        Time.timeScale = isPaused ? 0f : 1f;
        
        // Disable movement and turning while paused
        if (locomotionSystem != null)
        {
            locomotionSystem.SetActive(!isPaused);
        }
    }

    public void ResumeGame()
    {
        if (pauseUI == null) return;
        isPaused = false;
        pauseUI.SetActive(false);
        Time.timeScale = 1f;
        
        // Re-enable movement
        if (locomotionSystem != null)
        {
            locomotionSystem.SetActive(true);
        }
    }

    public void ResetTools()
    {
        if (toolResetManager != null)
        {
            toolResetManager.ResetAllTools();
        }
        
        // Optional: Automatically resume the game after resetting tools
        ResumeGame(); 
    }

    // --- OVERLAY MENUS ---

    public void ToggleTaskUI()
    {
        if (taskUI != null && !isPaused) 
        {
            taskUI.SetActive(!taskUI.activeSelf);
            if (taskUI.activeSelf) PositionUIInFrontOfPlayer(taskUI);
        }
    }

    public void ToggleNotebookUI()
    {
        if (notebookUI != null && !isPaused)
        {
            notebookUI.SetActive(!notebookUI.activeSelf);
            if (notebookUI.activeSelf) PositionUIInFrontOfPlayer(notebookUI);
        }
    }
}