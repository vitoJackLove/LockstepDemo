using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class TestPlayAble : MonoBehaviour
{
    public GameObject go;
    
    [ContextMenu("Test")]
    public void TestCreat()
    {
         GameObject.Instantiate(go);
    }
}
