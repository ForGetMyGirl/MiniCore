using UnityEngine;
using UnityEngine.EventSystems;


namespace MiniCore.Model
{
    public class PointerArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    public void OnPointerEnter(PointerEventData eventData)
    {
        EventCenter.Broadcast(GameEvent.OnPointerEnter, transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EventCenter.Broadcast(GameEvent.OnPointerExit, transform);
    }

    //public GameObject GetOverUI(GameObject canvas)
    //{
    //    PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
    //    pointerEventData.position = Input.mousePosition;
    //    GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
    //    List<RaycastResult> results = new List<RaycastResult>();
    //    gr.Raycast(pointerEventData, results);
    //    if (results.Count != 0)
    //    {
    //        return results[0].gameObject;
    //    }
    //    return null;
    //}

}

}
