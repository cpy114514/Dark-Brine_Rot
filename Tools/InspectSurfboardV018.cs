using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
public static class InspectSurfboardV018
{
    public static object Inspect()
    {
        var hero=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var rider=hero.GetComponent<Story1WreckRider>();var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
        Vector3 feet=hero.transform.position+Vector3.up*hero.LowestFootWorldOffset;
        var board=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None).OrderBy(p=>(p.transform.position-feet).sqrMagnitude).First();
        var at=board.BoardingPoint(feet);var cc=hero.GetComponent<CharacterController>();
        return new{hero=hero.transform.position.ToString(),feet=feet.ToString(),hero.Swimming,hero.CanUseGroundAttack,hero.VerticalSpeed,
            hero.OnFloatingWreckDeck,hero.ExternalMovementLock,hero.ExternalControlLock,state=hero.Motion.State.ToString(),owner=hero.Motion.Owner?.GetType().Name,
            animator=hero.CharacterAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash,rider.IsSurfing,rider.IsClimbing,support=rider.Support()?.name,
            board=board.name,board.IsBoardable,board.IsSurfboard,dimensions=board.Dimensions.ToString(),centre=board.transform.position.ToString(),velocity=board.Velocity.ToString(),
            point=at.ToString(),water=water.SampleSurfaceHeight(at,Time.time),feetGap=at.y-feet.y,contains=board.ContainsFootprint(feet),supports=board.Supports(feet),cc.isGrounded};
    }
}
