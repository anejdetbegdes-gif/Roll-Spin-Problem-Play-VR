using UnityEngine;
using UnityEngine.Rendering;

namespace RSPP
{
    /// <summary>
    /// Builds the small, bright classroom and the table. Everything is made from a handful of
    /// simple boxes so the scene stays far below the Quest triangle and draw-call budgets.
    /// </summary>
    public static class EnvironmentBuilder
    {
        /// <summary>
        /// Creates the room, the light and the table.
        /// Returns the "Play Area" transform: its origin is on the TABLE TOP directly at the player,
        /// and every interactive object is placed relative to it (see GameConfig).
        /// </summary>
        /// <summary>The classroom (walls, decoration). Hidden while the entrance screen is shown.</summary>
        public static Transform Room { get; private set; }

        public static Transform Build()
        {
            BuildLighting();
            BuildRoom();

            var playArea = new GameObject("Play Area");
            playArea.transform.position = new Vector3(0f, GameConfig.DesktopEyeHeight - GameConfig.EyeHeightAboveTable, 0f);
            BuildTable(playArea.transform);
            return playArea.transform;
        }

        private static void BuildLighting()
        {
            // One directional light without shadows plus flat ambient light: bright, even and cheap.
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.9f;
            light.color = new Color(1f, 0.98f, 0.93f);
            light.shadows = LightShadows.None;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.63f, 0.66f);
            RenderSettings.fog = false;
        }

        private static void BuildRoom()
        {
            var room = new GameObject("Classroom").transform;
            Room = room;

            // The room is 6 m x 6 m x 3 m. The player is at x = 0, z = 0, facing the front wall (+z).
            const float size = 6f;
            const float height = 3f;
            const float centerZ = 1f;
            const float wall = 0.1f;

            Color floorColor = new Color(0.80f, 0.66f, 0.48f);
            Color frontColor = new Color(0.72f, 0.87f, 0.95f);
            Color sideColor = new Color(0.99f, 0.95f, 0.82f);
            Color ceilingColor = new Color(0.97f, 0.97f, 0.97f);

            Shapes.Box("Floor", room, new Vector3(0f, -wall * 0.5f, centerZ), new Vector3(size, wall, size), floorColor);
            Shapes.Box("Ceiling", room, new Vector3(0f, height + wall * 0.5f, centerZ), new Vector3(size, wall, size), ceilingColor);
            Shapes.Box("Front Wall", room, new Vector3(0f, height * 0.5f, centerZ + size * 0.5f), new Vector3(size, height, wall), frontColor);
            Shapes.Box("Back Wall", room, new Vector3(0f, height * 0.5f, centerZ - size * 0.5f), new Vector3(size, height, wall), frontColor);
            Shapes.Box("Left Wall", room, new Vector3(-size * 0.5f, height * 0.5f, centerZ), new Vector3(wall, height, size), sideColor);
            Shapes.Box("Right Wall", room, new Vector3(size * 0.5f, height * 0.5f, centerZ), new Vector3(wall, height, size), sideColor);

            // A friendly rug under the table.
            Shapes.Box("Rug", room, new Vector3(0f, 0.005f, 0.6f), new Vector3(2.6f, 0.01f, 1.8f), new Color(0.36f, 0.62f, 0.80f));

            // Chalkboard on the front wall with the game title.
            float boardZ = centerZ + size * 0.5f - wall * 0.5f;
            Shapes.Box("Chalkboard Frame", room, new Vector3(0f, 1.75f, boardZ - 0.02f), new Vector3(3.0f, 1.3f, 0.04f), new Color(0.50f, 0.34f, 0.20f));
            Shapes.Box("Chalkboard", room, new Vector3(0f, 1.75f, boardZ - 0.045f), new Vector3(2.8f, 1.1f, 0.02f), new Color(0.13f, 0.33f, 0.25f));

            Canvas canvas = UIFactory.WorldCanvas("Chalkboard Text", room, new Vector3(0f, 1.75f, boardZ - 0.06f),
                Quaternion.identity, 2800f, 1100f, null, false);
            UIFactory.Label("Title", canvas.transform, "Roll, Spin, Problem, Play", 100f, 200f, 2600f, 300f, 190, new Color(1f, 1f, 0.92f));
            UIFactory.Localize(UIFactory.Label("Subtitle", canvas.transform, "", 100f, 520f, 2600f, 200f, 110, new Color(1f, 0.85f, 0.35f)), "chalk_subtitle");
            UIFactory.Label("Chalk Sum", canvas.transform, "3 + 4 = 7", 100f, 760f, 2600f, 180f, 120, new Color(0.92f, 0.95f, 0.92f));
            Shapes.Box("Chalk Tray", room, new Vector3(0f, 1.18f, boardZ - 0.09f), new Vector3(2.8f, 0.03f, 0.08f), new Color(0.50f, 0.34f, 0.20f));
            Shapes.Box("Chalk", room, new Vector3(0.6f, 1.205f, boardZ - 0.09f), new Vector3(0.08f, 0.02f, 0.02f), Color.white);

            BuildWindow(room, -size * 0.5f + wall * 0.5f);
            // The shelves stand 3 cm in front of the wall: if the back panel touched the wall surface,
            // the two would flicker ("z-fighting") because the GPU cannot tell which one is in front.
            float shelfX = size * 0.5f - wall * 0.5f - 0.17f - 0.03f;
            BuildBookshelf(room, new Vector3(shelfX, 0f, 0.0f));
            BuildBookshelf(room, new Vector3(shelfX, 0f, 1.3f));
            BuildPosters(room, boardZ);
            BuildClock(room, new Vector3(0f, 2.65f, boardZ - 0.03f));
            BuildCeilingLights(room, height);
        }

