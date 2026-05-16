using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Common.Physics
{
    public interface IPhysicsService
    {
        RaycastHit2D Raycast(Vector2 worldPosition, Vector2 direction, int layerMask);
        RaycastHit2D LineCast(Vector2 start, Vector2 end, int layerMask);
        Collider2D OverlapPoint(Vector2 worldPosition, int layerMask);
        IEnumerable<Collider2D> CircleCast(Vector3 position, float radius, int layerMask);
        int OverlapCircle(Vector3 worldPosition, float radius, Collider2D[] hits, int layerMask);
        int CircleCastNonAlloc(Vector3 position, float radius, int layerMask, Collider2D[] hitBuffer);
    }
}
