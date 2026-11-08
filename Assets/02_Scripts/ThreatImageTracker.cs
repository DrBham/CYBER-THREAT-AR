
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ThreatImageTracker : MonoBehaviour
{
    private static readonly Vector3 PhishingCenteredOffset
        = new Vector3(0.0025f, 0f, -0.0355f);

    private static readonly Vector3 RansomwareCenteredOffset
        = new Vector3(0f, 0f, -0.035f);

    [Header("AR Tracking")]
    [SerializeField] private ARTrackedImageManager imageManager;

    [Header("Threat Prefabs")]
    [SerializeField] private GameObject phishingPrefab;
    [SerializeField] private GameObject malwarePrefab;
    [SerializeField] private GameObject ransomwarePrefab;

    private readonly Dictionary<TrackableId, GameObject> spawnedObjects
        = new Dictionary<TrackableId, GameObject>();

    private void OnEnable()
    {
        if (imageManager != null)
            imageManager.trackedImagesChanged += OnImagesChanged;
    }

    private void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackedImagesChanged -= OnImagesChanged;
    }

    private void OnImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (ARTrackedImage image in args.added)
        {
            CreateThreatObject(image);
            UpdateVisibility(image);
        }

        foreach (ARTrackedImage image in args.updated)
        {
            UpdateVisibility(image);
        }

        foreach (ARTrackedImage image in args.removed)
        {
            if (spawnedObjects.TryGetValue(image.trackableId, out GameObject obj))
            {
                if (obj != null)
                    Destroy(obj);

                spawnedObjects.Remove(image.trackableId);
            }
        }
    }

    private void CreateThreatObject(ARTrackedImage image)
    {
        if (spawnedObjects.ContainsKey(image.trackableId))
            return;

        GameObject prefab = null;
        string imageName = image.referenceImage.name;

        switch (imageName)
        {
            case "Phishing":
                prefab = phishingPrefab;
                break;

            case "Malware":
                prefab = malwarePrefab;
                break;

            case "Ransomware":
                prefab = ransomwarePrefab;
                break;
        }

        if (prefab == null)
        {
            Debug.LogWarning("No prefab assigned for: " + imageName);
            return;
        }

        GameObject instance = Instantiate(prefab, image.transform);
        if (imageName == "Phishing")
            instance.transform.localPosition = PhishingCenteredOffset;
        else if (imageName == "Ransomware")
            instance.transform.localPosition = RansomwareCenteredOffset;
        else
            instance.transform.localPosition = Vector3.zero;

        // ARTrackedImage's surface normal is local +Y, while the threat prefabs
        // are authored with their visible front facing local -Z.
        instance.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        spawnedObjects.Add(image.trackableId, instance);

        Debug.Log("Threat detected: " + imageName);
    }

    private void UpdateVisibility(ARTrackedImage image)
    {
        if (spawnedObjects.TryGetValue(image.trackableId, out GameObject obj)
            && obj != null)
        {
            obj.SetActive(image.trackingState == TrackingState.Tracking);
        }
    }
}
