using Blackout.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Blackout.UI
{
    /// <summary>
    /// Center-screen crosshair plus the prompt of whatever the <see cref="PlayerInteractor"/> is looking at.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractor interactor;
        [Tooltip("Used only to show the bound key, e.g. \"[E]\".")]
        [SerializeField] private InputActionReference interact;
        [Tooltip("Binding group whose key is shown. Empty shows every binding.")]
        [SerializeField] private string controlScheme = "Keyboard&Mouse";
        [SerializeField] private Graphic crosshair;
        [SerializeField] private Text promptText;

        [Header("Crosshair")]
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color focusColor = Color.white;
        [SerializeField] private float idleScale = 1f;
        [SerializeField] private float focusScale = 1.6f;

        private void OnEnable()
        {
            if (interactor != null)
            {
                interactor.FocusChanged += Show;
                Show(interactor.Current);
            }
        }

        private void OnDisable()
        {
            if (interactor != null)
                interactor.FocusChanged -= Show;
        }

        private void Show(IInteractable target)
        {
            bool focused = target != null;

            if (crosshair != null)
            {
                crosshair.color = focused ? focusColor : idleColor;
                crosshair.rectTransform.localScale = Vector3.one * (focused ? focusScale : idleScale);
            }

            if (promptText != null)
            {
                promptText.enabled = focused;
                if (focused)
                    promptText.text = $"[{KeyLabel()}] {target.Prompt}";
            }
        }

        private string KeyLabel()
        {
            if (interact == null)
                return "E";
            return string.IsNullOrEmpty(controlScheme)
                ? interact.action.GetBindingDisplayString()
                : interact.action.GetBindingDisplayString(InputBinding.MaskByGroup(controlScheme));
        }
    }
}
