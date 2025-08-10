using System.Collections;
using System.Collections.Generic;
using RTReconstruct.Collector.SLAM3R;
using RTReconstruct.Collectors.NeuralRecon;
using TMPro;
using UnityEngine;

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
        SceneChoice.SetActive(false);
        RuntimeUI.SetActive(true);
        FindAnyObjectByType<ReconstructionManager>().InitReconstruction(role, scene);
    }

    public void OnModelDropdownChange(int index)
    {
        if (index == 0)
        {
            GameObject.Find("Reconstruction Manager").GetComponent<ReconstructionManager>().SetCollector(new NeuralReconCollector());
        }
        else if (index == 1)
        {
            GameObject.Find("Reconstruction Manager").GetComponent<ReconstructionManager>().SetCollector(new SLAM3RCollector());
        }
    }
}
