using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Coordinates the initialization and setup of the table tennis scene.
/// Delegates specific setup tasks to specialized builder classes.
/// </summary>
public class TableTennisSceneSetup : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject paddle;

    private RoomBuilder _roomBuilder;
    private TableTennisEquipmentBuilder _equipmentBuilder;
    private PlayerSetupBuilder _playerSetupBuilder;
    private SystemInitializer _systemInitializer;

    private void Start()
    {
        Debug.Log("Starting scene setup");
        InitializeBuilders();
        SetupScene();
    }

    private void InitializeBuilders()
    {
        _roomBuilder = new RoomBuilder();
        _equipmentBuilder = new TableTennisEquipmentBuilder();
        _playerSetupBuilder = new PlayerSetupBuilder(player, paddle);
        _systemInitializer = new SystemInitializer(gameObject);
    }

    private void SetupScene()
    {
        try
        {
            _roomBuilder.BuildRoom();
            _equipmentBuilder.BuildEquipment();
            StartCoroutine(_playerSetupBuilder.SetupPlayer());
            _systemInitializer.InitializeSystems();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to setup scene: {e.Message}");
            throw;
        }
    }
}

/// <summary>
/// Handles construction of the room including walls, floor, and ceiling.
/// </summary>
public class RoomBuilder
{
    public void BuildRoom()
    {
        Debug.Log("Building room");
        BuildFloor();
        BuildWalls();
        BuildCeiling();
    }

    private void BuildFloor()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.layer = LayerMask.NameToLayer("Floor");
        
        const float roomSize = TableTennisPhysicsConfig.RoomSizeMeters;
        const float floorThickness = 0.1f;
        floor.transform.localScale = new Vector3(roomSize, floorThickness, roomSize);
        floor.transform.position = new Vector3(0f, -floorThickness / 2f, 0f);

        ConfigureFloorVisuals(floor);
        ConfigureFloorCollider(floor);
    }

    private static void ConfigureFloorVisuals(GameObject floor)
    {
        var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(0.8f, 0.1f, 0.1f)
        };
        floor.GetComponent<Renderer>().material = floorMaterial;
    }

    private static void ConfigureFloorCollider(GameObject floor)
    {
        var existingCollider = floor.GetComponent<Collider>();
        if (existingCollider != null)
        {
            Object.DestroyImmediate(existingCollider);
        }
        floor.AddComponent<BoxCollider>();
    }

    private void BuildWalls()
    {
        const float wallThickness = 0.1f;
        const float roomSize = TableTennisPhysicsConfig.RoomSizeMeters;
        const float wallHeight = TableTennisPhysicsConfig.WallHeightMeters;

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

    private static void BuildCeiling()
    {
        var ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ceiling.name = "Ceiling";
        ceiling.layer = LayerMask.NameToLayer("Ceiling");
        
        const float roomSize = TableTennisPhysicsConfig.RoomSizeMeters;
        ceiling.transform.localScale = new Vector3(roomSize / 10f, 1f, roomSize / 10f);
        ceiling.transform.position = new Vector3(0f, TableTennisPhysicsConfig.WallHeightMeters, 0f);
        ceiling.transform.Rotate(180f, 0f, 0f);

        var ceilingMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = Color.white
        };
        ceiling.GetComponent<Renderer>().material = ceilingMaterial;
    }
}

/// <summary>
/// Handles setup of table tennis specific equipment (table, net).
/// </summary>
public class TableTennisEquipmentBuilder
{
    private GameObject _table;
    private GameObject _net;

    public void BuildEquipment()
    {
        Debug.Log("Building table tennis equipment");
        BuildTableTop();
        BuildNet();
    }

    private void BuildTableTop()
    {
        Debug.Log("Building table top");
        _table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _table.name = "TableTop";
        _table.layer = LayerMask.NameToLayer("Table");

        ConfigureTableDimensions();
        ConfigureTablePhysics();
    }

    private void ConfigureTableDimensions()
    {
        var tableWidth = TableTennisPhysicsConfig.TableWidthMeters;
        var tableLength = TableTennisPhysicsConfig.TableLengthMeters;

        _table.transform.localScale = new Vector3(
            tableWidth,
            TableTennisPhysicsConfig.TableThicknessMeters,
            tableLength);

        _table.transform.position = new Vector3(
            0f,
            TableTennisPhysicsConfig.TableHeightMeters - TableTennisPhysicsConfig.TableThicknessMeters / 2f,
            0f);

        Debug.Log($"Table dimensions: {_table.transform.localScale}");
    }

    private void ConfigureTablePhysics()
    {
        var tableMaterial = TableTennisPhysicsConfig.instance.tableMaterial;
        Debug.Assert(tableMaterial != null, "Table material not set in TableTennisPhysicsConfig");

        var tableCollider = _table.GetComponent<Collider>();
        tableCollider.material = tableMaterial;
        Debug.Log($"Table material: {tableMaterial.name}");
    }

    private void BuildNet()
    {
        Debug.Log("Building net");
        _net = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _net.name = "TableTennisNet";
        _net.layer = LayerMask.NameToLayer("Net");

        ConfigureNetDimensions();
        ConfigureNetPhysics();
        ConfigureNetVisuals();
    }

