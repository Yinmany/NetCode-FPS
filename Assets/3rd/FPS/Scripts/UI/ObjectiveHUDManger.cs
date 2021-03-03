using System.Collections.Generic;
using UnityEngine;

public class ObjectiveHUDManger : MonoBehaviour
{
    [Tooltip("UI panel containing the layoutGroup for displaying objectives")]
    public RectTransform objectivePanel;
    [Tooltip("Prefab for the primary objectives")]
    public GameObject primaryObjectivePrefab;
    [Tooltip("Prefab for the primary objectives")]
    public GameObject secondaryObjectivePrefab;

    Dictionary<Objective, ObjectiveToast> m_ObjectivesDictionnary;

    void Awake()
    {
        m_ObjectivesDictionnary = new Dictionary<Objective, ObjectiveToast>();
    }

    public void RegisterObjective(Objective objective)
    {
        objective.onUpdateObjective += OnUpdateObjective;

        // instanciate the Ui element for the new objective
        GameObject objectiveUIInstance = Instantiate(objective.isOptional ? secondaryObjectivePrefab : primaryObjectivePrefab, objectivePanel);

        if (!objective.isOptional)
            objectiveUIInstance.transform.SetSiblingIndex(0);

        ObjectiveToast toast = objectiveUIInstance.GetComponent<ObjectiveToast>();
        DebugUtility.HandleErrorIfNullGetComponent<ObjectiveToast, ObjectiveHUDManger>(toast, this, objectiveUIInstance.gameObject);

        // initialize the element and give it the objective description
        toast.Initialize(objective.title, objective.description, "", objective.isOptional, objective.delayVisible);

        m_ObjectivesDictionnary.Add(objective, toast);

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(objectivePanel);
    }

    public void UnregisterObjective(Objective objective)
    {
        objective.onUpdateObjective -= OnUpdateObjective;

        // if the objective if in the list, make it fade out, and remove it from the list
        if (m_ObjectivesDictionnary.TryGetValue(objective, out ObjectiveToast toast) && toast != null)
        {
            toast.Complete();
        }
        m_ObjectivesDictionnary.Remove(objective);
    }

    void OnUpdateObjective(UnityActionUpdateObjective updateObjective)
    {
        if (m_ObjectivesDictionnary.TryGetValue(updateObjective.objective, out ObjectiveToast toast) && toast != null)
            //&& !string.IsNullOrEmpty(descriptionText))
        {
            // set the new updated description for the objective, and forces the content size fitter to be recalculated
            Canvas.ForceUpdateCanvases();
            if (!string.IsNullOrEmpty(updateObjective.descriptionText))
                toast.descriptionTextContent.text = updateObjective.descriptionText;

            if (!string.IsNullOrEmpty(updateObjective.counterText))
                toast.counterTextContent.text = updateObjective.counterText;

            if (toast.GetComponent<RectTransform>())
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(toast.GetComponent<RectTransform>());
            }
        }
    }
}
