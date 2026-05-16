using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Common.Physics
{
    public class PhysicsService : IPhysicsService
    {
        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[128];
        private static readonly Collider2D[] OverlapHits = new Collider2D[128];

        public RaycastHit2D Raycast(Vector2 worldPosition, Vector2 direction, int layerMask)
        {
            int hitCount = Physics2D.RaycastNonAlloc(worldPosition, direction, Hits, Mathf.Infinity, layerMask);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = Hits[i];
                if (hit.collider != null)
                    return hit;
            }

            return default;
        }

        public RaycastHit2D LineCast(Vector2 start, Vector2 end, int layerMask)
        {
            int hitCount = Physics2D.LinecastNonAlloc(start, end, Hits, layerMask);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = Hits[i];
                if (hit.collider != null)
                    return hit;
            }

            return default;
        }

        public Collider2D OverlapPoint(Vector2 worldPosition, int layerMask)
        {
            int hitCount = Physics2D.OverlapPointNonAlloc(worldPosition, OverlapHits, layerMask);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = OverlapHits[i];
                if (hit != null)
                    return hit;
            }

            return null;
        }

        public IEnumerable<Collider2D> CircleCast(Vector3 position, float radius, int layerMask)
        {
            int hitCount = OverlapCircle(position, radius, OverlapHits, layerMask);

            DrawDebug(position, radius, 1f, Color.red);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = OverlapHits[i];
                if (hit != null)
                    yield return hit;
            }
        }

        public int CircleCastNonAlloc(Vector3 position, float radius, int layerMask, Collider2D[] hitBuffer)
        {
            int hitCount = OverlapCircle(position, radius, OverlapHits, layerMask);

            DrawDebug(position, radius, 1f, Color.green);

            for (int i = 0; i < hitCount && i < hitBuffer.Length; i++)
                hitBuffer[i] = OverlapHits[i];

            return hitCount;
        }

        public int OverlapCircle(Vector3 worldPosition, float radius, Collider2D[] hits, int layerMask) =>
            Physics2D.OverlapCircleNonAlloc(worldPosition, radius, hits, layerMask);

        private static void DrawDebug(Vector2 worldPosition, float radius, float seconds, Color color)
        {
            Debug.DrawRay(worldPosition, radius * Vector3.up, color, seconds);
            Debug.DrawRay(worldPosition, radius * Vector3.down, color, seconds);
            Debug.DrawRay(worldPosition, radius * Vector3.left, color, seconds);
            Debug.DrawRay(worldPosition, radius * Vector3.right, color, seconds);
        }
    }
}
