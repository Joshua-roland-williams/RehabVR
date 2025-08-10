using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using System.Collections;

public class FragileObject : RehabilitationObject
{
    [Header("Fragile Properties")]
    public float breakThreshold = 15f; // Force in Newtons
    public float warningThreshold = 10f; // When to start visual warnings
    public GameObject brokenVersion;
    public AudioClip breakSound;
    public AudioClip warningSound;
    public ParticleSystem breakParticles;
    
    [Header("Visual Feedback")]
    public Renderer objectRenderer;
    public Material normalMaterial;
    public Material stressMaterial;
    public Material criticalMaterial;
    
    [Header("Haptic Feedback")]
    public bool enableHapticWarnings = true;
    public float hapticIntensity = 0.3f;
    
    private bool isBroken = false;
    private float currentPressure = 0f;
    private bool warningTriggered = false;
    private Coroutine pressureMonitorCoroutine;
    
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        
        if (!isBroken)
        {
            warningTriggered = false;
            pressureMonitorCoroutine = StartCoroutine(MonitorGripPressure(args));
        }
    }
    
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        
        if (pressureMonitorCoroutine != null)
        {
            StopCoroutine(pressureMonitorCoroutine);
        }
        
        if (objectRenderer != null && normalMaterial != null)
            objectRenderer.material = normalMaterial;
            
        currentPressure = 0f;
        warningTriggered = false;
    }
    
    private IEnumerator MonitorGripPressure(SelectEnterEventArgs args)
    {
        while (isSelected && !isBroken)
        {
            currentPressure = CalculateGripPressure();
            
            // Handle visual feedback based on pressure
            UpdateVisualFeedback();
            
            // Handle haptic feedback
            if (enableHapticWarnings && currentPressure > warningThreshold)
            {
                TriggerHapticFeedback(args);
            }
            
            // Warning audio feedback
            if (!warningTriggered && currentPressure > warningThreshold)
            {
                warningTriggered = true;
                if (warningSound != null && audioSource != null)
                    audioSource.PlayOneShot(warningSound);
            }
            
            // Break if pressure too high
            if (currentPressure > breakThreshold)
            {
                BreakObject();
                yield break;
            }
            
            yield return new WaitForFixedUpdate();
        }
    }
    
    private void UpdateVisualFeedback()
    {
        if (objectRenderer == null) return;
        
        if (currentPressure > breakThreshold * 0.9f && criticalMaterial != null)
        {
            objectRenderer.material = criticalMaterial;
        }
        else if (currentPressure > warningThreshold && stressMaterial != null)
        {
            objectRenderer.material = stressMaterial;
        }
        else if (normalMaterial != null)
        {
            objectRenderer.material = normalMaterial;
        }
    }
    
    private void TriggerHapticFeedback(SelectEnterEventArgs args)
    {
        // Calculate haptic intensity based on pressure
        float intensity = Mathf.Lerp(0f, hapticIntensity, 
            (currentPressure - warningThreshold) / (breakThreshold - warningThreshold));
        
        // Send haptic impulse to controller
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor directInteractor)
        {
            var controller = directInteractor.GetComponent<ActionBasedController>();
            if (controller != null)
            {
                controller.SendHapticImpulse(intensity, 0.1f);
            }
        }
    }
    
    private float CalculateGripPressure()
    {
        // Simplified pressure calculation based on controller input
        // In a real implementation, this would use haptic feedback data
        if (isSelected)
        {
            // Simulate pressure based on grip trigger value
            float triggerValue = 0f;
            
            // Get trigger input from controller
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out triggerValue))
            {
                return triggerValue * 20f; // Convert to approximate Newtons
            }
        }
        return 0f;
    }
    
    private void BreakObject()
    {
        isBroken = true;
        
        // Audio feedback
        if (breakSound != null && audioSource != null)
            audioSource.PlayOneShot(breakSound);
            
        // Particle effect
        if (breakParticles != null)
            breakParticles.Play();
            
        // Replace with broken version
        if (brokenVersion != null)
        {
            GameObject broken = Instantiate(brokenVersion, transform.position, transform.rotation);
            
            // Copy velocity to broken pieces for realistic physics
            Rigidbody brokenRb = broken.GetComponent<Rigidbody>();
            Rigidbody originalRb = GetComponent<Rigidbody>();
            if (brokenRb != null && originalRb != null)
            {
                brokenRb.linearVelocity = originalRb.linearVelocity;
                brokenRb.angularVelocity = originalRb.angularVelocity;
            }
            
            Destroy(gameObject, 0.1f);
        }
        
        // Record failure in performance tracker
        wasGrippedCorrectly = false;
        performanceTracker?.OnObjectReleased(this);
        
        Debug.Log($"Fragile object {name} broke due to excessive pressure: {currentPressure:F1}N");
    }
    
    public void Reset()
    {
        isBroken = false;
        currentPressure = 0f;
        warningTriggered = false;
        
        if (objectRenderer != null && normalMaterial != null)
            objectRenderer.material = normalMaterial;
    }
    
    public float GetCurrentPressure()
    {
        return currentPressure;
    }
    
    public float GetPressurePercentage()
    {
        return currentPressure / breakThreshold;
    }
}
