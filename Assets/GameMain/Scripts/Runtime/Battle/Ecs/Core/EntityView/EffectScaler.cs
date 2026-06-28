using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 全屏背景图片等比例拉伸自适应      
/// </summary>
public class EffectScaler : MonoBehaviour
{
    public ScreenEffectAdaptationType screenEffectAdaptationType;

    public Vector2 screenSize = new Vector2(2400, 1080);

    private bool _isCorrection;
    
    /// <summary>
    /// 矫正
    /// </summary>
    [Button]
    public void Correction()
    {
        if (_isCorrection)
        {
            return;
        }
        
        float scalerX  = 0;
        
        switch (screenEffectAdaptationType)
        {
            case ScreenEffectAdaptationType.FullWindow:

                //10 = Camera.size * 2
                scalerX = (Screen.width * 1f) / (Screen.height * 1f) * 10;
                
                break;
            
            case ScreenEffectAdaptationType.LocalWindow :

                var tempValue = transform.localScale.x / ((screenSize.x * 1f) / (screenSize.y * 1f) * 10);

                scalerX = tempValue * ((Screen.width * 1f) / (Screen.height * 1f) * 10);

                var positionTempValue = transform.localPosition.x / ((screenSize.x * 1f) / (screenSize.y * 1f) * 10);

                var position = positionTempValue * ((Screen.width * 1f) / (Screen.height * 1f) * 10);
                
                transform.localPosition = new Vector3(position, transform.localPosition.y, 1);
                
                break;
        }
        
        transform.localScale = new Vector3(scalerX, transform.localScale.y, 1);

        _isCorrection = true;
    }
}

public enum ScreenEffectAdaptationType
{
    FullWindow,
    LocalWindow,
}