using UnityEngine;
using UnityEngine.UI;

namespace RSPP
{
    /// <summary>
    /// The single board-game path on the table: a winding track of coloured spaces from START to FINISH.
    /// It stores the position of every space so the token can be moved by code (no sockets needed).
    /// </summary>
    public class BoardPath : MonoBehaviour
    {
        private Vector3[] spacePositions;     // centre of each space, local to this object
        private bool[] starSpaces;

        public int SpaceCount
        {
            get { return spacePositions.Length; }
        }

        /// <summary>Index of the FINISH space.</summary>
        public int LastIndex
        {
            get { return spacePositions.Length - 1; }
        }

        public bool IsStarSpace(int index)
        {
            return index >= 0 && index < starSpaces.Length && starSpaces[index];
        }

        /// <summary>Where the token stands on a space (local to the board object).</summary>
        public Vector3 TokenLocalPosition(int index)
        {
            index = Mathf.Clamp(index, 0, LastIndex);
            Vector3 p = spacePositions[index];
            p.y = GameConfig.BoardBaseHeight + GameConfig.TileHeight;
            return p;
        }

        // ------------------------------------------------------------------ Construction

        public static BoardPath Create(Transform playArea)
        {
            var go = new GameObject("Board");
            go.transform.SetParent(playArea, false);
            var board = go.AddComponent<BoardPath>();
            board.Build();
            return board;
        }

