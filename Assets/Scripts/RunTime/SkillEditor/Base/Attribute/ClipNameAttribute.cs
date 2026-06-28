/// <summary>
/// Clip名字
/// </summary>
public class ClipNameAttribute :  System.Attribute
{
     public string ClipName;

     public ClipNameAttribute(string name)
     {
          this.ClipName = name;
     }
}
