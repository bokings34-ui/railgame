using UnityEngine;

public class TrainController : MonoBehaviour
{
    [SerializeField] private TrainBehaviour train;

    // 게임 시작 신호 받기
    /*private void OnEnable()
    {
        GameManager.OnGameStart += StartGame;
    }*/

    public void Start()
    {
        //작동 테스트    
        StartGame();
    }
    public void StartGame()
    {
        train.StartMoving();
    }
}
