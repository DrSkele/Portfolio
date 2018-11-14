using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIController : MonoBehaviour {

    public GameObject Canvas;
    public GameObject Intro;
    public GameObject SelectingInteface;
    public GameObject ButtonGroup;
    public Button MoveButton;
    public Button AttackButton;
    public Button OverwatchButton;
    public Button GrenadeButton;
    public Button ConfirmButton;
    public Button AttackPanelFireButton;

    public GameObject AttackInterface;
    public GameObject AttackTarget;
    public Button RightTargetButton;
    public Button LeftTargetButton;
    public Text AttackPanel;
    public GameObject MovementCountImage;
    public Text MovementCountText;

    public GameObject MissionSuccessful_Image;
    public GameObject MissionFail_Imange;

    public float CameraMoveSpeed;

    public Transform CameraHolder;
    public Camera mainCamera;
    public Transform originalPosition;
    
    public Text MessageText;

    Transform targetSight;
    bool isCameraMoving;

    public void SkipIntro()
    {
        Intro.SetActive(false);
    }

    //UI 화면 표시
    public void SelectUI(int action)
    {
        SelectingInteface.SetActive(false);
        AttackInterface.SetActive(false);
        ConfirmButton.gameObject.SetActive(false);

        Vector3 MoveButtonPosition = MoveButton.transform.position;
        Vector3 OverwatchButtonPosition = OverwatchButton.transform.position;
        Vector3 GrenadeButtonPosition = GrenadeButton.transform.position;

        //액션에 따라 표시
        if (action == (int)GameManager.Action.GUNFIRE)
        {
            AttackInterface.SetActive(true);
            RightTargetButton.gameObject.SetActive(false);
            LeftTargetButton.gameObject.SetActive(false);

            if (GameManager.GetInstance().CurrentAgentTargetInSight() > 0)
            {
                ColorBlock buttonColor = AttackPanelFireButton.colors;
                buttonColor.normalColor = Color.white;
                AttackPanelFireButton.colors = buttonColor;

                AttackTarget.transform.position = new Vector3(0, 0, 0);
                Vector3 PositionOfUI = mainCamera.WorldToScreenPoint(GameManager.GetInstance().EnemyPosition()) + new Vector3(0, 10, 0);
                AttackTarget.transform.position = PositionOfUI;

                if(GameManager.GetInstance().CurrentAgentTargetInSight() > 1)
                {
                    RightTargetButton.gameObject.SetActive(true);
                    LeftTargetButton.gameObject.SetActive(true);
                }
            }
            else
            {
                AttackTarget.transform.position = new Vector3(-100, -100, 0);

                ColorBlock buttonColor = AttackPanelFireButton.colors;
                buttonColor.normalColor = new Color(0, 0, 0, 0.5f);
                AttackPanelFireButton.colors = buttonColor;
            }
        }
        else
        {
            if (action != (int)GameManager.Action.NONE)
            {
                SelectingInteface.SetActive(true);
                ButtonGroup.SetActive(false);
                ConfirmButton.gameObject.SetActive(false);

                if (action == (int)GameManager.Action.IDLE)
                {
                    if(!GameManager.GetInstance().CurrentAgentMoving())
                    {
                        if (GameManager.GetInstance().CurrentAgentHasAction())
                        {
                            ButtonGroup.SetActive(true);
                            Vector3 PositionOfGroup = mainCamera.WorldToScreenPoint(GameManager.GetInstance().CurrentAgentPosition()) + new Vector3(0, 100, 0);
                            ButtonGroup.transform.position = PositionOfGroup;

                            if(GameManager.GetInstance().CurrentAgentGrenadeUsed())
                            {
                                ColorBlock buttonColor = GrenadeButton.colors;
                                buttonColor.normalColor = new Color(0, 0, 0, 0.5f);
                                GrenadeButton.colors = buttonColor;
                            }
                            else
                            {
                                ColorBlock buttonColor = GrenadeButton.colors;
                                buttonColor.normalColor = Color.white;
                                GrenadeButton.colors = buttonColor;
                            }
                        }
                        else
                        {
                            ButtonGroup.SetActive(false);
                        }
                    }
                    else
                    {
                        SelectingInteface.SetActive(false);
                    }
                    
                }
                else if (action == (int)GameManager.Action.MOVE)
                {
                    if(GameManager.GetInstance().MovePosition() != new Vector3(-1,-1,-1))
                    {
                        ConfirmButton.gameObject.SetActive(true);
                        Vector3 PositionOfConfirmButton = mainCamera.WorldToScreenPoint(GameManager.GetInstance().MovePosition()) + new Vector3(0, 100, 0);
                        ConfirmButton.transform.position = PositionOfConfirmButton;
                    }
                }
                else if (action == (int)GameManager.Action.OVERWATCH)
                {
                    ConfirmButton.gameObject.SetActive(true);
                    Vector3 PositionOfConfirmButton = mainCamera.WorldToScreenPoint(GameManager.GetInstance().CurrentAgentPosition()) + new Vector3(0, 200, 0);
                    ConfirmButton.transform.position = PositionOfConfirmButton;
                }
                else if (action == (int)GameManager.Action.THROW_GRENADE)
                {
                    if (GameManager.GetInstance().ThrowPositon() != new Vector3(-1, -1, -1))
                    {
                        ConfirmButton.gameObject.SetActive(true);
                        Vector3 PositionOfConfirmButton = mainCamera.WorldToScreenPoint(GameManager.GetInstance().ThrowPositon()) + new Vector3(0, 100, 0);
                        ConfirmButton.transform.position = PositionOfConfirmButton;
                    }
                    
                }

                Vector3 PositionOfImage = mainCamera.WorldToScreenPoint(GameManager.GetInstance().CurrentAgentPosition()) + new Vector3(0, -250, 0);
                MovementCountImage.transform.position = PositionOfImage;
            }
        }
    }
    
    //텍스트 띄우기
    public void ShowMessage(Transform target, string message)
    {
        Text showMessage = Instantiate(MessageText, PositionOnCanvas(target), Quaternion.identity);
        showMessage.transform.parent = Canvas.transform;
        showMessage.text = message;
        Destroy(showMessage, 1.0f);
    }

    //미션 종료
    public void MissionEndUI(bool mission)
    {
        SelectUI((int)GameManager.Action.NONE);

        if (mission)
            MissionSuccessful_Image.SetActive(true);
        else
            MissionFail_Imange.SetActive(true);
    }

    public void ShowAttackPanel(Agent attacker, int accuracy)
    {
        AttackPanel.text = "DMG : " + attacker.CurrentAgentStatus.ATK +"\n" + "ACC : " + accuracy;
    }

    public void ShowMovementcount(int movementcount)
    {
        MovementCountText.text = movementcount.ToString();
    }

    public bool CameraOnMove
    {
        get { return isCameraMoving; }
    }

    //화면 드래그
    public void MoveCamera(int action)
    {
        if (action != (int)GameManager.Action.GUNFIRE)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    Vector2 TouchPosition = Input.GetTouch(0).deltaPosition;

                    CameraHolder.transform.position += CameraHolder.forward * -TouchPosition.y * CameraMoveSpeed + CameraHolder.right * -TouchPosition.x * CameraMoveSpeed;
                }
            }
        }
    }
    
    public void CameraFollowAgent(Transform target)
    {
        CameraHolder.transform.position = new Vector3(target.transform.position.x - 4, transform.position.y, target.transform.position.z - 4);
    }

    //카메라가 따라갈 대상 설정
    public void MoveCameraTo(Transform target)
    {
        targetSight = target;

        SelectingInteface.SetActive(false);

        isCameraMoving = true;
    }

    //카메라 제자리 귀환
    public void CameraReposition()
    {
        targetSight = originalPosition;

        SelectingInteface.SetActive(false);

        isCameraMoving = true;
    }

    //카메라의 각도와 위치 변경
    public void CameraMovingToTarget()
    {
        mainCamera.transform.position = Vector3.Slerp(mainCamera.transform.position, targetSight.position, Time.deltaTime * 2f);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, Quaternion.LookRotation(targetSight.forward), Time.deltaTime * 2f);

        if (Vector3.Distance(mainCamera.transform.position, targetSight.position) < 3f && Quaternion.Angle(mainCamera.transform.rotation, targetSight.rotation) < 3f)
        {
            isCameraMoving = false;
        }
    }

    public Vector3 PositionOnCanvas(Transform target)
    {
        return mainCamera.WorldToScreenPoint(target.position);
    }

    // Use this for initialization
    void Start()
    {
        SelectingInteface.SetActive(false);
        AttackInterface.SetActive(false);
        MissionSuccessful_Image.SetActive(false);
        MissionFail_Imange.SetActive(false);
        isCameraMoving = false;
    }
}
