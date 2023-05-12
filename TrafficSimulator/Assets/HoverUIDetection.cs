using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using User;

public class HoverUIDetection : MonoBehaviour
{
    private PointerEventData _pointerEventData;
    private EventSystem _eventSystem;

    void Start()
    {
        _eventSystem = EventSystem.current;
        _pointerEventData = new PointerEventData(_eventSystem);
    }

    void Update()
    {
        _pointerEventData.position = Mouse.current.position.ReadValue();
        var results = new List<RaycastResult>();
        _eventSystem.RaycastAll(_pointerEventData, results);

        bool isHoveringUIGO = results.Exists(result => LayerMask.LayerToName(result.gameObject.layer) == "UI");
        UserSelectManager.Instance.IsHoveringUIElement = isHoveringUIGO;

    }
}