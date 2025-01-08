using UnityEngine;
using Domain.Interfaces;

namespace Infrastructure.SceneSetup
{
    /// <summary>
    /// Handles construction of the room including walls, floor, and ceiling.
    /// </summary>
    public class RoomBuilder
    {
        private readonly IPhysicsConfig _config;

        /// <summary>
        /// Initializes a new instance of the RoomBuilder class with the specified physics configuration.
        /// </summary>
        /// <param name="config">The physics configuration to use for room dimensions.</param>
        public RoomBuilder(IPhysicsConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Builds the entire room including the floor, walls, and ceiling.
        /// </summary>
        public void BuildRoom()
        {
            BuildFloor();
            BuildWalls();
            BuildCeiling();
        }

        /// <summary>
        /// Builds the floor of the room.
        /// </summary>
        private void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.layer = LayerMask.NameToLayer("Floor");

            const float floorThickness = 0.1f;
            floor.transform.localScale = new Vector3(_config.Room.WidthMeters, floorThickness, _config.Room.LengthMeters);
            floor.transform.position = new Vector3(0f, -floorThickness / 2f, 0f);

            ConfigureFloorVisuals(floor);
            ConfigureFloorCollider(floor);
        }

        /// <summary>
        /// Configures the visuals of the floor.
        /// </summary>
        /// <param name="floor">The floor GameObject to configure.</param>
        private static void ConfigureFloorVisuals(GameObject floor)
        {
            var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.8f, 0.1f, 0.1f)
            };
            floor.GetComponent<Renderer>().material = floorMaterial;
        }

        /// <summary>
        /// Configures the collider for the floor.
        /// </summary>
        /// <param name="floor">The floor GameObject to configure.</param>
        private static void ConfigureFloorCollider(GameObject floor)
        {
            var existingCollider = floor.GetComponent<Collider>();
            if (existingCollider != null)
            {
                Object.DestroyImmediate(existingCollider);
            }
            floor.AddComponent<BoxCollider>();
        }

        /// <summary>
        /// Builds the walls of the room.
        /// </summary>
        private void BuildWalls()
        {
            const float wallThickness = 0.1f;
            float roomSize = _config.Room.SizeMeters;
            float wallHeight = _config.Room.WallHeightMeters;

            var wallConfigs = new[]
            {
                new WallConfig("BackWall", new Vector3(0f, wallHeight / 2f, -roomSize / 2f),
                              new Vector3(roomSize, wallHeight, wallThickness)),
                new WallConfig("FrontWall", new Vector3(0f, wallHeight / 2f, roomSize / 2f),
                              new Vector3(roomSize, wallHeight, wallThickness)),
                new WallConfig("LeftWall", new Vector3(-roomSize / 2f, wallHeight / 2f, 0f),
                              new Vector3(wallThickness, wallHeight, roomSize)),
                new WallConfig("RightWall", new Vector3(roomSize / 2f, wallHeight / 2f, 0f),
                              new Vector3(wallThickness, wallHeight, roomSize))
            };

            foreach (var config in wallConfigs)
            {
                BuildWall(config);
            }
        }

        /// <summary>
        /// Represents the configuration for a wall.
        /// </summary>
        private struct WallConfig
        {
            public string Name;
            public Vector3 Position;
            public Vector3 Scale;

            public WallConfig(string name, Vector3 position, Vector3 scale)
            {
                Name = name;
                Position = position;
                Scale = scale;
            }
        }

        /// <summary>
        /// Builds a single wall based on the provided configuration.
        /// </summary>
        /// <param name="config">The configuration for the wall.</param>
        private static void BuildWall(WallConfig config)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = config.Name;
            wall.layer = LayerMask.NameToLayer("Walls");
            wall.transform.position = config.Position;
            wall.transform.localScale = config.Scale;

            var wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.1f, 0.1f, 0.8f)
            };
            wall.GetComponent<Renderer>().material = wallMaterial;
        }

        /// <summary>
        /// Builds the ceiling of the room.
        /// </summary>
        private void BuildCeiling()
        {
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.layer = LayerMask.NameToLayer("Ceiling");

            float roomSize = _config.Room.SizeMeters;
            ceiling.transform.localScale = new Vector3(roomSize / 10f, 1f, roomSize / 10f);
            ceiling.transform.position = new Vector3(0f, _config.Room.WallHeightMeters, 0f);
            ceiling.transform.Rotate(180f, 0f, 0f);

            var ceilingMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = Color.white
            };
            ceiling.GetComponent<Renderer>().material = ceilingMaterial;
        }
    }
}