using System.Collections;
using System.Collections.Generic;
using RTReconstruct.Collectors.SLAM3R;
using RTReconstruct.Collectors.NeuralRecon;
using RTReconstruct.Networking;
using TMPro;
using UnityEngine;
using RTReconstruct.Collectors.Default;

public class UIManager : MonoBehaviour
{

    [Header("General Menus")]
    [SerializeField] private GameObject RoleChoice;
    [SerializeField] private GameObject SceneChoice;
    [SerializeField] private GameObject RuntimeUI;

    [Header("Runtime UI")]
    [SerializeField] private TMP_Dropdown ModelDropdown;

    private string role;
    private string scene;

    void Start()
    {
        ModelDropdown.onValueChanged.AddListener(OnModelDropdownChange);
    }

    public void ChooseHost()
    {
        role = "host";
        RoleChoice.SetActive(false);
        SceneChoice.SetActive(true);
    }

    public void ChooseVisitor()
    {
        role = "visitor";
        RoleChoice.SetActive(false);
        SceneChoice.SetActive(true);
    }

    public void SetScene()
    {
        scene = SceneChoice.GetComponentInChildren<TMP_InputField>().text;
        FindAnyObjectByType<ReconstructionManager>().InitReconstruction(role, scene);


        StartCoroutine(AddAvailableModelsToDrowndown());
    }

    public void OnModelDropdownChange(int index)
    {
        ReconstructionManager reconstructionManager = GameObject.Find("Reconstruction Manager").GetComponent<ReconstructionManager>();

        string model = ModelDropdown.options[index].text;
        switch (model)
        {
            case "neucon":
                reconstructionManager.SetCollector(new NeuralReconCollector());
                break;
            case "slam3r":
                reconstructionManager.SetCollector(new SLAM3RCollector());
                break;
            default:
                reconstructionManager.SetCollector(new DefaultCollector(model));
                break;
        }
    }

    private IEnumerator AddAvailableModelsToDrowndown()
    {
        yield return new WaitForSeconds(1);

        ModelDropdown.ClearOptions();
        ModelDropdown.AddOptions(ReconstructionClient.Instance.AvailableModels);

        SceneChoice.SetActive(false);
        RuntimeUI.SetActive(true);

        OnModelDropdownChange(0);
    }
}
