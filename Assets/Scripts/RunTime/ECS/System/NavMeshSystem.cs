using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 寻路
/// </summary>
public class NavMeshSystem : BaseSystem
{
     /// <summary>
     /// 
     /// </summary>
     private Dictionary<int, NavMeshSurface> _navMeshDic = new Dictionary<int, NavMeshSurface>();

     /// <summary>
     /// 当前生效的寻路面
     /// </summary>
     private NavMeshSurface _currentNavMesh;

     public override async void OnInit(object data = null)
     {
          base.OnInit(data);

          GameObject obj = await GameEntryRunTime.GetComponent<ResourceComponent>()
                    .AsyncLoadAsset<GameObject>(AssetsPathHelper.NavMeshPathHelper(CurrentWorld.SceneName));

          GameObject navMeshGo = GameObject.Instantiate(obj);

          NavMeshSurface surface = navMeshGo.GetComponent<NavMeshSurface>();

          _currentNavMesh = surface;
          
          surface.AddData();
          
          _navMeshDic.Add(surface.agentTypeID, surface);
          
          navMeshGo.gameObject.name = $"NavMesh-{surface.agentTypeID}";
     }
     
     public int GetAgentTypeId()
     {
          return _currentNavMesh.agentTypeID;
     }

     /// <summary>
     /// 是否是有效位置
     /// </summary>
     /// <param name="poition"></param>
     /// <returns></returns>
     public bool IsValidPosition(Vector3 poition)
     {
          if (NavMesh.SamplePosition(poition, out NavMeshHit hit, 0.2f,NavMesh.AllAreas))
          {
               return true;
          }

          return false;
     }
}
