using Alg;
using UnityEngine;
using UnityEngine.UI;

public class DragControl : Singleton<DragControl>
{

    // todo:
    public enum State
    {
        Hold, // when user hold the touch screen and wait for character touch the charging zone (can see only first touch circle) 
        Processing // 
    }
    public Image StartDragPoint;
    public Image EndDragPoint;
    public float MaxDistance;
    public GameObject Segments;

    private RectTransform _myRectTransform;
    private RectTransform _startDragTransform;
    private RectTransform _endDragTransform;

    private GameObject[] _segmentsLerp;

   
    void Update()
    {
        if (InputHandler.Instance.IsTouching)
        {
            Segments.SetActive(true);

            Vector2 point1 = GetStartTouchPoint();
            _startDragTransform.anchoredPosition = point1;

            Vector2 point2 = GetEndTouchPoint();

            var delta = point1 - point2;
            delta = Vector2.ClampMagnitude(delta, MaxDistance);

            _endDragTransform.anchoredPosition = point1 - delta;

            for (int i = 0; i < _segmentsLerp.Length; ++i)
            {
                var t = Mathf.Clamp01(i / (float)_segmentsLerp.Length);
                (_segmentsLerp[i].transform as RectTransform).anchoredPosition = Vector2.Lerp(point1, point1 - delta, t);
                (_segmentsLerp[i].transform as RectTransform).localScale = Vector3.one * (t + 0.65f);
            }
        }
        else
        {
            Segments.SetActive(false);
        }
    }


    public void CreateSegments()
    {
        if (_segmentsLerp?.Length > 0 )
            return;
        _myRectTransform = transform.parent.GetComponent<RectTransform>();
        _startDragTransform = StartDragPoint.transform as RectTransform;
        _endDragTransform = EndDragPoint.transform as RectTransform;

        _segmentsLerp = new GameObject[10];
        for (int i = 0; i < _segmentsLerp.Length; ++i)
            _segmentsLerp[i] = Instantiate(StartDragPoint.gameObject, StartDragPoint.transform.parent, true);
        Segments.SetActive(false);
    }

    public Vector2 GetStartTouchPoint()
    {
        Vector2 point;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _myRectTransform, 
            InputHandler.Instance.FirstTouchPoint, null, out point);
        return point;
    }

    public Vector2 GetEndTouchPoint()
    {
        Vector2 point;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _myRectTransform,
            InputHandler.Instance.LastTouchPoint, null, out point);
        return point;
    }
}
