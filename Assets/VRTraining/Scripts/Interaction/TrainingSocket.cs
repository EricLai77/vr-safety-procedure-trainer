using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRTraining
{
    [RequireComponent(typeof(XRSocketInteractor))]
    [DisallowMultipleComponent]
    public sealed class TrainingSocket : MonoBehaviour
    {
        [SerializeField]
        private EquipmentId socketId;

        [SerializeField]
        private TrainingManager trainingManager;

        [Header("Socket Feedback")]

        [SerializeField]
        private Renderer socketVisualRenderer;

        [SerializeField]
        private Color idleColour =
            new Color(0.08f, 0.25f, 0.8f, 1f);

        [SerializeField]
        private Color validHoverColour =
            new Color(1f, 0.65f, 0.05f, 1f);

        [SerializeField]
        private Color invalidHoverColour =
            new Color(0.9f, 0.08f, 0.05f, 1f);

        [SerializeField]
        private Color occupiedColour =
            new Color(0.05f, 0.8f, 0.2f, 1f);

        [SerializeField]
        [Range(1f, 1.15f)]
        private float hoverPulseScale = 1.05f;

        [SerializeField]
        [Min(0f)]
        private float hoverPulseSpeed = 5f;

        private XRSocketInteractor socketInteractor;
        private Transform socketVisualTransform;
        private Vector3 socketVisualBaseScale;
        private MaterialPropertyBlock materialPropertyBlock;
        private readonly HashSet<IXRHoverInteractable>
            hoveredInteractables =
                new HashSet<IXRHoverInteractable>();
        private bool hasSelection;
        private bool selectedItemMatches;

        private static readonly int BaseColourProperty =
            Shader.PropertyToID("_BaseColor");

        private static readonly int ColourProperty =
            Shader.PropertyToID("_Color");

        private void Awake()
        {
            socketInteractor =
                GetComponent<XRSocketInteractor>();

            ResolveSocketVisual();
            ApplyVisualState();
        }

        private void OnEnable()
        {
            socketInteractor.selectEntered.AddListener(
                HandleSelectEntered);

            socketInteractor.selectExited.AddListener(
                HandleSelectExited);

            socketInteractor.hoverEntered.AddListener(
                HandleHoverEntered);

            socketInteractor.hoverExited.AddListener(
                HandleHoverExited);
        }

        private void OnDisable()
        {
            socketInteractor.selectEntered.RemoveListener(
                HandleSelectEntered);

            socketInteractor.selectExited.RemoveListener(
                HandleSelectExited);

            socketInteractor.hoverEntered.RemoveListener(
                HandleHoverEntered);

            socketInteractor.hoverExited.RemoveListener(
                HandleHoverExited);

            hoveredInteractables.Clear();
            hasSelection = false;
            selectedItemMatches = false;
            ApplyVisualState();
        }

        private void Update()
        {
            if (socketVisualTransform == null)
            {
                return;
            }

            if (!hasSelection && hoveredInteractables.Count > 0)
            {
                var pulse =
                    (Mathf.Sin(
                         Time.unscaledTime * hoverPulseSpeed) +
                     1f) * 0.5f;

                socketVisualTransform.localScale =
                    socketVisualBaseScale *
                    Mathf.Lerp(1f, hoverPulseScale, pulse);

                return;
            }

            socketVisualTransform.localScale =
                Vector3.Lerp(
                    socketVisualTransform.localScale,
                    socketVisualBaseScale,
                    Time.unscaledDeltaTime * 12f);
        }

        private void HandleHoverEntered(
            HoverEnterEventArgs eventArgs)
        {
            hoveredInteractables.Add(
                eventArgs.interactableObject);

            ApplyVisualState();
        }

        private void HandleHoverExited(
            HoverExitEventArgs eventArgs)
        {
            hoveredInteractables.Remove(
                eventArgs.interactableObject);

            ApplyVisualState();
        }

        private void HandleSelectEntered(
            SelectEnterEventArgs eventArgs)
        {
            var item = GetInspectableItem(
                eventArgs.interactableObject);

            hasSelection = true;
            selectedItemMatches =
                item != null && item.Id == socketId;

            ApplyVisualState();

            if (trainingManager == null)
            {
                Debug.LogError(
                    $"{name} has no TrainingManager assigned.");

                return;
            }

            if (item == null)
            {
                Debug.LogWarning(
                    $"{name} received an object without " +
                    "an InspectableItem component.");

                return;
            }

            trainingManager.Submit(
                item.Id,
                socketId);
        }

        private void HandleSelectExited(
            SelectExitEventArgs eventArgs)
        {
            hasSelection = false;
            selectedItemMatches = false;
            ApplyVisualState();
        }

        private InspectableItem GetInspectableItem(
            IXRInteractable interactable)
        {
            if (interactable == null)
            {
                return null;
            }

            return interactable
                .transform
                .GetComponentInParent<InspectableItem>();
        }

        private void ResolveSocketVisual()
        {
            if (socketVisualRenderer == null)
            {
                var visual = transform.Find("SocketVisual");

                if (visual != null)
                {
                    socketVisualRenderer =
                        visual.GetComponentInChildren<Renderer>(true);
                }
            }

            if (socketVisualRenderer == null)
            {
                return;
            }

            socketVisualTransform =
                socketVisualRenderer.transform;

            socketVisualBaseScale =
                socketVisualTransform.localScale;

            materialPropertyBlock =
                new MaterialPropertyBlock();
        }

        private void ApplyVisualState()
        {
            if (socketVisualRenderer == null ||
                materialPropertyBlock == null)
            {
                return;
            }

            var colour = idleColour;

            if (hasSelection)
            {
                colour = selectedItemMatches
                    ? occupiedColour
                    : invalidHoverColour;
            }
            else if (TryGetHoveredItem(out var hoveredItem))
            {
                colour = hoveredItem.Id == socketId
                    ? validHoverColour
                    : invalidHoverColour;
            }

            socketVisualRenderer.GetPropertyBlock(
                materialPropertyBlock);

            materialPropertyBlock.SetColor(
                BaseColourProperty,
                colour);

            materialPropertyBlock.SetColor(
                ColourProperty,
                colour);

            socketVisualRenderer.SetPropertyBlock(
                materialPropertyBlock);
        }

        private bool TryGetHoveredItem(
            out InspectableItem item)
        {
            foreach (var hovered in hoveredInteractables)
            {
                item = GetInspectableItem(hovered);

                if (item != null)
                {
                    return true;
                }
            }

            item = null;
            return false;
        }
    }
}