    private void ConfigureNetDimensions()
    {
        const float netWidth = TableTennisPhysicsConfig.TableWidthMeters + TableTennisPhysicsConfig.NetHeightMeters;
        const float netHeight = TableTennisPhysicsConfig.NetHeightMeters;
        const float netThickness = 0.001f;

        _net.transform.localScale = new Vector3(netWidth, netHeight, netThickness);

        const float surfaceOffset = 0.001f;
        float netYPosition = TableTennisPhysicsConfig.TableHeightMeters
                           + TableTennisPhysicsConfig.NetHeightMeters / 2f
                           + surfaceOffset;
        _net.transform.position = new Vector3(0f, netYPosition, 0f);
    }

    private void ConfigureNetPhysics()
    {
        var boxCollider = _net.GetComponent<BoxCollider>();
        boxCollider.material = TableTennisPhysicsConfig.instance.netMaterial;
        boxCollider.isTrigger = false;

        var rb = _net.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void ConfigureNetVisuals()
    {
        var netRenderer = _net.GetComponent<MeshRenderer>();
        if (netRenderer != null)
        {
            netRenderer.material.color = Color.white;
        }
    }
}

/// <summary>
/// Handles player and VR setup including paddle configuration.
/// </summary>
public class PlayerSetupBuilder
{
    private readonly GameObject _player;
    private readonly GameObject _paddle;
    private XROrigin _xrOrigin;

    public PlayerSetupBuilder(GameObject player, GameObject paddle)
    {
        _player = player;
        _paddle = paddle;
    }

    public IEnumerator SetupPlayer()
    {
        Debug.Log("Setting up player");
        _xrOrigin = _player.GetComponent<XROrigin>();

        yield return new WaitForSeconds(0.1f); // Wait for XR to initialize
        PositionPlayer();
        //ScalePaddleToRegulationSize();
        PositionBall();
    }

    private void PositionPlayer()
    {
        float tableHalfLength = TableTennisPhysicsConfig.TableLengthMeters / 2f;
        float zOffset = tableHalfLength + TableTennisPhysicsConfig.PlayerDistanceFromTable;

        _xrOrigin.transform.position = new Vector3(0f, 0f, zOffset);
        _xrOrigin.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Debug.Log($"Player positioned at z-offset: {zOffset}m from center");
    }

    private void ScalePaddleToRegulationSize()
    {
        var paddleMeshFilter = _paddle.GetComponentInChildren<MeshFilter>();
        if (paddleMeshFilter == null)
        {
            Debug.LogError("No MeshFilter found on paddle!");
            return;
        }

        // Get the current mesh bounds in local space
        var meshBounds = paddleMeshFilter.sharedMesh.bounds;
        var currentScale = _paddle.transform.localScale;
        var meshSize = Vector3.Scale(meshBounds.size, currentScale);

        // Calculate separate scale factors for each dimension
        float lengthScale = TableTennisPhysicsConfig.PaddleLengthMeters / meshSize.z;
        float widthScale = TableTennisPhysicsConfig.PaddleWidthMeters / meshSize.x;
        float thicknessScale = TableTennisPhysicsConfig.PaddleThicknessMeters / meshSize.y;

        // Apply non-uniform scaling to maintain correct proportions
        _paddle.transform.localScale = new Vector3(
            currentScale.x * widthScale,
            currentScale.y * thicknessScale,
            currentScale.z * lengthScale
        );

        Debug.Log($"Paddle scaled to dimensions - Length: {TableTennisPhysicsConfig.PaddleLengthMeters}m, " +
                  $"Width: {TableTennisPhysicsConfig.PaddleWidthMeters}m, " +
                  $"Thickness: {TableTennisPhysicsConfig.PaddleThicknessMeters}m");
    }

    private void PositionBall()
    {
        Debug.Log("Positioning ball");
        // Place the ball on the opposite side of the table
        GameObject ball = GameObject.FindGameObjectWithTag("Ball");
        if (ball != null)
        {
            float ballHeight = TableTennisPhysicsConfig.TableHeightMeters + 0.2f; // 20 cm above the table
            float ballPositionZ = -TableTennisPhysicsConfig.TableLengthMeters / 4f; // Move ball away from player
            ball.transform.position = new Vector3(0f, ballHeight, ballPositionZ);
            Debug.Log($"Ball positioned at: {ball.transform.position}");
        }
        else
        {
            Debug.LogError("Ball not found in the scene!");
        }
    }
}

/// <summary>
/// Handles initialization of core game systems.
/// </summary>
public class SystemInitializer
{
    private readonly GameObject _gameObject;

    public SystemInitializer(GameObject gameObject)
    {
        _gameObject = gameObject;
    }

    public void InitializeSystems()
    {
        Time.fixedDeltaTime = TableTennisPhysicsConfig.PhysicsFixedTimestep;

        _gameObject.AddComponent<VRInputManager>();
        _gameObject.AddComponent<PhysicsManager>();
        _gameObject.AddComponent<PerformanceMonitor>();

        Debug.Log("Core systems initialized");
    }
}