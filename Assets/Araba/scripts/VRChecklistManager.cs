using System.Collections.Generic;
using UnityEngine;
public class VRChecklistManager : MonoBehaviour
{
    public Transform contentContainer;
    public GameObject checklistItemPrefab;
    public List<string> tasks = new List<string>
    {
        "Check for physical risks (impact, looseness, arcing, leakage).",
        "If no abnormalities, secure the operation area within the laboratory cage.",
        "Remove and lock the High Voltage (HV) manual service disconnect (MSD).",
        "Disconnect the 12V battery.",
        "Confirm de-energization on the dashboard (perform manual test if necessary).",
        "Test the high voltage tester on the 12V battery (Red:+, Blue:-).",
        "Short-circuit the gigaohmmeter to verify zero error.",
        "Test the internal integrity of the intermediate measuring unit with a multimeter.",
        "Connect the probes and confirm one last time that the system is de-energized.",
        "Set the gigaohmmeter to the proper voltage (e.g., 500V) and run HV+ isolation test (vs chassis).",
        "Switch the probe and run the HV- isolation test (vs chassis).",
        "If present, repeat isolation tests for additional components (e.g., A/C compressor).",
        "Confirm and report that the results comply with the standard (500 ohm/V)."
    };
    private List<VRChecklistItem> instantiatedItems = new List<VRChecklistItem>();
    private void Start()
    {
        populateChecklist();
        initializeSequentialLogic();
    }
    private void populateChecklist()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
        instantiatedItems.Clear();
        foreach (string taskText in tasks)
        {
            GameObject newItem = Instantiate(checklistItemPrefab, contentContainer);
            VRChecklistItem itemScript = newItem.GetComponent<VRChecklistItem>();
            if (itemScript != null)
            {
                if (itemScript.taskText != null)
                {
                    itemScript.taskText.text = taskText;
                }
                if (itemScript.checkbox != null)
                {
                    itemScript.checkbox.isOn = false;
                }
                instantiatedItems.Add(itemScript);
            }
        }
    }
    private void initializeSequentialLogic()
    {
        for (int i = 0; i < instantiatedItems.Count; i++)
        {
            int index = i; 
            var item = instantiatedItems[i];
            item.checkbox.onValueChanged.AddListener((isOn) => onItemToggled(index, isOn));
            item.setInteractable(i == 0);
        }
    }
    private void onItemToggled(int index, bool isOn)
    {
        if (isOn)
        {
            if (index + 1 < instantiatedItems.Count)
            {
                var nextItem = instantiatedItems[index + 1];
                if (nextItem != null)
                {
                    nextItem.setInteractable(true);
                }
            }
        }
        else
        {
            for (int i = index + 1; i < instantiatedItems.Count; i++)
            {
                instantiatedItems[i].checkbox.isOn = false;
                instantiatedItems[i].setInteractable(false);
            }
        }
    }
}