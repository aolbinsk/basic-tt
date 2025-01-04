using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Handles initial scene setup and positioning of table tennis elements
/// </summary>
public class TableTennisSceneSetup : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject paddle;

    private GameObject _table;
    private GameObject _net;
    private XROrigin _xrOrigin;

    private void Start()
    {
        Debug.Log("Setting up scene");
        SetupScene();
    }

    private void SetupScene()
    {
        _xrOrigin = player.GetComponent<XROrigin>();
        
        Debug.Log("Setting up scene");
        BuildRoom();
        Debug.Log("Building table and net");
        BuildTableAndNet();
        Debug.Log("Setting up player");
        SetupPlayer();
        Debug.Log("Setting up paddle");
        ScalePaddleToRegulationSize();

        // Set the physics fixed timestep for higher update frequency
        Time.fixedDeltaTime = TableTennisPhysicsConfig.PhysicsFixedTimestep;

        // Configure paddle's collision detection mode
        Rigidbody paddleRigidbody = paddle.GetComponent<Rigidbody>();
        if (paddleRigidbody != null)
        {
            paddleRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        else
        {
            Debug.LogError("No Rigidbody found on paddle!");
        }
    }

    private void BuildTableAndNet()
    {
        BuildTableTop();
        BuildNet();
    }

    private void BuildTableTop()
    {
        Debug.Log("Building table top");
        // Create table top as a cube
        _table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _table.name = "TableTop";

        // Set dimensions using TableTennisPhysicsConfig
        var tableWidth = TableTennisPhysicsConfig.TableWidthMeters;
        var tableLength = TableTennisPhysicsConfig.TableLengthMeters;

        _table.transform.localScale = new Vector3(tableWidth, 
                                               TableTennisPhysicsConfig.TableThicknessMeters, 
                                               tableLength);
        _table.transform.position = new Vector3(0f, TableTennisPhysicsConfig.TableHeightMeters, 0f);

        Debug.Log($"Table dimensions: {_table.transform.localScale}");
        // Assign physics material
        var tableMaterial = TableTennisPhysicsConfig.instance.tableMaterial;
        Debug.Assert(tableMaterial != null, "Table material not set in TableTennisPhysicsConfig");
        
        var tableCollider = _table.GetComponent<Collider>();
        tableCollider.material = tableMaterial;
        Debug.Log($"Table material: {tableMaterial.name}");
    }

    private void BuildNet()
    {
        Debug.Log("Building net");
        // Create the net as a thin box
        _net = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _net.name = "TableTennisNet";

        // Net dimensions from config
        const float netWidth = TableTennisPhysicsConfig.TableWidthMeters + TableTennisPhysicsConfig.NetHeightMeters;
        const float netHeight = TableTennisPhysicsConfig.NetHeightMeters;
        const float netThickness = 0.001f; // Slightly thicker to be more visible

        _net.transform.localScale = new Vector3(netWidth, netHeight, netThickness);

        // Parent to table and position
        //_net.transform.SetParent(_table.transform, false);
        Debug.Log($"Net dimensions: {_net.transform.localScale}");
        const float surfaceOffset = 0.001f; // Small offset to prevent z-fighting
        float netYPosition = TableTennisPhysicsConfig.TableHeightMeters 
                             + TableTennisPhysicsConfig.TableThicknessMeters / 2f
                             + TableTennisPhysicsConfig.NetHeightMeters / 2 + surfaceOffset;
        _net.transform.localPosition = new Vector3(0f, netYPosition, 0f);
        Debug.Log($"Net position: {_net.transform.localPosition}");

        // Add physics properties
        var boxCollider = _net.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.material = TableTennisPhysicsConfig.instance.netMaterial;
            boxCollider.isTrigger = false; // Make it solid for ball collisions
        }

        // Add rigidbody to make it static
        var rb = _net.AddComponent<Rigidbody>();
        rb.isKinematic = true; // Make it immovable
        rb.useGravity = false;

        // Make net visible with a distinct material
        var netRenderer = _net.GetComponent<MeshRenderer>();
        if (netRenderer != null)
        {
            netRenderer.material.color = Color.white; // White net color
        }
    }
    
    private void BuildRoom()
    {
        BuildFloor();
        BuildWalls();
        BuildCeiling();
    }

    private void BuildFloor()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        const float roomSize = TableTennisPhysicsConfig.RoomSizeMeters;
        floor.transform.localScale = new Vector3(roomSize / 10f, 1f, roomSize / 10f); // Plane is 10x10 by default
        floor.transform.position = new Vector3(0f, 0f, 0f);

        var floorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(0.8f, 0.1f, 0.1f) // Reddish color
        };
        floor.GetComponent<Renderer>().material = floorMaterial;
    }

    private void BuildWalls()
    {
        const float wallThickness = 0.1f;
        const float roomSize = TableTennisPhysicsConfig.RoomSizeMeters;
        const float wallHeight = TableTennisPhysicsConfig.WallHeightMeters;

        var wallPositions = new Vector3[]
        {
            new(0f, wallHeight / 2f, -roomSize / 2f), // Back wall
            new(0f, wallHeight / 2f, roomSize / 2f),  // Front wall
            new(-roomSize / 2f, wallHeight / 2f, 0f), // Left wall
            new(roomSize / 2f, wallHeight / 2f, 0f)   // Right wall
        };

        var wallScales = new Vector3[]
        {
            new(roomSize, wallHeight, wallThickness), // Back and front walls
            new(wallThickness, wallHeight, roomSize)  // Left and right walls
        };

        string[] wallNames = { "BackWall", "FrontWall", "LeftWall", "RightWall" };

        for (var i = 0; i < 4; i++)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallNames[i];

            // Assign scale and position
            if (i < 2)
            {
                wall.transform.localScale = wallScales[0];
            }
            else
            {
                wall.transform.localScale = wallScales[1];
            }
            wall.transform.position = wallPositions[i];

            // Apply blueish color
            var wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(0.1f, 0.1f, 0.8f) // Blueish color
            };
            wall.GetComponent<Renderer>().material = wallMaterial;
        }
    }

    private static void BuildCeiling()
    {
        var ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ceiling.name = "Ceiling";
        const float roomSize = TableTennisPhysicsConfig.RoomSizeMeters;
        ceiling.transform.localScale = new Vector3(roomSize / 10f, 1f, roomSize / 10f);
        ceiling.transform.position = new Vector3(0f, TableTennisPhysicsConfig.WallHeightMeters, 0f); // Position at top of walls
        ceiling.transform.Rotate(180f, 0f, 0f); // Invert to face downward

        var ceilingMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = Color.white
        };
        ceiling.GetComponent<Renderer>().material = ceilingMaterial;
    }

    private void SetupPlayer()
    {
        Debug.Log("Setting up player");
        // Wait for XR subsystems to initialize
        StartCoroutine(SetupPlayerAfterXRInit());
    }

    private IEnumerator SetupPlayerAfterXRInit()
    {
        // Wait for XR to initialize
        yield return new WaitForSeconds(0.1f);
        
        // Calculate offset relative to XR space
        float zOffset = TableTennisPhysicsConfig.TableLengthMeters / 2f + 
                        TableTennisPhysicsConfig.PlayerDistanceFromTable;
                       
        // Apply offset to XR Origin
        _xrOrigin.transform.position = new Vector3(
            0f,
            0f, // Let XR handle height
            zOffset
        );
        Debug.Log($"Player offset: {_xrOrigin.transform.position}");
    }

    private float GetPlayerHeight()
    {
        // Try to get VR boundary height
        if (XRDevice.GetTrackingSpaceType() == TrackingSpaceType.RoomScale)
        {
            return InputTracking.GetLocalPosition(XRNode.Head).y;
        }

        // Fallback to configured height
        return TableTennisPhysicsConfig.PlayerHeightMeters;
    }

    private void ScalePaddleToRegulationSize()
    {
        // Get the current mesh bounds
        var paddleMeshFilter = paddle.GetComponentInChildren<MeshFilter>();
        if (paddleMeshFilter == null)
        {
            Debug.LogError("No MeshFilter found on paddle!");
            return;
        }

        var meshBounds = paddleMeshFilter.sharedMesh.bounds;
        var meshSize = Vector3.Scale(meshBounds.size, paddle.transform.localScale);

        // Calculate uniform scale factor needed to reach regulation size
        var lengthScale = TableTennisPhysicsConfig.PaddleLengthMeters / meshSize.z;
        var widthScale = TableTennisPhysicsConfig.PaddleWidthMeters / meshSize.x;

        // Use the smaller scale to maintain proportions
        var uniformScale = Mathf.Min(lengthScale, widthScale);

        paddle.transform.localScale *= uniformScale;
    }
}