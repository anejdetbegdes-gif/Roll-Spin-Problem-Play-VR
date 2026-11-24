using System.Collections;
using UnityEngine;

namespace RSPP
{
    /// <summary>
    /// The player's game piece. It is moved automatically by code along the board path;
    /// the player never has to grab it.
    /// </summary>
    public class PlayerToken : MonoBehaviour
    {
        public const float HopTime = 0.28f;
        public const float HopHeight = 0.05f;

        private BoardPath board;

        public static PlayerToken Create(BoardPath boardPath)
        {
            var go = new GameObject("Player Token");
            go.transform.SetParent(boardPath.transform, false);

            var color = new Color(0.90f, 0.15f, 0.45f);
            Shapes.Cylinder("Token Base", go.transform, new Vector3(0f, 0.006f, 0f), 0.05f, 0.012f, color);
            Shapes.Cylinder("Token Body", go.transform, new Vector3(0f, 0.035f, 0f), 0.028f, 0.05f, color);
            Shapes.Sphere("Token Head", go.transform, new Vector3(0f, 0.072f, 0f), 0.04f, color);

            var token = go.AddComponent<PlayerToken>();
            token.board = boardPath;
            token.PlaceOn(0);
            return token;
        }

        /// <summary>Puts the token on a space immediately (used for start and restart).</summary>
        public void PlaceOn(int spaceIndex)
        {
            transform.localPosition = board.TokenLocalPosition(spaceIndex);
        }

        /// <summary>Hops the token one space at a time from <paramref name="fromIndex"/> to <paramref name="toIndex"/>.</summary>
        public IEnumerator MoveAlong(int fromIndex, int toIndex)
        {
            for (int i = fromIndex + 1; i <= toIndex; i++)
            {
                Vector3 start = board.TokenLocalPosition(i - 1);
                Vector3 end = board.TokenLocalPosition(i);

                float t = 0f;
                while (t < 1f)
                {
                    t = Mathf.Min(1f, t + Time.deltaTime / HopTime);
                    Vector3 p = Vector3.Lerp(start, end, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * HopHeight;
                    transform.localPosition = p;
                    yield return null;
                }
                yield return new WaitForSeconds(0.06f);
            }
            PlaceOn(toIndex);
        }
    }
}