        // ------------------------------------------------------------------ Classroom decoration
        // Simple low-poly objects made of boxes, spheres and cylinders (no imported models).
        // All materials are shared per colour with GPU instancing, which keeps draw calls low.

        /// <summary>A window on the left wall with a sunny "outside": sky, a tree and the sun.</summary>
        private static void BuildWindow(Transform room, float wallX)
        {
            var frame = new Color(0.98f, 0.98f, 0.96f);
            float x = wallX + 0.06f;           // just in front of the wall surface
            Vector3 c = new Vector3(x, 1.55f, 1.4f);

            Shapes.Box("Window Frame", room, c, new Vector3(0.03f, 1.25f, 1.65f), frame);
            Shapes.Box("Window Sky", room, c + new Vector3(0.02f, 0f, 0f), new Vector3(0.01f, 1.1f, 1.5f), new Color(0.62f, 0.84f, 0.98f));
            Shapes.Box("Window Grass", room, c + new Vector3(0.025f, -0.42f, 0f), new Vector3(0.01f, 0.26f, 1.5f), new Color(0.45f, 0.75f, 0.35f));

            GameObject crown = Shapes.Sphere("Tree Crown", room, c + new Vector3(0.03f, 0.05f, -0.3f), 0.5f, new Color(0.30f, 0.62f, 0.28f));
            crown.transform.localScale = new Vector3(0.01f, 0.5f, 0.5f);
            Shapes.Box("Tree Trunk", room, c + new Vector3(0.028f, -0.33f, -0.3f), new Vector3(0.01f, 0.32f, 0.08f), new Color(0.48f, 0.32f, 0.18f));
            GameObject sun = Shapes.Sphere("Sun", room, c + new Vector3(0.03f, 0.38f, 0.45f), 0.18f, new Color(1f, 0.86f, 0.25f));
            sun.transform.localScale = new Vector3(0.01f, 0.18f, 0.18f);

            // Window bars.
            Shapes.Box("Window Bar V", room, c + new Vector3(0.04f, 0f, 0f), new Vector3(0.02f, 1.1f, 0.04f), frame);
            Shapes.Box("Window Bar H", room, c + new Vector3(0.04f, 0.1f, 0f), new Vector3(0.02f, 0.04f, 1.5f), frame);
            Shapes.Box("Window Sill", room, c + new Vector3(0.06f, -0.64f, 0f), new Vector3(0.12f, 0.04f, 1.75f), frame);
        }

