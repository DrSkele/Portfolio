using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[System.Serializable]
public class CharacterStatus
{
    int m_HP; // 체력
    int m_ATK; // 공격력
    int m_ACC; //명중률
    int m_MOVE; //이동력
    int m_SIGHT; //시야

    public CharacterStatus(int hp, int atk, int acc, int move, int sight)
    {
        m_HP = hp;
        m_ATK = atk;
        m_ACC = acc;
        m_MOVE = move;
        m_SIGHT = sight;
    }
    
    public int HP
    {
        set { m_HP = value; }
        get { return m_HP; }
    }
    public int ATK
    {
        set { m_ATK = value; }
        get { return m_ATK; }
    }
    public int ACC
    {
        set { m_ACC = value; }
        get { return m_ACC; }
    }
    public int MOVE
    {
        set { m_MOVE = value; }
        get { return m_MOVE; }
    }
    public int SIGHT
    {
        set { m_SIGHT = value; }
        get { return m_SIGHT; }
    }
}

public class Agent : MonoBehaviour {
    
    public CharacterStatus agentStatus; //캐릭터의 원래 상태
    public CharacterStatus CurrentAgentStatus; //캐릭터의 현재 상태
    public int movecount; //현재 가진 이동력

    public GameObject AgentModel; //몸통모델
    public GameObject AgentSelected; //선택표시
    public Bullet AgentBullet; //Prefab 총알
    public Grenade AgentGrenade; //Prefab 유탄
    public Transform AgentGun; //총모델
    public Transform GrenadeLauncher; //유탈 발사 위치
    public Transform GunFirePosition; //총 발사 위치

    public AgentHealthBar HealthBar;

    public enum Side { ALLY, ENEMY };// 적 아군 구분
    public enum Specialty { SOLDIER, SNIPER, GRANADIER};
    int sideOn;
    int specialtyOn;

    bool isMoving; //움직이고 있는지 여부 확인용
    float previousDiff; //이동 버그 체크용 변수
    bool onOverwatch; //경계스킬 발동 여부 확인용

    bool hasAttacked; //공격을 수행했는지 여부 확인용
    bool grenadeUsed; //유탄을 이미 사용했는지 여부 확인용
    int targetIndex; //현재 타겟의 인덱스
    List<Agent> targetInSight = new List<Agent>(); //시야 안의 적 저장

    int pathIndex; //현재 목표경로 인덱스
    List<Vector3> path = new List<Vector3>(); //이동 경로 저장용
    
    public Camera AgentView; //카메라의 위치
    
	void Start () {
        isMoving = false;
        onOverwatch = false;
        hasAttacked = false;
        grenadeUsed = false;
    }
	
    //이동 수행
    public void AgentOnMove()
    {
        if (pathIndex < path.Count)//아직 이동경로가 남아있다면 실행
        {
            transform.position = transform.position + ((path[pathIndex] - path[pathIndex - 1]).normalized * 2f * Time.deltaTime);
            float angle = Vector3.SignedAngle(AgentModel.transform.forward, (path[pathIndex] - path[pathIndex - 1]), AgentModel.transform.position);
            AgentModel.transform.Rotate(new Vector3(0, angle, 0) * 2f * Time.deltaTime);

            if (IsAtPosition(path[pathIndex]))//목표위치에 도달했다면 실행
            {
                pathIndex++;
                if(pathIndex < path.Count)//마지막 경로에서의 버그 방지용 조건문
                    previousDiff = (transform.position - path[pathIndex]).magnitude;
            }
        }
        else//이동경로가 없으면 실행
            isMoving = false;
    }
    
    //스텟 초기화
    public void SetAgentStatus(CharacterStatus status)
    {
        agentStatus = status;
        CurrentAgentStatus = agentStatus;
    }
    
    //매턴 행동 초기화
    public void AgentInitialize()
    {
        movecount = 2;
        hasAttacked = false;
        onOverwatch = false;
    }

    //GM : 이동경로 설정 및 초기화
    public void MoveAgentOnGrid(List<Node> nodePath)
    {
        isMoving = true;
        path.Clear();
        pathIndex = 1;
        for (int i = nodePath.Count; i >= 0; i--)
        {
            if (i != nodePath.Count)
                path.Add(Utility.NodeToPosition(nodePath[i]));
            else
                path.Add(new Vector3(transform.position.x, 0, transform.position.z));
        }
        if(path.Count > 1)
            previousDiff = (transform.position - path[pathIndex]).magnitude;
    }

