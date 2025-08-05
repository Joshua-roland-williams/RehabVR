// File: Assets/Scripts/KitchenUtensil.cs
using UnityEngine;

public class KitchenUtensil : MonoBehaviour
{
    [Header("Utensil Properties")]
    public UtensilType utensilType;
    public float optimalGripForce = 0.5f;
    public bool requiresPreciseGrip = true;
    
    [Header("Task-Specific Settings")]
    public bool canCut = false;
    public bool canStir = false;
    public bool canPour = false;
    public bool canFlip = false;
    
    public enum UtensilType
    {
        Spoon,
        Fork,
        Knife,
        Spatula,
        Whisk,
        Ladle,
        Tongs
    }
    
    private RehabGrabbableObject grabbableComponent;
    
    void Start()
    {
        grabbableComponent = GetComponent<RehabGrabbableObject>();
        if (grabbableComponent == null)
        {
            grabbableComponent = gameObject.AddComponent<RehabGrabbableObject>();
        }
        
        ConfigureUtensilProperties();
    }
    
    private void ConfigureUtensilProperties()
    {
        if (grabbableComponent == null) return;
        
        switch (utensilType)
        {
            case UtensilType.Spoon:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.trainsManipulation = true;
                grabbableComponent.therapySettings.weightSimulation = 0.05f;
                grabbableComponent.therapySettings.objectID = "spoon_" + GetInstanceID();
                canStir = true;
                break;
                
            case UtensilType.Fork:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.weightSimulation = 0.06f;
                grabbableComponent.therapySettings.objectID = "fork_" + GetInstanceID();
                requiresPreciseGrip = true;
                break;
                
            case UtensilType.Knife:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.trainsManipulation = true;
                grabbableComponent.therapySettings.weightSimulation = 0.15f;
                grabbableComponent.therapySettings.difficultyLevel = 2f;
                grabbableComponent.therapySettings.objectID = "knife_" + GetInstanceID();
                canCut = true;
                break;
                
            case UtensilType.Spatula:
                grabbableComponent.therapySettings.trainsManipulation = true;
                grabbableComponent.therapySettings.weightSimulation = 0.08f;
                grabbableComponent.therapySettings.objectID = "spatula_" + GetInstanceID();
                canFlip = true;
                break;
                
            case UtensilType.Whisk:
                grabbableComponent.therapySettings.trainsManipulation = true;
                grabbableComponent.therapySettings.trainsBimanual = true;
                grabbableComponent.therapySettings.weightSimulation = 0.12f;
                grabbableComponent.therapySettings.objectID = "whisk_" + GetInstanceID();
                canStir = true;
                break;
                
            case UtensilType.Ladle:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.trainsManipulation = true;
                grabbableComponent.therapySettings.weightSimulation = 0.2f;
                grabbableComponent.therapySettings.objectID = "ladle_" + GetInstanceID();
                canPour = true;
                break;
                
            case UtensilType.Tongs:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.trainsBimanual = false;
                grabbableComponent.therapySettings.weightSimulation = 0.1f;
                grabbableComponent.therapySettings.difficultyLevel = 1.5f;
                grabbableComponent.therapySettings.objectID = "tongs_" + GetInstanceID();
                break;
        }
        
        // Update the grabbable object with new settings
        grabbableComponent.UpdateObjectProperties();
    }
    
    public bool CanPerformTask(string taskType)
    {
        switch (taskType.ToLower())
        {
            case "cut":
                return canCut;
            case "stir":
                return canStir;
            case "pour":
                return canPour;
            case "flip":
                return canFlip;
            default:
                return false;
        }
    }
    
    public void PerformTask(string taskType)
    {
        if (CanPerformTask(taskType))
        {
            Debug.Log($"{utensilType} performing {taskType} task");
            
            // Add task-specific logic here
            switch (taskType.ToLower())
            {
                case "cut":
                    PerformCuttingTask();
                    break;
                case "stir":
                    PerformStirringTask();
                    break;
                case "pour":
                    PerformPouringTask();
                    break;
                case "flip":
                    PerformFlippingTask();
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"{utensilType} cannot perform {taskType} task");
        }
    }
    
    private void PerformCuttingTask()
    {
        // Implement cutting motion detection and feedback
        Debug.Log("Performing cutting motion");
    }
    
    private void PerformStirringTask()
    {
        // Implement stirring motion detection and feedback
        Debug.Log("Performing stirring motion");
    }
    
    private void PerformPouringTask()
    {
        // Implement pouring motion detection and feedback
        Debug.Log("Performing pouring motion");
    }
    
    private void PerformFlippingTask()
    {
        // Implement flipping motion detection and feedback
        Debug.Log("Performing flipping motion");
    }
}