        /// <summary>A bookshelf against the right wall with colourful books, a globe and a pencil cup on top.</summary>
        private static void BuildBookshelf(Transform room, Vector3 position)
        {
            var wood = new Color(0.62f, 0.42f, 0.27f);
            var shelf = new GameObject("Bookshelf").transform;
            shelf.SetParent(room, false);
            shelf.localPosition = position;
            shelf.localRotation = Quaternion.Euler(0f, 90f, 0f);     // back (+z local) against the right wall, open side faces the room

            const float width = 1.0f, height = 1.5f, depth = 0.32f;
            Shapes.Box("Back", shelf, new Vector3(0f, height * 0.5f, depth * 0.5f), new Vector3(width, height, 0.02f), wood);
            Shapes.Box("Left Side", shelf, new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(0.03f, height, depth), wood);
            Shapes.Box("Right Side", shelf, new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(0.03f, height, depth), wood);

            Color[] bookColors = GameConfig.TileColors;
            float[] shelfHeights = { 0.03f, 0.52f, 1.01f, 1.49f };
            for (int s = 0; s < shelfHeights.Length; s++)
            {
                Shapes.Box("Shelf " + s, shelf, new Vector3(0f, shelfHeights[s], 0f), new Vector3(width, 0.03f, depth), wood);
                if (s == shelfHeights.Length - 1) break;     // nothing above the top board here

                // A row of books with slightly different sizes.
                float x = -width * 0.5f + 0.06f;
                for (int b = 0; b < 7; b++)
                {
                    float bookWidth = 0.06f + (b * 7 + s * 3) % 4 * 0.012f;
                    float bookHeight = 0.30f + (b * 5 + s) % 3 * 0.04f;
                    Shapes.Box("Book", shelf, new Vector3(x + bookWidth * 0.5f, shelfHeights[s] + 0.015f + bookHeight * 0.5f, 0.02f),
                        new Vector3(bookWidth, bookHeight, 0.22f), bookColors[(b + s * 2) % bookColors.Length]);
                    x += bookWidth + 0.01f;
                }
            }

            // On top: a globe and a pencil cup.
            float top = shelfHeights[shelfHeights.Length - 1] + 0.015f;
            Shapes.Cylinder("Globe Stand", shelf, new Vector3(-0.25f, top + 0.05f, 0f), 0.02f, 0.10f, wood);
            Shapes.Sphere("Globe", shelf, new Vector3(-0.25f, top + 0.19f, 0f), 0.2f, new Color(0.25f, 0.56f, 0.90f));
            Shapes.Sphere("Globe Land", shelf, new Vector3(-0.28f, top + 0.22f, -0.04f), 0.12f, new Color(0.36f, 0.75f, 0.38f));
            Shapes.Cylinder("Pencil Cup", shelf, new Vector3(0.25f, top + 0.06f, 0f), 0.08f, 0.12f, new Color(0.93f, 0.33f, 0.31f));
            Shapes.Cylinder("Pencil 1", shelf, new Vector3(0.23f, top + 0.15f, 0f), 0.012f, 0.16f, new Color(1f, 0.80f, 0.20f));
            Shapes.Cylinder("Pencil 2", shelf, new Vector3(0.27f, top + 0.15f, 0.01f), 0.012f, 0.16f, new Color(0.25f, 0.56f, 0.90f));
        }

