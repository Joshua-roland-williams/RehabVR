// File: Assets/Scripts/Container.cs
using UnityEngine;

public class Container : MonoBehaviour
{
    [Header("Container Properties")]
    public ContainerType containerType;
    public float capacity = 1f;
    public float currentFillLevel = 0f;
    public bool requiresTwoHands = false;
    
    [Header("Pouring Mechanics")]
    public bool canPour = true;
    public Transform pourPoint;
    public ParticleSystem pourEffect;
    public AudioClip pourSound;
    
    public enum ContainerType
    {
        Cup,
        Mug,
        Bowl,
        Pot,
        Pan,
        Pitcher,
        Jar
    }
    
    private RehabGrabbableObject grabbableComponent;
    private AudioSource audioSource;
    
    void Start()
    {
        grabbableComponent = GetComponent<RehabGrabbableObject>();
        if (grabbableComponent == null)
        {
            grabbableComponent = gameObject.AddComponent<RehabGrabbableObject>();
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        ConfigureContainerProperties();
    }
    
    private void ConfigureContainerProperties()
    {
        if (grabbableComponent == null) return;
        
        switch (containerType)
        {
            case ContainerType.Cup:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.weightSimulation = 0.1f + (currentFillLevel * 0.2f);
                grabbableComponent.therapySettings.objectID = "cup_" + GetInstanceID();
                break;
                
            case ContainerType.Mug:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.weightSimulation = 0.15f + (currentFillLevel * 0.25f);
                grabbableComponent.therapySettings.objectID = "mug_" + GetInstanceID();
                break;
                
            case ContainerType.Bowl:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.trainsManipulation = true;
                grabbableComponent.therapySettings.weightSimulation = 0.2f + (currentFillLevel * 0.3f);
                grabbableComponent.therapySettings.objectID = "bowl_" + GetInstanceID();
                break;
                
            case ContainerType.Pot:
                grabbableComponent.therapySettings.trainsBimanual = true;
                grabbableComponent.therapySettings.weightSimulation = 0.5f + (currentFillLevel * 0.8f);
                grabbableComponent.therapySettings.difficultyLevel = 2f;
                grabbableComponent.therapySettings.objectID = "pot_" + GetInstanceID();
                requiresTwoHands = true;
                break;
                
            case ContainerType.Pan:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.trainsManipulation = true;
                grabbableComponent.therapySettings.weightSimulation = 0.4f + (currentFillLevel * 0.6f);
                grabbableComponent.therapySettings.difficultyLevel = 1.5f;
                grabbableComponent.therapySettings.objectID = "pan_" + GetInstanceID();
                break;
                
            case ContainerType.Pitcher:
                grabbableComponent.therapySettings.trainsGrasping = true;
                grabbableComponent.therapySettings.trainsManipulation = true;
                grabbableComponent.therapySettings.weightSimulation = 0.3f + (currentFillLevel * 1f);
                grabbableComponent.therapySettings.objectID = "pitcher_" + GetInstanceID();
                canPour = true;
                break;
                
            case ContainerType.Jar:
                grabbableComponent.therapySettings.trainsBimanual = true;
                grabbableComponent.therapySettings.weightSimulation = 0.25f + (currentFillLevel * 0.5f);
                grabbableComponent.therapySettings.difficultyLevel = 1.8f;
                grabbableComponent.therapySettings.objectID = "jar_" + GetInstanceID();
                canPour = false;
                break;
        }
        
        // Update required hands setting
        grabbableComponent.therapySettings.requiresTwoHands = requiresTwoHands;
        
        // Update the grabbable object with new settings
        grabbableComponent.UpdateObjectProperties();
    }
    
    public void StartPouring()
    {
        if (!canPour || currentFillLevel <= 0f) return;
        
        if (pourEffect != null)
        {
            pourEffect.Play();
        }
        
        if (audioSource && pourSound)
        {
            audioSource.clip = pourSound;
            audioSource.Play();
        }
        
        Debug.Log($"Started pouring from {containerType}");
    }
    
    public void StopPouring()
    {
        if (pourEffect != null)
        {
            pourEffect.Stop();
        }
        
        if (audioSource)
        {
            audioSource.Stop();
        }
        
        Debug.Log($"Stopped pouring from {containerType}");
    }
    
    public void SetFillLevel(float fillLevel)
    {
        currentFillLevel = Mathf.Clamp01(fillLevel);
        ConfigureContainerProperties(); // Update weight based on fill level
        
        Debug.Log($"{containerType} fill level set to: {currentFillLevel:F2}");
    }
    
    public float GetFillLevel()
    {
        return currentFillLevel;
    }
    
    public bool CanPour()
    {
        return canPour && currentFillLevel > 0f;
    }
    
    public void AddLiquid(float amount)
    {
        SetFillLevel(currentFillLevel + amount);
    }
    
    public void RemoveLiquid(float amount)
    {
        SetFillLevel(currentFillLevel - amount);
    }
}
