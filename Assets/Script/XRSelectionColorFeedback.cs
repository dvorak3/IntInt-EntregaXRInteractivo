using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public sealed class XRSelectionColorFeedback : MonoBehaviour
{
    [SerializeField] private Color selectedColor = new Color(1f, 0.72f, 0.12f);

    private Renderer[] renderers;
    private Color[] originalColors;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (var i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnSelected);
        grabInteractable.selectExited.AddListener(OnDeselected);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnSelected);
        grabInteractable.selectExited.RemoveListener(OnDeselected);
        SetColor(false);
    }

    private void OnSelected(SelectEnterEventArgs _) => SetColor(true);

    private void OnDeselected(SelectExitEventArgs _) => SetColor(grabInteractable.isSelected);

    private void SetColor(bool selected)
    {
        if (renderers == null)
            return;

        for (var i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = selected ? selectedColor : originalColors[i];
        }
    }
}
