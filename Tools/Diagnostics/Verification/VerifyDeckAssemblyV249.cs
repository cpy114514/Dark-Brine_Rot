using System;
using System.Reflection;
using UnityEngine;
public static class VerifyDeckAssemblyV249
{
 public static object Run()
 {
  var method=typeof(Story1WreckDeckClearance).GetMethod("BoundsOf",BindingFlags.Static|BindingFlags.NonPublic);
  var il=method.GetMethodBody().GetILAsByteArray();bool normalized=false;
  for(int i=0;i<il.Length-4;i++)
  {
   if(il[i]!=0x28&&il[i]!=0x6f)continue;
   try{var call=method.Module.ResolveMethod(BitConverter.ToInt32(il,i+1));if(call.DeclaringType==typeof(Quaternion)&&call.Name=="get_normalized")normalized=true;}
   catch(ArgumentException){}
  }
  if(!normalized)throw new Exception("Loaded deck-query assembly still lacks quaternion normalization; wait for compilation.");
  return new{normalized,tracking=typeof(Story1WreckDeckClearance).GetMethod("Track")!=null,playing=Application.isPlaying};
 }
}
