using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 相机系统
/// </summary>
public class CameraSystem : BaseSystem
{
     /// <summary>
     /// 战斗相机
     /// </summary>
     private Camera _battleCamera;

     /// <summary>
     /// 虚拟相机中心控制
     /// </summary>
     private CinemachineBrain _cinemachineBrain;

     /// <summary>
     /// 当前生效的相机效果
     /// </summary>
     private CinemachineVirtualCamera _cameraEffect;

     /// <summary>
     /// 主角实体
     /// </summary>
     private BaseEntity _actorEntity;
     
     public override void OnInit(object data = null)
     {
          base.OnInit(data);

          _battleCamera = GameEntryRunTime.GetComponent<SceneComponent>().battleCamera;

          _cinemachineBrain = _battleCamera.GetOrAddComponent<CinemachineBrain>();

          CreateCameraEffect(10, 40, new Vector3(45, 0, 0), 
               null, new Vector3(0, 1.5f, 0));
     }

     public void RegisterActor(BaseEntity baseEntity)
     {
          _actorEntity = baseEntity;
          _cameraEffect.Follow = baseEntity.GetComponent<TransformComponent>().UnityTransform;
     }
  
     /// <summary>
     /// 添加一个相机效果
     /// </summary>
     /// <param name="priority"></param>
     /// <param name="fieldOfView"></param>
     /// <param name="rotate"></param>
     /// <param name="followTransform"></param>
     /// <param name="offsetPosition"></param>
     private void CreateCameraEffect(int priority, float fieldOfView, Vector3 rotate, Transform followTransform, Vector3 offsetPosition)
     {
          GameObject cameraEffectGo = new GameObject("CameraEffect");
          cameraEffectGo.transform.SetParent(CurrentWorld.WorldRoot);
          cameraEffectGo.transform.eulerAngles = rotate;

          _cameraEffect =  cameraEffectGo.GetOrAddComponent<CinemachineVirtualCamera>();
          //跟随目标
          _cameraEffect.Follow = followTransform;
          _cameraEffect.Priority = priority;
          //视距
          _cameraEffect.m_Lens.FieldOfView = fieldOfView;
          _cameraEffect.m_Lens.FarClipPlane = 500;
          
          //具体效果
          var transposer = _cameraEffect.AddCinemachineComponent<CinemachineFramingTransposer>();
          transposer.m_TrackedObjectOffset = offsetPosition;
          transposer.m_UnlimitedSoftZone = true;
          transposer.m_CameraDistance = 13;
          transposer.m_XDamping = 0;
          transposer.m_YDamping =0;
          transposer.m_ZDamping = 0;
     }

     public Camera BattleCamera => _battleCamera;
}
