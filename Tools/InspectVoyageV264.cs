using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
public static class InspectVoyageV264 {
 public static object Run(){var r=UnityEngine.Object.FindFirstObjectByType<OpeningTailReview>();var frames=(System.Collections.Generic.List<object>)typeof(OpeningTailReview).GetField("recorded",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(r);return frames.Where((x,i)=>i%30==0&&i<240).ToArray();}
}
