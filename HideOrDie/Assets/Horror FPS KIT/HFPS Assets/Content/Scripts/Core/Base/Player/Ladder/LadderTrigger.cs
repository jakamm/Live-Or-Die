using ThunderWire.Utility;
using UnityEngine;

public class LadderTrigger : MonoBehaviour
{
    private Collider colliderSelf;
    private PlayerController player;
    private MouseLook mouseLook;

    [Header("Mouse Lerp")]
    public Vector2 LadderLook;

    [Header("Ladder")]
    public Transform CenterDown;
    public Transform CenterUp;
    public Transform UpFinishPosition;
    [Space(7)]
    public LadderEvent UpFinishTrigger;
    public LadderEvent DownFinishTrigger;
    [Space(7)]
    public float UpDistance;
    public bool IsPlayerUp;

    private bool onLadder;

    void Awake()
    {
        player = PlayerController.Instance;
        mouseLook = ScriptManager.Instance.GetComponent<MouseLook>();
        colliderSelf = GetComponentInChildren<Collider>();
    }

    void Update()
    {
        if (!player) return;

        if(Vector3.Distance(player.transform.position, UpFinishTrigger.transform.position) > UpDistance)
        {
            IsPlayerUp = false;
        }
        else
        {
            IsPlayerUp = true;
        }

        onLadder = player.ladderReady;
        colliderSelf.enabled = player.movementState != PlayerController.MovementState.Ladder;

        UpFinishTrigger.blockTrigger = !player.ladderReady;
        DownFinishTrigger.blockTrigger = !player.ladderReady;
    }

    public void UseObject()
    {
        if (!player) return;

        Vector2 rotation = LadderLook;
        rotation.x -= mouseLook.playerOriginalRotation.eulerAngles.y;

        if (!IsPlayerUp)
        {
            player.UseLadder(CenterDown, rotation, true);
        }
        else
        {
            player.UseLadder(CenterUp, rotation, false);
        }
    }

    public void OnClimbFinish(bool finishUp)
    {
        if (!player) return;

        if (onLadder)
        {
            if (finishUp)
            {
                player.LerpPlayerLadder(UpFinishPosition.position);
            }
            else
            {
                player.LadderExit();
            }
        }
    }

    void OnDrawGizmos()
    {
        if (CenterDown) { Gizmos.color = Color.green; Gizmos.DrawSphere(CenterDown.position, 0.05f); }
        if (CenterUp) { Gizmos.color = Color.red; Gizmos.DrawSphere(CenterUp.position, 0.05f); }
        if (UpFinishPosition) { Gizmos.color = Color.white; Gizmos.DrawSphere(UpFinishPosition.position, 0.1f); }

        PlayerController playerController = PlayerController.Instance;

        if (playerController)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            Vector2 rotation = LadderLook;
            rotation.x -= playerController.transform.eulerAngles.y;

            Vector3 gizmoRotation = Quaternion.Euler(new Vector3(0, rotation.x, rotation.y)) * playerController.transform.forward * 1f;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, gizmoRotation);
        }
    }
}
