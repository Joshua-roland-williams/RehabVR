// File: Assets/Scripts/HapticFeedbackController.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[System.Serializable]
public class HapticProfile
{
    [Header("Contact Feedback")]
    public float contactIntensity = 0.3f;
    public float contactDuration = 0.1f;
    
    [Header("Grab Feedback")]
    public float grabIntensity = 0.6f;
    public float grabDuration = 0.2f;
    
    [Header("Resistance Feedback")]
    public bool enableResistance = true;
    public AnimationCurve resistanceCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float maxResistanceIntensity = 0.5f;
    public float resistanceDuration = 3f;
    
    [Header("Success Feedback")]
    public float successIntensity = 0.8f;
    public float successDuration = 0.3f;
    public int successPulses = 2;
}

public class HapticFeedbackController : MonoBehaviour
{
    [Header("Haptic Configuration")]
    public HapticProfile hapticProfile;
    
    [Header("Therapeutic Settings")]
    public bool enableTherapeuticResistance = true;
    public float muscleStrengtheningIntensity = 0.4f;
    public float motorLearningFeedback = 0.3f;
    
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor currentInteractor;
    private Coroutine resistanceFeedbackCoroutine;
    private bool isProvidingResistance = false;
    
    void Awake()
{
    // Initialize haptic profile if null
    if (hapticProfile == null)
    {
        hapticProfile = new HapticProfile();
        hapticProfile.contactIntensity = 0.3f;
        hapticProfile.contactDuration = 0.1f;
        hapticProfile.grabIntensity = 0.6f;
        hapticProfile.grabDuration = 0.2f;
        hapticProfile.enableResistance = true;
        hapticProfile.maxResistanceIntensity = 0.5f;
        hapticProfile.resistanceDuration = 3f;
        hapticProfile.successIntensity = 0.8f;
        hapticProfile.successDuration = 0.3f;
        hapticProfile.successPulses = 2;
    }
    
    // Initialize curve safely
    if (hapticProfile.resistanceCurve == null || hapticProfile.resistanceCurve.keys.Length == 0)
    {
        hapticProfile.resistanceCurve = CreateDefaultResistanceCurve();
    }
}

    
    public void TriggerContactFeedback(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
    {
        SendHapticPulse(interactor, hapticProfile.contactIntensity, hapticProfile.contactDuration);
    }
    
    public void TriggerGrabFeedback(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
    {
        currentInteractor = interactor;
        
        // Initial grab confirmation
        SendHapticPulse(interactor, hapticProfile.grabIntensity, hapticProfile.grabDuration);
        
        // Start therapeutic resistance if enabled
        if (enableTherapeuticResistance && hapticProfile.enableResistance)
        {
            StartResistanceFeedback();
        }
    }
    
    public void TriggerSuccessFeedback(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
    {
        StartCoroutine(PlaySuccessFeedback(interactor));
    }
    
    public void StopAllFeedback()
    {
        if (resistanceFeedbackCoroutine != null)
        {
            StopCoroutine(resistanceFeedbackCoroutine);
            resistanceFeedbackCoroutine = null;
        }
        
        isProvidingResistance = false;
        currentInteractor = null;
    }
    
    private void StartResistanceFeedback()
    {
        if (currentInteractor == null || isProvidingResistance) return;
        
        isProvidingResistance = true;
        resistanceFeedbackCoroutine = StartCoroutine(ResistanceFeedbackLoop());
    }
    
    private IEnumerator ResistanceFeedbackLoop()
    {
        float elapsedTime = 0f;
        
        while (isProvidingResistance && currentInteractor != null && elapsedTime < hapticProfile.resistanceDuration)
        {
            float normalizedTime = elapsedTime / hapticProfile.resistanceDuration;
            float resistanceIntensity = hapticProfile.resistanceCurve.Evaluate(normalizedTime) * hapticProfile.maxResistanceIntensity;
            
            // Apply resistance feedback
            SendHapticPulse(currentInteractor, resistanceIntensity, 0.1f);
            
            elapsedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        isProvidingResistance = false;
    }
    
    private IEnumerator PlaySuccessFeedback(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
    {
        for (int i = 0; i < hapticProfile.successPulses; i++)
        {
            SendHapticPulse(interactor, hapticProfile.successIntensity, hapticProfile.successDuration / hapticProfile.successPulses);
            yield return new WaitForSeconds(hapticProfile.successDuration / hapticProfile.successPulses + 0.1f);
        }
    }
    
    // FIX: Updated haptic pulse method to properly access xrController
    private void SendHapticPulse(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor, float intensity, float duration)
    {
        if (interactor == null) return;
        
        // Clamp values to safe ranges
        intensity = Mathf.Clamp01(intensity);
        duration = Mathf.Clamp(duration, 0f, 1f);
        
        // FIX: Cast to XRBaseControllerInteractor to access xrController
        UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor baseControllerInteractor = interactor as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor;
        if (baseControllerInteractor != null && baseControllerInteractor.xrController != null)
        {
            baseControllerInteractor.xrController.SendHapticImpulse(intensity, duration);
        }
        else
        {
            // Alternative approach for other interactor types
            UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor directInteractor = interactor as UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor;
            if (directInteractor != null && directInteractor.xrController != null)
            {
                directInteractor.xrController.SendHapticImpulse(intensity, duration);
            }
            else
            {
                UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor = interactor as UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor;
                if (rayInteractor != null && rayInteractor.xrController != null)
                {
                    rayInteractor.xrController.SendHapticImpulse(intensity, duration);
                }
            }
        }
    }
    
    private AnimationCurve CreateDefaultResistanceCurve()
    {
        // Create a curve that gradually increases resistance, then decreases
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);      // Start with no resistance
        curve.AddKey(0.3f, 0.5f);  // Gradually increase
        curve.AddKey(0.7f, 1f);    // Peak resistance
        curve.AddKey(1f, 0.3f);    // Taper off
        
        // Ensure smooth transitions
        for (int i = 0; i < curve.keys.Length; i++)
        {
            curve.SmoothTangents(i, 0f);
        }
        
        return curve;
    }
    
    // Public methods for external control
    public void SetResistanceIntensity(float intensity)
    {
        hapticProfile.maxResistanceIntensity = Mathf.Clamp01(intensity);
    }
    
    public void SetTherapeuticMode(bool enableMuscleStrengthening, bool enableMotorLearning)
    {
        if (enableMuscleStrengthening)
        {
            hapticProfile.maxResistanceIntensity = muscleStrengtheningIntensity;
            hapticProfile.resistanceDuration = 5f; // Longer duration for strength training
        }
        
        if (enableMotorLearning)
        {
            hapticProfile.contactIntensity = motorLearningFeedback;
            hapticProfile.grabIntensity = motorLearningFeedback;
        }
    }
    
    // Debugging visualization
    void OnDrawGizmosSelected()
    {
        if (isProvidingResistance)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
