using Cysharp.Threading.Tasks;
using Loxodon.Framework.ObjectPool;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class UITextDamage : EntityInfoBase
{
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private TextMeshProUGUI _label2;

    [LabelText("生命周期")] [SerializeField] private float lifeTime;

    [LabelText("坐标偏移X最小值")] public int OffsetXMin = -100;
    [LabelText("坐标偏移X最大值")] public int OffsetXMax = 100;
    [LabelText("坐标偏移Y最小值")] public int OffsetYMin = 60;
    [LabelText("坐标偏移Y最大值")] public int OffsetYMax = 180;
    public Vector2 OffSetVector2 { get; set; }
    private bool _firstTime;
    private Vector3 _initialPosition;

    private Vector3 _offset;

    private float _time;

    private async void Start()
    {
        /*await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        gameObject.SetActive(false);*/
    }

    public override void Init(Camera barCamera, Canvas infoCanvas)
    {
        base.Init(barCamera, infoCanvas);

        // this._viewModel = ReferencePool.Acquire<DamageBarData>();

        // BindingSet<UITextDamage, DamageBarData> bindingSet = this.CreateBindingSet(this._viewModel);

        // bindingSet.Bind(_label).For(v => v.text).To(vm => vm.Content);

        // bindingSet.Bind(_label2).For(v => v.text).To(vm => vm.Content);
        _firstTime = true;
        // bindingSet.Build();
    }

    private void updateDamageData(string text)
    {
        // _viewModel.UpdateData(text);
        _label.SetText(text);
        _label2.SetText(text);
    }
    /// <summary>
    /// 显示伤害文本
    /// </summary>
    /// <param name="text"></param>
    /// <param name="isMonster"></param>
    public void UpdateData(string text, bool isMonster = false)
    {
        _time = 0;
        updateDamageData(text);
        int deviate1 = 0;
        int deviate2 = 0;

        if (isMonster)
        {
            deviate1 = Random.Range(OffsetXMin, OffsetXMax);
            deviate2 = Random.Range(OffsetYMin, OffsetYMax);
        }

        OffSetVector2 = new Vector2(deviate1, deviate2);
        _offset = Vector3.zero;
        gameObject.SetActive(true);
        RefreshPosition();
    }

    /// <summary>
    /// 显示伤害文本
    /// </summary>
    /// <param name="text"></param>
    /// <param name="offset"></param>
    /// <param name="isMonster"></param>
    public void UpdateData(string text, Vector3 offset, int maxSiteLevel, bool isMonster = false)
    {
        _time = 0;
        updateDamageData(text);
        int deviate1 = 0;
        int deviate2 = 0;

        if (isMonster)
        {
            deviate1 = Random.Range(OffsetXMin, OffsetXMax);
            deviate2 = Random.Range(OffsetYMin, OffsetYMax);
        }

        OffSetVector2 = new Vector2(deviate1, deviate2);
        _offset = offset;
        gameObject.SetActive(true);
        RefreshPosition();
    }

    /// <summary>
    /// 动画回调
    /// </summary>
    private void ClearData()
    {
        gameObject.SetActive(false);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        _time += Time.deltaTime;

        if (_time >= lifeTime)
        {
            _time = -999;
            ClearData();
        }
    }

    protected override void RefreshPosition()
    {
        if (followRoot == null)
        {
            return;
        }

        Vector3 uiWorldPos = followRoot.position + _offset;
        if (_firstTime)
        {
            _initialPosition = uiWorldPos;
        }

        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            uiWorldPos = _initialPosition;

            transform.position = uiWorldPos + Vector3.up * 0;

            transform.localRotation *= Quaternion.Euler(0, 180, 0);
        }
        else
        {
            uiWorldPos = _initialPosition;
            if (camera != null)
            {
                Vector2 screenPoint;
                screenPoint = camera.WorldToScreenPoint(uiWorldPos);
                Vector2 output;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent as RectTransform,
                    screenPoint + Vector2.up * 0,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out output);

                output += OffSetVector2;
                rectTransform.anchoredPosition3D = output;
            }
            else
            {
                rectTransform.anchoredPosition3D =
                    (canvas.transform as RectTransform).InverseTransformPoint(uiWorldPos) + Vector3.up * 0;
            }
        }

        transform.SetAsLastSibling();

        _firstTime = false;
    }
}