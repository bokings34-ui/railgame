using System.Collections;
using UnityEngine;

public enum TrainStateType
{
    Idle,
    Moving,
    Stopped,
    Accelerating,
    Decelerating
}

public enum TrainPhase
{
    Idle,
    Start,
    Progress,
    End
}
public enum TrainDirection
{
    Forward,
    Left,
    Right,
}

public struct TrainData
{
    public float speed;
    public Vector3 position;
    public Quaternion rotation;

}
public class TrainBehaviour : MonoBehaviour
{
    [SerializeField] private TrainSection[] trainSections;

    [SerializeField] private Transform engineRoom;
    [SerializeField] private float speed = 0.1f;
    [SerializeField] private float onceMoveRange = 1f;

    [SerializeField] private TrainPhase trainPhase = TrainPhase.Idle;
    [SerializeField] private TrainDirection trainDirection = TrainDirection.Forward;

    [SerializeField] private TMPro.TextMeshProUGUI countDownText;

    private Vector3 moveForward;
    private int _countDown = 5;

    private void Awake()
    {
    }

    private void FixedUpdate()
    {
        if (trainPhase == TrainPhase.Idle || trainPhase == TrainPhase.Start ||trainPhase == TrainPhase.End) return;

        moveForward = transform.forward * speed;

        engineRoom.position += moveForward;
        if (trainDirection == TrainDirection.Left)
        {

            engineRoom.rotation = Quaternion.Euler(engineRoom.rotation.eulerAngles + new Vector3(0, -1, 0));
            if (engineRoom.rotation.eulerAngles.y <= -90)
            {
                trainDirection = TrainDirection.Forward;
                return;
            }
        }
        else if (trainDirection == TrainDirection.Right)
        {
            engineRoom.rotation = Quaternion.Euler(engineRoom.rotation.eulerAngles + new Vector3(0, 1, 0));
            if (engineRoom.rotation.eulerAngles.y >= 90)
            {
                trainDirection = TrainDirection.Forward;
                return;
            }
        }
    }

    public void StartMoving()
    {
        trainPhase = TrainPhase.Start;
        StartCoroutine(StartCountDown());
    }

    IEnumerator StartCountDown()
    {
        while (_countDown > 0)
        {
            countDownText.text = _countDown.ToString();
            yield return new WaitForSeconds(1f);
            _countDown--;
        }
        countDownText.text = "";
        trainPhase = TrainPhase.Progress;
    }

    private void MoveForward()
    {

    }
}

