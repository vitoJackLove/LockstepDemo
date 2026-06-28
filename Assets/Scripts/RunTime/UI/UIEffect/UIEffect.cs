using System.Collections;
using UnityEngine;

public class UIEffect : MonoBehaviour
{
    [Range(0f, 1f)]
    public float from = 1f;
    [Range(0f, 1f)]
    public float to = 1f;

    public float duration = 2f;

    public float delay;

    private CanvasGroup _view;
    
    public void OnEnable()
    {
        _view = GetComponent<CanvasGroup>();
        this.StartCoroutine(DoPlay());
    }

    IEnumerator DoPlay()
    {
        yield return delay;
        
        var delta = (to - from) / duration;
        var alpha = from;
        this._view.alpha = alpha;
        if (delta > 0f)
        {
            while (alpha < to)
            {
                alpha += delta * Time.deltaTime;
                if (alpha > to)
                {
                    alpha = to;
                }
                this._view.alpha = alpha;
                yield return null;
            }
        }
        else
        {
            while (alpha > to)
            {
                alpha += delta * Time.deltaTime;
                if (alpha < to)
                {
                    alpha = to;
                }
                this._view.alpha = alpha;
                yield return null;
            }
        }
    }
}
