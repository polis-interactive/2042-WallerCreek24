using UnityEngine;
using UnityEngine.UIElements;

public class InstallationUi : MonoBehaviour
{

    private UIDocument document;

    // framerate
    public float fpsRefreshTime = 0.5f;
    private int frameCounter = 0;
    private float timeCounter = 0.0f;
    private float lastFrameRate = 0.0f;
    private Label fpsCounterDispaly;

    // config
    private VisualElement configMenu;
    private Button openConfig;
    private Button saveConfig;
    private Button closeConfig;

    private void Setup()
    {
        // TODO: load config from file 
    }

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        if (!document)
        {
            throw new System.Exception("InstallationControls.Awake() No UI Document found");
        }
        // fps
        fpsCounterDispaly = document.rootVisualElement.Q<Label>("FrameRateValue");
        fpsCounterDispaly.text = lastFrameRate.ToString("F2");

        // config
        configMenu = document.rootVisualElement.Q("Config");
        openConfig = document.rootVisualElement.Q<Button>("EditConfig");
        openConfig.RegisterCallback<ClickEvent>(OnOpenConfigClick);
        saveConfig = document.rootVisualElement.Q<Button>("SaveConfig");
        saveConfig.RegisterCallback<ClickEvent>(OnSaveConfigClick);
        closeConfig = document.rootVisualElement.Q<Button>("CloseConfig");
        closeConfig.RegisterCallback<ClickEvent>(OnCloseConfigClick);
    }

    private void OnOpenConfigClick(ClickEvent evt)
    {
        // TODO: reset config
        saveConfig.SetEnabled(false);
        configMenu.style.display = DisplayStyle.Flex;
    }

    private void OnSaveConfigClick(ClickEvent evt)
    {
        // TODO: set config object
        // TODO: update game
        // TODO: save config file
        saveConfig.SetEnabled(false);
    }

    private void OnCloseConfigClick(ClickEvent evt)
    {
        configMenu.style.display = DisplayStyle.None;
    }

    void Start()
    {
        frameCounter = 0;
        timeCounter = 0.0f;
        lastFrameRate = 0.0f;
    }

    void Update()
    {
        if (timeCounter < fpsRefreshTime)
        {
            timeCounter += Time.deltaTime;
            frameCounter++;
        } else
        {
            lastFrameRate = (float) frameCounter / timeCounter;
            fpsCounterDispaly.text = lastFrameRate.ToString("F2");
            frameCounter = 0;
            timeCounter = 0.0f;
        }
    }
}
