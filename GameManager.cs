using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour {

    static GameManager GameManagerInstance;

    public MapGenerator Map;
    public UIController UI;
    
    public ParticleSystem ObjectDestroyedEffect;

    public enum Action { NONE, IDLE, MOVE, GUNFIRE, OVERWATCH, THROW_GRENADE };
    int CurrentAction;
    bool TargetChanged; //OnMove에서 블럭을 생성하기 위한 변수

    public List<Agent> AllyAgent;
    public List<Agent> EnemyAgent;

    Agent AgentOnAttack;

    Agent CurrentAgentSelected;
    int CurrentAgentIndex;
    
    public Vector3 ClickedPosition;
    
    public Agent AllyAgentPrefab;
    public Agent EnemyAgentPrefab;

    //GameManagerUI
    public GameObject SelectedBlockPrefab;
    public GameObject FullCoverPrefab;
    public GameObject HalfCoverPrefab;
    public GameObject GrenadeExplosionAreaPrefab;
    bool PrefabGenerated;

    bool GameStart;
    bool PlayerTurn;
    bool TurnEnd;
    bool MissionEnd;
    bool Pause;

    bool MoveButtonPressed;
    bool FireButtonPressed;
    bool OverwatchButtonPressed;
    bool GrenadeButtonPressed;
    bool ConfirmButtonPressed;
    bool CancelButtonPressed;
    bool RightButtonPressed;
    bool LeftButtonPressed;

    LineRenderer LineRendererForGM;

    void Awake()
    {
        GameManagerInstance = this;
        LineRendererForGM = GetComponent<LineRenderer>();
        LineRendererForGM.startColor = Color.red;
        LineRendererForGM.endColor = Color.red;
        LineRendererForGM.startWidth = 0.1f;
        LineRendererForGM.endWidth = 0.1f;
        LineRendererForGM.positionCount = 25;
    }
    // Use this for initialization
    void Start() {

        Screen.SetResolution(900, 1600, true);

        GameStart = false;
        PlayerTurn = false;
        TurnEnd = true;
        MissionEnd = false;
        TargetChanged = true;
        PrefabGenerated = false;

        MoveButtonPressed = false;
        FireButtonPressed = false;
        OverwatchButtonPressed = false;
        GrenadeButtonPressed = false;
        ConfirmButtonPressed = false;
        CancelButtonPressed = false;

        RightButtonPressed = false;
        LeftButtonPressed = false;

        SpawnAlly(3);
        SpawnEnemy(3);
    }

    // Update is called once per frame
    void Update() {

        if (GameStart)
        {
            if (!Pause)
            {
                if (!MissionEnd) //미션이 끝나지 않았다면 실행
                {
                    if (TurnEnd) //턴이 끝났다면 실행
                    {
                        if (PlayerTurn) //플레이어 턴이 끝났다면
                        {
                            //적들 행동력 초기화
                            for (int i = 0; i < EnemyAgent.Count; i++)
                            {
                                EnemyAgent[i].AgentInitialize();
                            }
                            //현재 캐릭터 설정
                            CurrentAgentIndex = 0;
                            CurrentAgentSelected = EnemyAgent[CurrentAgentIndex];
                            //UI를 화면에서 비우기
                            UI.SelectUI((int)Action.NONE);
                            EraseUIHolder();
                            Map.RemoveInstanceBlock();
                            //카메라를 다시 원위치로
                            UI.CameraReposition();
                            PlayerTurn = false;
                        }
                        else// 적 턴이 끝났다면
                        {
                            //아군들 행동력 초기화
                            for (int i = 0; i < AllyAgent.Count; i++)
                            {
                                AllyAgent[i].AgentInitialize();
                            }
                            //현재 캐릭터 설정
                            CurrentAgentIndex = 0;
                            CurrentAgentSelected = AllyAgent[CurrentAgentIndex];
                            //이동 선택
                            SwitchTo((int)Action.IDLE);
                            PlayerTurn = true;

                            UI.CameraFollowAgent(CurrentAgentSelected.transform);
                        }
                        TurnEnd = false;
                    }
                    if (!UI.CameraOnMove)//카메라가 움직이는 중이 아니라면
                    {
                        HealthBarReposition();
                        UI.MoveCamera(CurrentAction);

                        //공격 가능한 대상이 공격 실행
                        if (AgentOnAttack != null)
                        {
                            if (AgentOnAttack.GunFire(AgentOnAttack.TargetInSight[AgentOnAttack.TargetIndex], GetAccuracyOn(AgentOnAttack, AgentOnAttack.TargetInSight[AgentOnAttack.TargetIndex])))
                            {
                                UI.ShowMessage(AgentOnAttack.TargetInSight[AgentOnAttack.TargetIndex].transform, "2 대미지");

                                if (AgentOnAttack.TargetInSight[AgentOnAttack.TargetIndex].IsDead())
                                {
                                    GameObject DeadAgent = AgentOnAttack.TargetInSight[AgentOnAttack.TargetIndex].gameObject;
                                    if (AgentOnAttack.isOnSide == (int)Agent.Side.ALLY)
                                        EnemyAgent.Remove(AgentOnAttack.TargetInSight[AgentOnAttack.TargetIndex]);
                                    else
                                        AllyAgent.Remove(AgentOnAttack.TargetInSight[AgentOnAttack.TargetIndex]);
                                    EffectOnDestroy(DeadAgent.transform, Color.white);
                                    Destroy(DeadAgent);
                                }
                            }
                            else
                            {
                                UI.ShowMessage(AgentOnAttack.TargetInSight[AgentOnAttack.TargetIndex].transform, "Miss");
                            }
                            
                            HealthBarReposition();

                            AgentOnAttack = null;
                            SwitchTo((int)Action.IDLE);
                            StartCoroutine("OneSecondLater");

                        }

                        if (CurrentAgentSelected != null)//현재 캐릭터가 죽고 파괴되면 NULL값이 반환된다, 따라서 NULL이 아니어야만 실행가능
                        {
                            if (!CurrentAgentSelected.IsMoving) //행동을 하는 동안에는 입력을 받지 않는다
                            {
                                if (PlayerTurn) //플레이어 턴
                                {
                                    UI.SelectUI(CurrentAction);

                                    //캐릭터가 선택되었는지 확인용으로 빨간 원을 띄워준다
                                    for (int i = 0; i < AllyAgent.Count; i++)
                                    {
                                        AllyAgent[i].isSelected(false);
                                    }
                                    CurrentAgentSelected.isSelected(true);

                                    if (Input.GetKeyDown(KeyCode.Escape) || CancelButtonPressed) //esc를 누르면 이동선택으로 바뀐다
                                    {
                                        CancelButtonPressed = false;
                                        SwitchTo((int)Action.IDLE);
                                    }

                                    RaycastHit hitInfo;
                                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                                    if (Physics.Raycast(ray, out hitInfo))
                                    {
                                        //캐릭터를 선택했다면 그 캐릭터로 변경된다
                                        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                                        {
                                            Agent SelectAgent = hitInfo.transform.GetComponent<Agent>();

                                            if (SelectAgent != null)
                                            {
                                                if (AllyAgent.Contains(SelectAgent))
                                                {
                                                    CurrentAgentIndex = AllyAgent.FindIndex(item => item == SelectAgent);
                                                    CurrentAgentSelected = AllyAgent[CurrentAgentIndex];
                                                    UI.ShowMovementcount(CurrentAgentSelected.movecount);
                                                    SwitchTo((int)Action.IDLE);
                                                    UI.CameraFollowAgent(CurrentAgentSelected.transform);
                                                    TargetChanged = true;
                                                }
                                            }
                                        }
                                    }

                                    if (CurrentAgentSelected.movecount > 0) //행동력이 남아있을 때 실행
                                    {
                                        //현재 행동에 따라 실행된다
                                        if (CurrentAction == (int)Action.IDLE)
                                        {
                                            UI.ShowMovementcount(CurrentAgentSelected.movecount);
                                        }
                                        else if (CurrentAction == (int)Action.MOVE)
                                        {
                                            UI.ShowMovementcount(CurrentAgentSelected.movecount);
                                            OnMove();
                                        }
                                        else if (CurrentAction == (int)Action.GUNFIRE)
                                        {
                                            OnAttack();
                                        }
                                        else if (CurrentAction == (int)Action.OVERWATCH)
                                        {
                                            OnOverwatch();
                                        }
                                        else if (CurrentAction == (int)Action.THROW_GRENADE)
                                        {
                                            OnThrowGrenade();
                                        }

                                        //각 버튼별로 행동 선택
                                        if ((Input.GetKeyDown(KeyCode.Alpha1)||MoveButtonPressed))
                                        {
                                            MoveButtonPressed = false;
                                            TargetChanged = true;
                                            SwitchTo((int)Action.MOVE);
                                        }
                                        if ((Input.GetKeyDown(KeyCode.Alpha2) || FireButtonPressed) && !CurrentAgentSelected.HasAttacked)
                                        {
                                            FireButtonPressed = false;
                                            SwitchTo((int)Action.GUNFIRE);
                                        }
                                        if ((Input.GetKeyDown(KeyCode.Alpha3) || OverwatchButtonPressed) && !CurrentAgentSelected.HasAttacked)
                                        {
                                            OverwatchButtonPressed = false;
                                            SwitchTo((int)Action.OVERWATCH);
                                        }
                                        if ((Input.GetKeyDown(KeyCode.Alpha4) || GrenadeButtonPressed) && !CurrentAgentSelected.HasAttacked && !CurrentAgentSelected.GrenadeUsed)
                                        {
                                            GrenadeButtonPressed = false;
                                            SwitchTo((int)Action.THROW_GRENADE);
                                        }
                                    }
                                    else //행동력이 남아있지 않다면
                                    {
                                        //행동력을 갱신해주고 UI를 정리한다
                                        UI.ShowMovementcount(CurrentAgentSelected.movecount);
                                        Map.RemoveInstanceBlock();
                                        EraseUIHolder();
                                    }
                                }
                                else //AI턴
                                {
                                    EnemyAgentAct();
                                }
                            }
                            else //캐릭터가 움직임 실행
                            {
                                CurrentAgentSelected.AgentOnMove(); //캐릭터를 움직여준다
                                UI.CameraFollowAgent(CurrentAgentSelected.transform);

                                if (!PlayerTurn) //적의 턴에 경계스킬을 발동
                                {
                                    for (int i = 0; i < AllyAgent.Count; i++)
                                    {
                                        if (AllyAgent[i].IsOnOverwatch)
                                        {
                                            if (AllyAgent[i].EnemyInSight(EnemyAgent))
                                            {
                                                AllyAgent[i].AgentModel.transform.LookAt(AllyAgent[i].TargetInSight[AllyAgent[i].TargetIndex].transform.position);
                                                AttackOn(AllyAgent[i]);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if(AllyAgent.Count > 0 && EnemyAgent.Count > 0)// 현재 캐릭터가 파괴되었다면 실행
                        {
                            //누구 턴이냐에 따라 새로운 대상으로 변경
                            if (PlayerTurn)
                            {
                                CurrentAgentSelected = AllyAgent[0];
                            }
                            else
                            {
                                CurrentAgentSelected = EnemyAgent[0];
                            }
                        }
                    }
                    else//카메라 움직임 실행
                    {
                        UI.CameraMovingToTarget();
                    }
                }

                if (EnemyAgent.Count <= 0)//적들이 모두 사라졌다면 미션 성공
                {
                    MissionEnd = true;
                    UI.MissionEndUI(true);
                    EraseUIHolder();
                }
                else if (AllyAgent.Count <= 0)//아군이 모두 사라졌다면 미션 실패
                {
                    MissionEnd = true;
                    UI.MissionEndUI(false);
                    EraseUIHolder();
                }
            }
        }
    }
    
    public static GameManager GetInstance()
    {
        return GameManagerInstance;
    }

    //아군 생성
    public void SpawnAlly(int agentNumber)
    {
        Vector3[] AllySpawnPositions = { new Vector3(2, 1, 2), new Vector3(2, 1, 1), new Vector3(1, 1, 2) };

        for (int i = 0; i < agentNumber; i++)
        {
            Agent newAlly = Instantiate(AllyAgentPrefab, AllySpawnPositions[i], Quaternion.identity);
            newAlly.SetAgentStatus(new CharacterStatus(5, 2, 80, 6, 15));// HP, ATK, ACC, MOVE, SIGHT
            newAlly.isOnSide = (int)Agent.Side.ALLY;
            newAlly.AgentSpecialty = (int)Agent.Specialty.SOLDIER;
            AllyAgent.Add(newAlly);
        }
    }

    //적군 생성
    public void SpawnEnemy(int agentNumber)
    {
        Vector3[] EnemySpawnPositions = { new Vector3(19, 1, 19), new Vector3(19, 1, 20), new Vector3(20, 1, 19) };

        for (int i = 0; i < agentNumber; i++)
        {
            Agent newEnemy = Instantiate(EnemyAgentPrefab, EnemySpawnPositions[i], Quaternion.identity);
            newEnemy.SetAgentStatus(new CharacterStatus(5, 2, 80, 6, 15));// HP, ATK, ACC, MOVE, SIGHT
            newEnemy.isOnSide = (int)Agent.Side.ENEMY;
            EnemyAgent.Add(newEnemy);
        }
    }

    //현재 행동 변경
    public void SwitchTo(int action)
    {
        if(action == (int)Action.IDLE)
        {
            CurrentAction = (int)Action.IDLE;
            //카메라 위치
            UI.CameraReposition();
        }
        else if (action == (int)Action.MOVE) //이동
        {
            CurrentAction = (int)Action.MOVE;
            //현재 이동 가능한 블럭 표시
            UI.ShowMovementcount(CurrentAgentSelected.movecount);
            MoveLocation = null;
            //카메라 위치
            UI.CameraReposition();
        }
        else if(action == (int)Action.GUNFIRE) //총 발사
        {
            CurrentAction = (int)Action.GUNFIRE;

            //현재 캐릭터 시야 안에 보이는 적 확인
            if (CurrentAgentSelected.EnemyInSight(EnemyAgent))//시야 안에 적이 있다면 실행
            {
                //현재 캐릭터의 시점으로 카메라 이동
                CurrentAgentSelected.AgentModel.transform.LookAt(CurrentAgentSelected.TargetInSight[CurrentAgentSelected.TargetIndex].transform.position);
                //첫번째 타켓 설정
                CurrentAgentSelected.TargetIndex = 0;
            }
            else//적이 안 보이는 경우
            {
                CurrentAgentSelected.AgentModel.transform.LookAt(EnemyAgent[0].transform.position);
            }

            UI.MoveCameraTo(CurrentAgentSelected.AgentView.transform);
        }
        else if (action == (int)Action.OVERWATCH) //경계
        {
            CurrentAction = (int)Action.OVERWATCH;
            //카메라 재위치
            UI.CameraReposition();
        }
        else if (action == (int)Action.THROW_GRENADE) //수류탄 투척
        {
            CurrentAction = (int)Action.THROW_GRENADE;
            ThrowLocation = new Vector3(-1, -1, -1);
            //수류탄 생성
            PrefabGenerated = false;
            
            UI.CameraReposition();
        }

        EraseUIHolder();
        Map.RemoveInstanceBlock();

        CurrentAgentSetPos();
        UI.ShowMovementcount(CurrentAgentSelected.movecount);
        TargetChanged = true;
    }

    //적 캐릭터의 행동
    public void EnemyAgentAct()
    {
        if (CurrentAgentSelected.movecount > 0)
        {
            System.Random prng = new System.Random();
            
            if (CurrentAgentSelected.EnemyInSight(AllyAgent) && !CurrentAgentSelected.HasAttacked)//공격가능하다면 공격 실행
            {
                CurrentAgentSelected.AgentModel.transform.LookAt(CurrentAgentSelected.TargetInSight[CurrentAgentSelected.TargetIndex].transform.position);
                AttackOn(CurrentAgentSelected);
                CurrentAgentSelected.movecount--;
            }
            else
            {
                Vector3 AgentPosition = CurrentAgentSelected.transform.position;

                //가장 가까운 적 탐색 후 이동
                Agent nearestAgent = AllyAgent[0];
                for(int i = 0; i<AllyAgent.Count; i++)
                {
                    if (Vector3.Distance(AgentPosition, AllyAgent[i].transform.position) < Vector3.Distance(AgentPosition, nearestAgent.transform.position))
                        nearestAgent = AllyAgent[i];
                }

                Node movePosition = Map.BestPosition(CurrentAgentSelected, nearestAgent.transform.position);
                
                if (Map.FindPath(Utility.PositionToNode(CurrentAgentSelected.transform.position), movePosition))
                {
                    UI.CameraReposition();
                    CurrentAgentSelected.MoveAgentOnGrid(Map.agentPath);
                    CurrentAgentSelected.movecount--;
                }
            }
        }
        else
        {
            CurrentAgentIndex++;
            if (CurrentAgentIndex < EnemyAgent.Count)
            {
                CurrentAgentSelected = EnemyAgent[CurrentAgentIndex];
            }
            else
                TurnEnd = true;
        }
    }

    //이동 행동
    GameObject MoveLocation;
    public void OnMove()
    {
        if(TargetChanged)
        {
            Map.ShowMoveableSpace(CurrentAgentSelected);
            TargetChanged = !TargetChanged;
        }

        string holderName = "Instance UI";

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);

                if (Physics.Raycast(ray, out hit))
                {
                    MoveLocation = hit.transform.gameObject;
                    
                    //프레임 단위에서 1번만 돌아가도록 하기 위해 이전과 다른 곳을 터치했을 때만 실행
                    if (Utility.PointOnGrid(MoveLocation.transform.position) != ClickedPosition)
                    {
                        ClickedPosition = Utility.PointOnGrid(MoveLocation.transform.position);
                        if (transform.Find(holderName))
                        {
                            DestroyImmediate(transform.Find(holderName).gameObject);
                        }

                        Transform Holder = new GameObject(holderName).transform;
                        Holder.parent = this.transform;

                        GameObject cursor = Instantiate(SelectedBlockPrefab, ClickedPosition, Quaternion.identity);
                        cursor.transform.parent = Holder;

                        List<Vector3> SpaceToMove = Map.MovableSpace(CurrentAgentSelected);

                        //이동가능할때만 실행
                        if (SpaceToMove.Contains(Utility.PointOnGrid(MoveLocation.transform.position)))
                        {
                            //이동 경로 표시
                            if (Map.FindPath(Utility.PositionToNode(CurrentAgentSelected.transform.position), Utility.PositionToNode(MoveLocation.transform.position)))
                            {
                                Vector3[] PathArray = new Vector3[Map.agentPath.Count + 1];

                                for (int i = 0; i < Map.agentPath.Count; i++)
                                {
                                    PathArray[i] = Utility.NodeToPosition(Map.agentPath[i]) + new Vector3(0, 0.1f, 0);
                                }
                                PathArray[Map.agentPath.Count] = Utility.PointOnGrid(CurrentAgentSelected.transform.position) + new Vector3(0, 0.1f, 0);

                                LineRendererForGM.positionCount = PathArray.Length;
                                LineRendererForGM.SetPositions(PathArray);
                            }

                            //이동 장소의 엄폐 상태 표시
                            List<Node> adjacentNode = Map.AdjacentNode(MoveLocation.transform.position);

                            for (int i = 0; i < adjacentNode.Count; i++)
                            {
                                Vector3 instantiatePoint = new Vector3(Mathf.Lerp(MoveLocation.transform.position.x, adjacentNode[i].x, 0.45f), 1f, Mathf.Lerp(MoveLocation.transform.position.z, adjacentNode[i].z, 0.45f));

                                if (adjacentNode[i].Tag == (int)Node.NodeTag.HIGH_OBSTACLE)
                                {
                                    GameObject ShowCover = Instantiate(FullCoverPrefab, instantiatePoint, Quaternion.identity);
                                    ShowCover.transform.parent = Holder;
                                    if (adjacentNode[i].x == MoveLocation.transform.position.x)
                                        ShowCover.transform.Rotate(new Vector3(0, 90, 0));
                                }
                                else if (adjacentNode[i].Tag == (int)Node.NodeTag.OBSTACLE)
                                {
                                    GameObject ShowCover = Instantiate(HalfCoverPrefab, instantiatePoint, Quaternion.identity);
                                    ShowCover.transform.parent = Holder;
                                    if (adjacentNode[i].x == MoveLocation.transform.position.x)
                                        ShowCover.transform.Rotate(new Vector3(0, 90, 0));
                                }
                            }
                        }
                        else //이동경로 표시 삭제
                        {
                            LineRendererForGM.positionCount = 0;
                        }
                    }

                    //아군 캐릭터를 터치하면 아군 캐릭터로 변경
                    if (MoveLocation.GetComponent<Agent>() != null)
                    {
                        if (AllyAgent.Contains(MoveLocation.GetComponent<Agent>()))
                        {
                            CurrentAgentIndex = AllyAgent.FindIndex(item => item == MoveLocation.GetComponent<Agent>());
                            CurrentAgentSelected = AllyAgent[CurrentAgentIndex];
                            TargetChanged = true;
                            SwitchTo((int)Action.IDLE);
                            UI.SelectUI(CurrentAction);
                        }
                    }
                }
            }
        }

        //이동 결정
        if (ConfirmButtonPressed && MoveLocation != null)
        {
            if (AgentMove(MoveLocation))
            {
                SwitchTo((int)Action.IDLE);
                UI.SelectUI(CurrentAction);
                ConfirmButtonPressed = false;
                EraseUIHolder();
                Map.RemoveInstanceBlock();
                TargetChanged = true;
            }
        }

        UI.ShowMovementcount(CurrentAgentSelected.movecount);
    }

    //공격 행동
    public void OnAttack()
    {
        if (CurrentAgentSelected.TargetInSight.Count > 0)
        {
            //공격 대상 변경
            if (Input.GetKeyDown(KeyCode.Q) || LeftButtonPressed)
            {
                LeftButtonPressed = false;
                if (CurrentAgentSelected.TargetIndex > 0)
                    CurrentAgentSelected.TargetIndex--;
                else
                    CurrentAgentSelected.TargetIndex = CurrentAgentSelected.TargetInSight.Count - 1;
                CurrentAgentSelected.AgentModel.transform.LookAt(CurrentAgentSelected.TargetInSight[CurrentAgentSelected.TargetIndex].transform.position);
                UI.MoveCameraTo(CurrentAgentSelected.AgentView.transform);
            }
            else if (Input.GetKeyDown(KeyCode.E) || RightButtonPressed)
            {
                RightButtonPressed = false;
                if (CurrentAgentSelected.TargetIndex < CurrentAgentSelected.TargetInSight.Count - 1)
                    CurrentAgentSelected.TargetIndex++;
                else
                    CurrentAgentSelected.TargetIndex = 0;
                CurrentAgentSelected.AgentModel.transform.LookAt(CurrentAgentSelected.TargetInSight[CurrentAgentSelected.TargetIndex].transform.position);
                UI.MoveCameraTo(CurrentAgentSelected.AgentView.transform);
            }

            //공격 행동 결정
            if (Input.GetKeyDown(KeyCode.Space) || ConfirmButtonPressed)
            {
                ConfirmButtonPressed = false;
                AttackOn(CurrentAgentSelected);
                UI.SelectUI(CurrentAction);
                CurrentAgentSelected.movecount = 0;
            }

            UI.ShowAttackPanel(CurrentAgentSelected, GetAccuracyOn(CurrentAgentSelected, CurrentAgentSelected.TargetInSight[CurrentAgentSelected.TargetIndex]));
        }
    }

    //공격 대기 상태로 변경
    public void AttackOn(Agent AttakingAgent)
    {
        UI.MoveCameraTo(AttakingAgent.AgentView.transform);
        AgentOnAttack = AttakingAgent;
    }

    //경계 행동
    public void OnOverwatch()
    {
        //경계 행동 결정
        if (Input.GetKeyDown(KeyCode.Space)|| ConfirmButtonPressed)
        {
            ConfirmButtonPressed = false;
            SwitchTo((int)Action.IDLE);

            UI.ShowMessage(CurrentAgentSelected.transform, "경계모드");

            CurrentAgentSelected.Overwatch();
            Map.RemoveInstanceBlock();
            EraseUIHolder();
            CurrentAgentSelected.movecount = 0;
        }
    }

    //투척 행동
    Vector3 ThrowLocation;
    public void OnThrowGrenade()
    {
        string holderName = "Instance UI";

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);

                if (Physics.Raycast(ray, out hit))
                {
                    if (Vector3.Distance(CurrentAgentSelected.transform.position, hit.point) > 10f)
                    {
                        ThrowLocation = CurrentAgentSelected.transform.position + (hit.point - CurrentAgentSelected.transform.position).normalized * 10f;
                    }
                    else
                    {
                        ThrowLocation = hit.point;
                    }

                    if (transform.Find(holderName))
                    {
                        if (PrefabGenerated)
                        {
                            transform.Find(holderName).Find("GrenadeExplosionArea(Clone)").transform.position = ThrowLocation;
                        }
                        else
                        {
                            GameObject showGrenadeThrowArea = Instantiate(GrenadeExplosionAreaPrefab, ThrowLocation, Quaternion.identity);
                            showGrenadeThrowArea.transform.parent = transform.Find(holderName);
                            PrefabGenerated = true;
                        }
                    }
                    else
                    {
                        Transform Holder = new GameObject(holderName).transform;
                        Holder.parent = this.transform;

                        GameObject showGrenadeThrowArea = Instantiate(GrenadeExplosionAreaPrefab, ThrowLocation, Quaternion.identity);
                        showGrenadeThrowArea.transform.parent = transform.Find(holderName);
                        PrefabGenerated = true;
                    }

                    //투척 경로 표시와 모델 자세 변경
                    if(PrefabGenerated)
                    {
                        float onX = ThrowLocation.x - CurrentAgentSelected.transform.position.x;
                        float onZ = ThrowLocation.z - CurrentAgentSelected.transform.position.z;
                        Vector3 targetDirection = ThrowLocation - CurrentAgentSelected.transform.position;
                        Vector3 agentPosition = CurrentAgentSelected.AgentModel.transform.position;

                        CurrentAgentSelected.AgentModel.transform.position = Utility.PointOnGrid(agentPosition) - new Vector3(targetDirection.normalized.x / 2f, -1f, targetDirection.normalized.z / 2f);

                        CurrentAgentSelected.AgentModel.transform.LookAt(ThrowLocation + Vector3.up);
                        CurrentAgentSelected.AgentGun.transform.rotation =
                            Quaternion.LookRotation(ThrowLocation - CurrentAgentSelected.AgentModel.transform.position + new Vector3(0, Vector3.Distance(ThrowLocation,
                            CurrentAgentSelected.transform.position) * Vector3.Distance(ThrowLocation, CurrentAgentSelected.transform.position) / 10, 0));

                        ShowTrajectory(CurrentAgentSelected.GrenadeLauncher.position, ThrowLocation);
                    }
                }
            }
        }

        //투척 행동 결정
        if (ConfirmButtonPressed && ThrowLocation != null)
        {
            ConfirmButtonPressed = false;

            CurrentAgentSelected.ThrowGrenade(ThrowLocation);

            SwitchTo((int)Action.IDLE);
            CurrentAgentSelected.movecount = 0;
            EraseUIHolder();
        }
    }

    //폭발 범위 내 오브젝트에 대한 처리
    public void EffectInArea(Vector3 target)
    {
        Collider[] hitColliders = Physics.OverlapSphere(target, 2.5f);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].GetComponent<Agent>())
            {
                Agent agentInArea = hitColliders[i].GetComponent<Agent>();
                agentInArea.TakeDamage(2);
                if (agentInArea.IsDead())
                {
                    GameObject DeadAgent = agentInArea.gameObject;
                    if (agentInArea == CurrentAgentSelected)
                        TargetChanged = true;

                    if (agentInArea.isOnSide == (int)Agent.Side.ALLY)
                    {
                        AllyAgent.Remove(agentInArea);
                    }
                    else
                    {
                        EnemyAgent.Remove(agentInArea);
                    }
                    EffectOnDestroy(DeadAgent.transform, Color.white);
                    Destroy(DeadAgent);
                }
            }
            else
            {
                Map.InExplosionArea(hitColliders[i].gameObject);
            }

        }
    }

    //투척 경로 표시
    public void ShowTrajectory(Vector3 LaunchingPosition, Vector3 Target)
    {
        KinematicMovement.LaunchData launchData = KinematicMovement.CalculateLaunchData(LaunchingPosition, Target);
        Vector3 previousDrawPoint = LaunchingPosition;
        
        int resolution = 30;
        Vector3[] PathArray = new Vector3[resolution];

        for (int i = 1; i <= resolution; i++)
        {
            float simulationTime = i / (float)resolution * launchData.timeToTarget;
            Vector3 displacement = launchData.initialVelocity * simulationTime + Vector3.up * KinematicMovement.gravity * simulationTime * simulationTime / 2f;
            Vector3 drawPoint = LaunchingPosition + displacement;

            PathArray[i - 1] = drawPoint;
        }

        LineRendererForGM.positionCount = PathArray.Length;
        LineRendererForGM.SetPositions(PathArray);
    }

    //명중률 계산 후 반환
    public int GetAccuracyOn(Agent AttackingAgent ,Agent target)
    {
        float currentAgentAccuracy = AttackingAgent.agentStatus.ACC;

        List<Node> targetCover = Map.AdjacentNode(target.transform.position);

        Node attackerPosition = Utility.PositionToNode(AttackingAgent.transform.position);
        Node targetPosition = Utility.PositionToNode(target.transform.position);

        for (int i = 0; i < targetCover.Count; i++)
        {
            //상대의 엄폐상태와 각도에 따른 계산
            if ((targetCover[i].x < attackerPosition.x && targetCover[i].x > targetPosition.x) || (targetCover[i].x > attackerPosition.x && targetCover[i].x < targetPosition.x))
            {
                float accuracyCorrection;
                if (attackerPosition.z != targetPosition.z)
                {
                    if (Mathf.Abs((attackerPosition.x - targetPosition.x) / (attackerPosition.z - targetPosition.z)) >= 1)
                        accuracyCorrection = 1;
                    else
                        accuracyCorrection = Mathf.Abs((attackerPosition.x - targetPosition.x) / (float)(attackerPosition.z - targetPosition.z));
                }
                else
                    accuracyCorrection = 1;

                if (targetCover[i].Tag == (int)Node.NodeTag.HIGH_OBSTACLE)
                {
                    currentAgentAccuracy -= 40 * accuracyCorrection;
                }
                else if(targetCover[i].Tag == (int)Node.NodeTag.OBSTACLE)
                {
                    currentAgentAccuracy -= 20 * accuracyCorrection;
                }
            }
            else if((targetCover[i].z < attackerPosition.x && targetCover[i].z > targetPosition.z) || (targetCover[i].z > attackerPosition.x && targetCover[i].z < targetPosition.z))
            {
                float accuracyCorrection;
                if (attackerPosition.x != targetPosition.x)
                {
                    if (Mathf.Abs((attackerPosition.z - targetPosition.z) / (attackerPosition.x - targetPosition.x)) >= 1)
                        accuracyCorrection = 1;
                    else
                        accuracyCorrection = Mathf.Abs((attackerPosition.z - targetPosition.z) / (float)(attackerPosition.x - targetPosition.x));
                }
                else
                    accuracyCorrection = 1;

                if (targetCover[i].Tag == (int)Node.NodeTag.HIGH_OBSTACLE)
                {
                    currentAgentAccuracy -= 40 * accuracyCorrection;
                }
                else if (targetCover[i].Tag == (int)Node.NodeTag.OBSTACLE)
                {
                    currentAgentAccuracy -= 20 * accuracyCorrection;
                }
            }
        }
        return (int)currentAgentAccuracy;
    }

    //파괴 효과
    public void EffectOnDestroy(Transform destroyedObject, Color color)
    {
        ParticleSystem effect = Instantiate(ObjectDestroyedEffect, destroyedObject.position + Vector3.up, destroyedObject.rotation);
        ParticleSystem.MainModule main = effect.GetComponent<ParticleSystem>().main;
        main.startColor = color;
        effect.Play();
        Destroy(effect, effect.main.duration);
    }
    
    //UI 삭제
    public void EraseUIHolder()
    {
        string holderName = "Instance UI";
        PrefabGenerated = false;
        if (transform.Find(holderName))
        {
            DestroyImmediate(transform.Find(holderName).gameObject);
        }
        LineRendererForGM.positionCount = 0;
    }

    //캐릭터 움직임
    public bool AgentMove(GameObject target)
    {
        if (Map.MovableSpace(CurrentAgentSelected).Contains(Utility.PointOnGrid(target.transform.position)))
        {
            if (Map.FindPath(Utility.PositionToNode(CurrentAgentSelected.transform.position), Utility.PositionToNode(target.transform.position)))
            {
                CurrentAgentSelected.MoveAgentOnGrid(Map.agentPath);
                CurrentAgentSelected.movecount--;
                
                return true;
            }
        }
        return false;
    }

    //체력 바 표시
    public void HealthBarReposition()
    {
        for(int i = 0; i < AllyAgent.Count; i++)
        {
            AllyAgent[i].HealthBar.transform.rotation = Quaternion.LookRotation(-UI.mainCamera.transform.forward);
            AllyAgent[i].SetAgentHealthBar();
        }

        for (int j = 0; j < EnemyAgent.Count; j++)
        {
            EnemyAgent[j].HealthBar.transform.rotation = Quaternion.LookRotation(-UI.mainCamera.transform.forward);
            EnemyAgent[j].SetAgentHealthBar();
        }
    }

    //모델 자세 초기화
    public void CurrentAgentSetPos()
    {
        CurrentAgentSelected.AgentGun.transform.localEulerAngles = new Vector3(0, 0, 0);
        CurrentAgentSelected.AgentModel.transform.localEulerAngles = new Vector3(0, CurrentAgentSelected.AgentModel.transform.localEulerAngles.y, 0);
        CurrentAgentSelected.AgentModel.transform.position = Utility.PointOnGrid(CurrentAgentSelected.AgentModel.transform.position) + Vector3.up;
    }

    public bool CurrentAgentMoving()
    {
        return CurrentAgentSelected.IsMoving;
    }

    public Vector3 CurrentAgentPosition()
    {
        return CurrentAgentSelected.transform.position;
    }
    
    public Vector3 EnemyPosition()
    {
        return CurrentAgentSelected.TargetInSight[CurrentAgentSelected.TargetIndex].transform.position;
    }

    public Vector3 MovePosition()
    {
        if(MoveLocation != null)
            return MoveLocation.transform.position;
        return new Vector3(-1, -1, -1);
    }

    public Vector3 ThrowPositon()
    {
        return ThrowLocation;
    }

    public List<Agent> AllAgents()
    {
        List<Agent> AgentsList = new List<Agent>();

        for(int i = 0; i < AllyAgent.Count; i++)
        {
            AgentsList.Add(AllyAgent[i]);
        }
        for (int i = 0; i < EnemyAgent.Count; i++)
        {
            AgentsList.Add(EnemyAgent[i]);
        }

        return AgentsList;
    }

    public bool CurrentAgentHasAction()
    {
        if (CurrentAgentSelected.movecount > 0)
            return true;
        return false;
    }

    public bool CurrentAgentHasAttacked()
    {
        return CurrentAgentSelected.HasAttacked;
    }

    public bool CurrentAgentGrenadeUsed()
    {
        return CurrentAgentSelected.GrenadeUsed;
    }

    public int CurrentAgentTargetInSight()
    {
        return CurrentAgentSelected.TargetInSight.Count;
    }
    
    public void EndTurn()
    {
        TurnEnd = true;
    }

    public void MoveButtonPress()
    {
        MoveButtonPressed = true;
    }

    public void FireButtonPress()
    {
        FireButtonPressed = true;
    }

    public void OverwatchButtonPress()
    {
        OverwatchButtonPressed = true;
    }

    public void GrenadeButtonPress()
    {
        GrenadeButtonPressed = true;
    }

    public void ConfirmButtonPress()
    {
        ConfirmButtonPressed = true;
    }

    public void CancelButtonPress()
    {
        CancelButtonPressed = true;
    }

    public void RightButtonPress()
    {
        RightButtonPressed = true;
    }

    public void LeftButtonPress()
    {
        LeftButtonPressed = true;
    }

    public void StartGame()
    {
        GameStart = true;
        UI.SkipIntro();
    }

    IEnumerator OneSecondLater()
    {
        Pause = true;
        yield return new WaitForSeconds(1);
        Pause = false;
    }
}
