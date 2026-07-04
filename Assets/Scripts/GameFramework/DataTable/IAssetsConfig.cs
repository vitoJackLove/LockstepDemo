using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 资源表接口
/// </summary>
public interface IAssetsConfig 
{
     Type GetDataTableType();

     EntityAssetsConfig GetDataTable(int id);

     List<EntityAssetsConfig> GetAllDataTable();
}