        private void Build()
        {
            int columns = GameConfig.BoardColumns;
            float spacing = GameConfig.TileSpacing;
            float halfWidth = (columns - 1) * spacing * 0.5f;
            float z0 = GameConfig.BoardFirstRowZ;
            float rowGap = GameConfig.BoardRowGap;

            // Three rows joined by two single connector spaces: a simple S-shaped path.
            //   row 3:  -> -> -> -> -> -> -> FINISH
            //   row 2:  <- <- <- <- <- <- <- <-
            //   row 1:  START -> -> -> -> -> ->
            int count = columns * 3 + 2;
            spacePositions = new Vector3[count];
            starSpaces = new bool[count];

            int index = 0;
            for (int i = 0; i < columns; i++) spacePositions[index++] = new Vector3(-halfWidth + i * spacing, 0f, z0);
            spacePositions[index++] = new Vector3(halfWidth, 0f, z0 + rowGap * 0.5f);
            for (int i = 0; i < columns; i++) spacePositions[index++] = new Vector3(halfWidth - i * spacing, 0f, z0 + rowGap);
            spacePositions[index++] = new Vector3(-halfWidth, 0f, z0 + rowGap * 1.5f);
            for (int i = 0; i < columns; i++) spacePositions[index++] = new Vector3(-halfWidth + i * spacing, 0f, z0 + rowGap * 2f);

            foreach (int star in GameConfig.StarSpaces)
            {
                if (star > 0 && star < count - 1) starSpaces[star] = true;
            }

            // Board base.
            float baseHeight = GameConfig.BoardBaseHeight;
            // A green "game mat" with a cream border, like a printed board game.
            var baseSize = new Vector3(halfWidth * 2f + GameConfig.TileSize + 0.11f, baseHeight, rowGap * 2f + GameConfig.TileSize + 0.06f);
            Shapes.Box("Board Border", transform, new Vector3(0f, baseHeight * 0.5f, z0 + rowGap),
                baseSize + new Vector3(0.03f, -0.001f, 0.03f), new Color(0.98f, 0.96f, 0.88f));
            Shapes.Box("Board Base", transform, new Vector3(0f, baseHeight * 0.5f, z0 + rowGap), baseSize, new Color(0.43f, 0.75f, 0.35f));

            // Spaces.
            float tileY = baseHeight + GameConfig.TileHeight * 0.5f;
            float topY = baseHeight + GameConfig.TileHeight;
            var tileSize = new Vector3(GameConfig.TileSize, GameConfig.TileHeight, GameConfig.TileSize);

            for (int i = 0; i < count; i++)
            {
                Color color;
                if (i == 0) color = GameConfig.StartTileColor;
                else if (i == count - 1) color = GameConfig.FinishTileColor;
                else if (starSpaces[i]) color = GameConfig.StarTileColor;
                else color = GameConfig.TileColors[i % GameConfig.TileColors.Length];

                Vector3 p = spacePositions[i];
                if (i == 0 || i == count - 1)
                {
                    // START and FINISH are longer, sticking out past the ends of their rows.
                    float outward = i == 0 ? -0.025f : 0.025f;
                    Shapes.Box(i == 0 ? "Space 0 (START)" : "Space " + i + " (FINISH)", transform,
                        new Vector3(p.x + outward, tileY + 0.002f, p.z), tileSize + new Vector3(0.05f, 0.004f, 0f), color);
                }
                else
                {
                    Shapes.Box("Space " + i, transform, new Vector3(p.x, tileY, p.z), tileSize, color);
                }

                if (starSpaces[i])
                {
                    // A gold star lying flat on the space marks a mini challenge.
                    GameObject star = Shapes.Star("Star Marker " + i, transform, new Vector3(p.x, topY + 0.0015f, p.z),
                        GameConfig.TileSize * 0.8f, GameConfig.StarColor);
                    star.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }

            // A small flag behind the FINISH space.
            Vector3 finish = spacePositions[count - 1];
            Shapes.Cylinder("Finish Flag Pole", transform, new Vector3(finish.x + 0.035f, topY + 0.06f, finish.z + 0.065f), 0.008f, 0.12f, new Color(0.35f, 0.25f, 0.2f));
            Shapes.Box("Finish Flag", transform, new Vector3(finish.x + 0.005f, topY + 0.10f, finish.z + 0.065f), new Vector3(0.06f, 0.035f, 0.004f), GameConfig.ButtonRed);

            BuildLabels(topY);
        }

        /// <summary>Flat text on the table: START, FINISH, and hints next to the die and the wheel handle.</summary>
        private void BuildLabels(float tileTopY)
        {
            Vector3 center = GameConfig.TableCenter;
            float width = GameConfig.TableSize.x / UIFactory.CanvasScale;
            float height = GameConfig.TableSize.y / UIFactory.CanvasScale;

            // Rotated to lie flat on the table, readable from the player's side.
            Canvas canvas = UIFactory.WorldCanvas("Board Labels", transform,
                new Vector3(center.x, tileTopY + 0.002f, center.z), Quaternion.Euler(90f, 0f, 0f),
                width, height, null, false);

            Vector3 start = spacePositions[0];
            Vector3 finish = spacePositions[spacePositions.Length - 1];
            AddLabel(canvas, "board_start", start.x - 0.025f, start.z, 136f, 50f, 32, GameConfig.TextDark);
            AddLabel(canvas, "board_finish", finish.x + 0.025f, finish.z, 136f, 50f, 30, Color.white);

            AddLabel(canvas, "board_roll", GameConfig.DieHome.x, GameConfig.DieHome.z - 0.075f, 160f, 44f, 36, GameConfig.TextDark);
            AddLabel(canvas, "board_spin", GameConfig.HandlePosition.x, GameConfig.HandlePosition.z - 0.075f, 160f, 44f, 36, GameConfig.TextDark);
        }

        /// <summary>Adds a label lying on the table. <paramref name="key"/> is its translation key (see Loc).</summary>
        private void AddLabel(Canvas canvas, string key, float localX, float localZ, float boxWidth, float boxHeight, int fontSize, Color color)
        {
            Vector3 center = GameConfig.TableCenter;
            float canvasWidth = GameConfig.TableSize.x / UIFactory.CanvasScale;
            float canvasHeight = GameConfig.TableSize.y / UIFactory.CanvasScale;

            // Convert table coordinates (metres) to canvas coordinates (millimetres from the top-left corner).
            float x = canvasWidth * 0.5f + (localX - center.x) / UIFactory.CanvasScale;
            float y = canvasHeight * 0.5f - (localZ - center.z) / UIFactory.CanvasScale;
            Text label = UIFactory.Label(key, canvas.transform, "", x - boxWidth * 0.5f, y - boxHeight * 0.5f, boxWidth, boxHeight, fontSize, color);
            UIFactory.Localize(label, key);
        }
    }
}
