using UnityEngine;

/// <summary>
/// Handles initial scene setup and positioning of table tennis elements
/// </summary>
public class TableTennisSceneSetup : MonoBehaviour
{
    [SerializeField] private GameObject table;
    [SerializeField] private GameObject room;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject paddle;

    private void Start()
    {
        SetupScene();
    }

    private void SetupScene()
    {
        // Center table
        var tableScale = new Vector3(
            TableTennisPhysicsConfig.TABLE_WIDTH_METERS,
            TableTennisPhysicsConfig.TABLE_HEIGHT_METERS,
            TableTennisPhysicsConfig.TABLE_LENGTH_METERS
        );
        table.transform.localScale = tableScale;
        table.transform.position = Vector3.zero;

        // Size room relative to table
        var roomScale = TableTennisPhysicsConfig.TABLE_LENGTH_METERS * 4f;
        room.transform.localScale = new Vector3(roomScale, roomScale / 2f, roomScale);

        // Position player
        var playerPos = new Vector3(
            0f,
            TableTennisPhysicsConfig.PLAYER_HEIGHT_METERS,
            -(TableTennisPhysicsConfig.TABLE_LENGTH_METERS / 2 + TableTennisPhysicsConfig.PLAYER_DISTANCE_FROM_TABLE)
        );
        player.transform.position = playerPos;

        // Scale paddle to regulation size
        ScalePaddleToRegulationSize();
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

        var bounds = paddleMeshFilter.sharedMesh.bounds;

        // Calculate scale factors needed to reach regulation size
        float lengthScale = TableTennisPhysicsConfig.PADDLE_LENGTH_METERS / bounds.size.z;
        float widthScale = TableTennisPhysicsConfig.PADDLE_WIDTH_METERS / bounds.size.x;

        // Use the smaller scale to maintain proportions
        float uniformScale = Mathf.Min(lengthScale, widthScale);

        paddle.transform.localScale = Vector3.one * uniformScale;
    }
}