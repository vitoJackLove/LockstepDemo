using Rogue;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class EntityInfoBase : MonoBehaviour
{
    /// <summary>
    /// 画布
    /// </summary>
    protected Canvas canvas = null;

    protected Camera camera = null;

    protected RectTransform rectTransform = null;

    protected CanvasGroup canvasGroup;

    protected Transform followRoot;

    public bool IsInitHandlerCalled { get; private set; } = false;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="barCamera">血条相机</param>
    /// <param name="infoCanvas">画布</param>
    public virtual void Init(Camera barCamera, Canvas infoCanvas)
    {
        if (this.IsInitHandlerCalled)
        {
            return;
        }

        this.IsInitHandlerCalled = true;

        this.camera = barCamera;

        if (this.camera == null)
        {
            this.camera = GameEntry.Camera.BattleCamera;
        }

        this.canvas = infoCanvas;

        if (!TryGetComponent(out rectTransform))
        {
            Debug.LogError("RectTransform is invalid.");
            return;
        }

        if (!TryGetComponent(out canvasGroup))
        {
            Debug.LogError("CanvasGroup is invalid.");
            return;
        }
    }

    public virtual void BindFollowRoot(Transform follow)
    {
        this.followRoot = follow;

        transform.SetAsLastSibling();
    }

    public void ShowUI()
    {
        this.RefreshPosition();

        this.gameObject.SetActive(true);
    }

    protected virtual void LateUpdate()
    {
        this.RefreshPosition();
    }

    public void ExternalCallRefreshPosition()
    {
        RefreshPosition();
    }

    protected virtual void RefreshPosition()
    {
        if (this.followRoot == null)
            return;

        Vector3 screenPosition = this.camera.WorldToScreenPoint(this.followRoot.position);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screenPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var position))
        {
            rectTransform.localPosition = position;
            // rectTransform.anchoredPosition3D = position;
        }
    }

    public virtual void Remove()
    {
        Recovery();
    }

    /// <summary>
    /// 回收
    /// </summary>
    public virtual void Recovery()
    {
        this.followRoot = null;
        this.canvasGroup.alpha = 1;
    }
}