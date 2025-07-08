using System.Collections;
using System.Collections.Generic;
using RTReconstruct.Collector.SLAM3R;
using RTReconstruct.Collectors.NeuralRecon;
using TMPro;
using UnityEngine;

public class ModelSelector : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    void Start()
    {
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    void OnDropdownValueChanged(int index)
    {
        if (index == 0)
        {
            GameObject.Find("ReconstructionManager").GetComponent<ReconstructionManager>().SetCollector(new NeuralReconCollector());
        }
        
        if (index == 1)
        {
            GameObject.Find("ReconstructionManager").GetComponent<ReconstructionManager>().SetCollector(new SLAM3RCollector());
        }
    }
}