        /// <summary>Two math posters next to the chalkboard: a 0-20 number line and an odd/even chart.</summary>
        private static void BuildPosters(Transform room, float wallZ)
        {
            Color dark = new Color(0.12f, 0.16f, 0.30f);

            // Number line poster (left of the chalkboard).
            Vector3 left = new Vector3(-2.25f, 1.75f, wallZ - 0.02f);
            Shapes.Box("Number Line Poster", room, left, new Vector3(1.1f, 0.75f, 0.01f), Color.white);
            Shapes.Box("Number Line Header", room, left + new Vector3(0f, 0.30f, -0.006f), new Vector3(1.1f, 0.15f, 0.005f), new Color(0.25f, 0.56f, 0.90f));
            Canvas lineCanvas = UIFactory.WorldCanvas("Number Line Text", room, left + new Vector3(0f, 0f, -0.012f), Quaternion.identity, 1100f, 750f, null, false);
            UIFactory.Localize(UIFactory.Label("Title", lineCanvas.transform, "", 0f, 10f, 1100f, 120f, 80, Color.white), "poster_numberline");
            UIFactory.Label("Numbers 1", lineCanvas.transform, "0  1  2  3  4  5  6  7  8  9  10", 20f, 220f, 1060f, 150f, 70, dark);
            UIFactory.Label("Numbers 2", lineCanvas.transform, "11  12  13  14  15  16  17  18  19  20", 20f, 450f, 1060f, 150f, 70, dark);
            Shapes.Box("Number Line 1", room, left + new Vector3(0f, -0.02f, -0.008f), new Vector3(0.98f, 0.012f, 0.004f), dark);
            Shapes.Box("Number Line 2", room, left + new Vector3(0f, -0.25f, -0.008f), new Vector3(0.98f, 0.012f, 0.004f), dark);

            // Odd and even poster (right of the chalkboard).
            Vector3 right = new Vector3(2.25f, 1.75f, wallZ - 0.02f);
            Shapes.Box("Odd Even Poster", room, right, new Vector3(1.1f, 0.75f, 0.01f), Color.white);
            Shapes.Box("Odd Even Header", room, right + new Vector3(0f, 0.30f, -0.006f), new Vector3(1.1f, 0.15f, 0.005f), new Color(0.62f, 0.42f, 0.83f));
            Canvas oddCanvas = UIFactory.WorldCanvas("Odd Even Text", room, right + new Vector3(0f, 0f, -0.012f), Quaternion.identity, 1100f, 750f, null, false);
            UIFactory.Localize(UIFactory.Label("Title", oddCanvas.transform, "", 0f, 10f, 1100f, 120f, 80, Color.white), "poster_oddeven");
            UIFactory.Localize(UIFactory.Label("Even", oddCanvas.transform, "", 40f, 220f, 1020f, 180f, 84, new Color(0.13f, 0.55f, 0.22f)), "poster_even");
            UIFactory.Localize(UIFactory.Label("Odd", oddCanvas.transform, "", 40f, 470f, 1020f, 180f, 84, new Color(0.80f, 0.20f, 0.18f)), "poster_odd");
        }

        /// <summary>A round wall clock above the chalkboard.</summary>
        private static void BuildClock(Transform room, Vector3 position)
        {
            GameObject rim = Shapes.Cylinder("Clock Rim", room, position, 0.42f, 0.04f, new Color(0.25f, 0.56f, 0.90f));
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject face = Shapes.Cylinder("Clock Face", room, position + new Vector3(0f, 0f, -0.012f), 0.36f, 0.03f, Color.white);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var dark = new Color(0.12f, 0.12f, 0.18f);
            GameObject hour = Shapes.Box("Hour Hand", room, position + new Vector3(0.04f, 0.02f, -0.03f), new Vector3(0.10f, 0.015f, 0.005f), dark);
            hour.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            Shapes.Box("Minute Hand", room, position + new Vector3(0f, 0.065f, -0.032f), new Vector3(0.012f, 0.13f, 0.005f), dark);
        }

        /// <summary>Flat light panels on the ceiling (decoration; the real light is the directional light).</summary>
        private static void BuildCeilingLights(Transform room, float ceilingHeight)
        {
            var panel = new Color(1f, 0.99f, 0.92f);
            Shapes.Box("Ceiling Light 1", room, new Vector3(-1.2f, ceilingHeight - 0.02f, 1.5f), new Vector3(1.2f, 0.03f, 0.35f), panel);
            Shapes.Box("Ceiling Light 2", room, new Vector3(1.2f, ceilingHeight - 0.02f, 1.5f), new Vector3(1.2f, 0.03f, 0.35f), panel);
            Shapes.Box("Ceiling Light 3", room, new Vector3(-1.2f, ceilingHeight - 0.02f, -0.5f), new Vector3(1.2f, 0.03f, 0.35f), panel);
            Shapes.Box("Ceiling Light 4", room, new Vector3(1.2f, ceilingHeight - 0.02f, -0.5f), new Vector3(1.2f, 0.03f, 0.35f), panel);
        }

        private static void BuildTable(Transform playArea)
        {
            Vector3 center = GameConfig.TableCenter;
            Vector2 size = GameConfig.TableSize;
            const float topThickness = 0.04f;

            Shapes.Box("Table Top", playArea, new Vector3(center.x, -topThickness * 0.5f, center.z),
                new Vector3(size.x, topThickness, size.y), new Color(0.87f, 0.72f, 0.53f));

            // A solid pedestal that always reaches the floor, whatever height the table is set to.
            const float pedestalHeight = 1.3f;
            Shapes.Box("Table Pedestal", playArea, new Vector3(center.x, -topThickness - pedestalHeight * 0.5f, center.z + 0.08f),
                new Vector3(size.x - 0.3f, pedestalHeight, size.y - 0.3f), new Color(0.60f, 0.45f, 0.30f));
        }
    }
}
