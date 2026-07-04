using System;
using System.Collections.Generic;

public static class GameEntryRunTime
{
     public static readonly Dictionary<Type, RunTimeComponent> RunTimeComponents =
          new Dictionary<Type, RunTimeComponent>();

     public static void Register(RunTimeComponent runTimeComponent)
     {
          RunTimeComponents.Add(runTimeComponent.GetType(), runTimeComponent);
     }

     public static T GetComponent<T>() where T : RunTimeComponent
     {
          RunTimeComponents.TryGetValue(typeof(T), out var component);
          
          return (T)component;
     }

     public static void Shutdown()
     {
          foreach (var component in RunTimeComponents.Values)
          {
               component.Shutdown();    
          }
          
          RunTimeComponents.Clear();
     }
}
