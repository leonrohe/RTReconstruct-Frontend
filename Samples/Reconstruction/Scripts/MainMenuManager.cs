using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{

    [SerializeField] private GameObject RoleChoice;
    [SerializeField] private GameObject SceneChoice;
    [SerializeField] private GameObject RuntimeUI;

    private string role;
    private string scene;

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
}