    //현재 위치에서 입력받은 대상이 보이는지 확인
    public bool EnemyInSight(List<Agent> TargetAgent)
    {
        targetInSight.Clear();
        bool[] TargetFlag = new bool[TargetAgent.Count];

        for (int i = 0; i < TargetAgent.Count; i++)
        {
            TargetFlag[i] = false;
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 || z == 0)
                    {
                        if (TargetAgent.Count > 0)
                        {
                            Vector3 LookPoint = transform.position + new Vector3(x * 0.05f, 0.01f, z * 0.05f); //시야 보정
                            
                            RaycastHit hitInfo;
                            Ray sight = new Ray(LookPoint, TargetAgent[i].transform.position - LookPoint);
                            Debug.DrawRay(LookPoint, TargetAgent[i].transform.position - LookPoint, Color.green, 3f);
                            if (Physics.Raycast(sight, out hitInfo, agentStatus.SIGHT))
                            {
                                if (TargetFlag[i] != true && TargetAgent.Contains(hitInfo.transform.GetComponent<Agent>()))
                                {
                                    TargetFlag[i] = true;
                                    targetInSight.Add(TargetAgent[i]);
                                }
                            }
                        }
                    }
                }
            }
        }
        targetIndex = 0;

        if (targetInSight.Count > 0)//적이 보인다면 true
            return true;
        return false;//없다면 false
    }

    //이동 시 목표지점에 도달했는지 확인하는 함수
    public bool IsAtPosition(Vector3 point)
    {
        Vector3 diff = transform.position - point;
        if (Mathf.Abs(diff.x) < 0.03f && Mathf.Abs(diff.z) < 0.03f) //오차범위 내에 이동
        {
            return true;
        }
        else if(diff.magnitude > previousDiff) //이동버그(무한이동)체크용 : 목표지점에서 멀어지고 있다면 멈춘다
        {
            return true;
        }
        previousDiff = diff.magnitude;
        return false;
    }

    //GM : 총 발사 함수
    public bool GunFire(Agent Enemy, int accuracy)
    {
        System.Random prng = new System.Random();
        hasAttacked = true;
        onOverwatch = false;

        Bullet shootingBullet;

        if (prng.Next(0,100) <= accuracy)//명중률 안이라면 수행
        {
            for (int i = 0; i < 3; i++)
            {
                shootingBullet = Instantiate(AgentBullet, GunFirePosition.position, GunFirePosition.rotation);
                shootingBullet.transform.Rotate(new Vector3(prng.Next(-1, 1), prng.Next(-1, 1), 0));
            }
            Enemy.TakeDamage(this.CurrentAgentStatus.ATK);
            return true;
        }

        for (int i = 0; i < 3; i++)
        {
            shootingBullet = Instantiate(AgentBullet, GunFirePosition.position, GunFirePosition.rotation);
            shootingBullet.transform.Rotate(new Vector3(prng.Next(4, 5), prng.Next(-5, 5), 0)); //탄퍼짐
        }
        return false;
    }

    //GM : 경계스킬 사용
    public void Overwatch()
    {
        onOverwatch = true;
        hasAttacked = true;
    }

    //GM : 유탄 발사
    public void ThrowGrenade(Vector3 targetSpot)
    {
        Grenade throwingGrenade = Instantiate(AgentGrenade, GrenadeLauncher.position, GrenadeLauncher.rotation);
        throwingGrenade.Launch(targetSpot);
        grenadeUsed = true;
    }

    public void SetAgentHealthBar()
    {
        HealthBar.SetHealthBar(CurrentAgentStatus.HP);
    }

    public int isOnSide
    {
        get { return sideOn; }
        set { sideOn = value; }
    }

    public int AgentSpecialty
    {
        get { return specialtyOn; }
        set { specialtyOn = value; }
    }

    public void isSelected(bool selected)
    {
        AgentSelected.SetActive(selected);
    }

    public List<Agent> TargetInSight
    {
        get { return targetInSight; }
    }

    public int TargetIndex
    {
        get { return targetIndex; }
        set { targetIndex = value; }
    }

    public bool IsOnOverwatch
    {
        get { return onOverwatch; }
    }

    public bool IsMoving
    {
        get { return isMoving; }
    }

    public bool HasAttacked
    {
        get { return hasAttacked; }
    }

    public bool GrenadeUsed
    {
        get { return grenadeUsed; }
    }

    public bool IsDead()
    {
        if (this.CurrentAgentStatus.HP <= 0)
            return true;
        else
            return false;
    }

    public void TakeDamage(int damage)
    {
        this.CurrentAgentStatus.HP -= damage;
    }


}
