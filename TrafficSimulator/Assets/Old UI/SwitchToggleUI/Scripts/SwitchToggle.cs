using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SwitchToggle : MonoBehaviour 
{
   [SerializeField] RectTransform _UIHandleRectTransform;
   [SerializeField] private Color _backgroundActiveColor;
   [SerializeField] private Color _handleActiveColor;

   private Image _backgroundImage; 
   private Image _handleImage;

   private Color _backgroundDefaultColor;
   private Color _handleDefaultColor;

   private Toggle _toggle;

   private Vector2 _handlePosition;

   void Awake()
   {
      _toggle = GetComponent<Toggle>();

      _handlePosition = _UIHandleRectTransform.anchoredPosition;

      _backgroundImage = _UIHandleRectTransform.parent.GetComponent<Image>();
      _handleImage = _UIHandleRectTransform.GetComponent<Image>();

      _backgroundDefaultColor = _backgroundImage.color;
      _handleDefaultColor = _handleImage.color;

      _toggle.onValueChanged.AddListener(OnSwitch);

      if(_toggle.isOn)
         OnSwitch(true);
   }

   void OnSwitch(bool on)
   {
      //uiHandleRectTransform.anchoredPosition = on ? handlePosition * -1 : handlePosition ; // no anim
      _UIHandleRectTransform.DOAnchorPos(on ? _handlePosition * -1 : _handlePosition, .4f).SetEase(Ease.InOutBack);

      //backgroundImage.color = on ? backgroundActiveColor : backgroundDefaultColor ; // no anim
      _backgroundImage.DOColor(on ? _backgroundActiveColor : _backgroundDefaultColor, .6f);

      //handleImage.color = on ? handleActiveColor : handleDefaultColor ; // no anim
      _handleImage.DOColor(on ? _handleActiveColor : _handleDefaultColor, .4f);
   }

   void OnDestroy()
   {
      _toggle.onValueChanged.RemoveListener(OnSwitch);
   }
}
