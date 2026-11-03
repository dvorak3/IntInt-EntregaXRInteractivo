using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public sealed class GrabStatusUI : MonoBehaviour
{
    [SerializeField] private Text statusText;

    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        SetStatus("Acércate y agarra el objeto.");
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
    }

    public void SetStatusText(Text text)
    {
        statusText = text;
        SetStatus("Acércate y agarra el objeto.");
    }

    private void OnSelected(SelectEnterEventArgs _) => SetStatus("¡Objeto agarrado!");

    private void OnDeselected(SelectExitEventArgs _) => SetStatus("Objeto soltado.");

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
