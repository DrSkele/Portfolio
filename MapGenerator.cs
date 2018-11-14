using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MapGenerator : MonoBehaviour {

    public Map[] maps;
    public int mapIndex;

    public Transform tilePrefab;
    public GameObject obstaclePrefab;
    
    public Transform MoveableBlockPrefab;

    [Range(0, 1)]
    public float outlinePercent;
    public float tileSize;

    List<Node> allTileCoords;
    Queue<Node> shuffledTileCoords;

    Map CurrentMap;
    
    public void GenerateMap()
    {
        CurrentMap = maps[mapIndex];
        System.Random prng = new System.Random(CurrentMap.seed);

        //좌표 생성
        allTileCoords = new List<Node>();
        for (int x = 0; x < CurrentMap.mapSize.x; x++)
        {
            for (int y = 0; y < CurrentMap.mapSize.y; y++)
            {
                allTileCoords.Add(new Node(x, y));
            }
        }
        shuffledTileCoords = new Queue<Node>(Utility.ShuffleArray(allTileCoords.ToArray(), CurrentMap.seed));

        //맵홀더 생성
        string holderName = "Generated Map";
        if (transform.Find(holderName))
        {
            DestroyImmediate(transform.Find(holderName).gameObject);
        }
        
        Transform mapHolder = new GameObject(holderName).transform;
        mapHolder.parent = this.transform;


        //타일 생성
        for (int x = 0; x < CurrentMap.mapSize.x; x++)
        {
            for (int y = 0; y < CurrentMap.mapSize.y; y++)
            {
                if (allTileCoords[x * (int)CurrentMap.mapSize.y + y].isWalkable == true)
                {
                    Vector3 tilePosition = Utility.NodeToPosition(x, y);
                    Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(Vector3.right * 90));
                    newTile.localScale = Vector3.one * (1 - outlinePercent) * tileSize;
                    newTile.parent = mapHolder;
                }
            }
        }

        //장애물 생성
        int obstacleCount = (int)(CurrentMap.mapSize.x * CurrentMap.mapSize.y * CurrentMap.obstaclePercent);
        int currentObstacleCount = 0;

        for (int i = 0; i < obstacleCount; i++)
        {
            Node randomCoord = GetRandomCoord();
            currentObstacleCount++;
            Node ObstacleCoord = allTileCoords[randomCoord.x * (int)CurrentMap.mapSize.y + randomCoord.z];
            ObstacleCoord.isWalkable = false;

            if (randomCoord != CurrentMap.mapCentre && MapIsFullyAccessible(currentObstacleCount))
            {
                float obstacleHeight;
                if (prng.Next(0,100) < 50)
                {
                    obstacleHeight = CurrentMap.maxObstacleHeight;
                    ObstacleCoord.Tag = (int)Node.NodeTag.HIGH_OBSTACLE;
                }
                else
                {
                    obstacleHeight = CurrentMap.minObstacleHeight;
                    ObstacleCoord.Tag = (int)Node.NodeTag.OBSTACLE;
                }

                Vector3 obstaclePosition = Utility.NodeToPosition(randomCoord.x, randomCoord.z);

                allTileCoords[randomCoord.x * (int)CurrentMap.mapSize.y + randomCoord.z] = ObstacleCoord;

                //장애물 생성
                GameObject newObstacle = Instantiate(obstaclePrefab, obstaclePosition + Vector3.up * obstacleHeight / 2, Quaternion.identity);
                newObstacle.transform.parent = mapHolder;
                newObstacle.transform.localScale = new Vector3(1, obstacleHeight, 1);
                newObstacle.GetComponent<BoxCollider>().size = new Vector3(1, 0.75f, 1);
                newObstacle.GetComponent<BoxCollider>().center = new Vector3(0, -0.125f, 0);

                //색상 바꾸기
                Renderer obstacleRenderer = newObstacle.GetComponent<Renderer>();
                Material obstacleMaterial = new Material(obstacleRenderer.sharedMaterial);
                float colourPercent = randomCoord.z / (float)CurrentMap.mapSize.y;
                obstacleMaterial.color = Color.Lerp(CurrentMap.foregroundColour, CurrentMap.backgroundColour, colourPercent);
                obstacleRenderer.sharedMaterial = obstacleMaterial;
            }
            else
            {
                ObstacleCoord.isWalkable = true;
                ObstacleCoord.Tag = (int)Node.NodeTag.TILE;
                currentObstacleCount--;
            }
        }
    }

    //장애물 생성 보조
    bool MapIsFullyAccessible(int currentObstacleCount)
    {
        bool[,] mapFlags = new bool[(int)CurrentMap.mapSize.x, (int)CurrentMap.mapSize.y];
        Queue<Node> queue = new Queue<Node>();
        queue.Enqueue(CurrentMap.mapCentre);
        mapFlags[CurrentMap.mapCentre.x, CurrentMap.mapCentre.z] = true;

        int accessibleTileCount = 1;

        while(queue.Count > 0)
        {
            Node tile = queue.Dequeue();
            for(int x = -1; x<= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int neighbourX = tile.x + x;
                    int neighbourY = tile.z + y;
                    if(x == 0 || y == 0)
                    {
                        if(neighbourX >= 0 && neighbourX < (int)CurrentMap.mapSize.x && neighbourY >= 0 && neighbourY < (int)CurrentMap.mapSize.y)
                        {
                            if(!mapFlags[neighbourX,neighbourY] && allTileCoords[neighbourX * (int)CurrentMap.mapSize.y + neighbourY].isWalkable)
                            {
                                mapFlags[neighbourX, neighbourY] = true;
                                queue.Enqueue(new Node(neighbourX, neighbourY));
                                accessibleTileCount++;
                            }
                        }
                    }
                }
            }
        }
        int targetAccessibleTileCount = (int)(CurrentMap.mapSize.x * CurrentMap.mapSize.y - currentObstacleCount);
        return targetAccessibleTileCount == accessibleTileCount;
    }

    //타일들의 상태 변경
    public void SetTileState(Agent CurrentAgent)
    {
        Node agentPosition;
        List<Agent> ally = GameManager.GetInstance().AllyAgent;
        List<Agent> enemy = GameManager.GetInstance().EnemyAgent;

        for(int i = 0; i< allTileCoords.Count; i++)
        {
            if (allTileCoords[i].Tag == (int)Node.NodeTag.AGENT)
            {
                allTileCoords[i].Tag = (int)Node.NodeTag.TILE;
                allTileCoords[i].isWalkable = true;
            }
        }

        for (int i = 0; i < ally.Count; i++)
        {
            if (ally[i] != CurrentAgent)
            {
                agentPosition = Utility.PositionToNode(ally[i].transform.position);
                allTileCoords[agentPosition.x * (int)CurrentMap.mapSize.y + agentPosition.z].isWalkable = false;
                allTileCoords[agentPosition.x * (int)CurrentMap.mapSize.y + agentPosition.z].Tag = (int)Node.NodeTag.AGENT;
            }
        }
        for (int i = 0; i < enemy.Count; i++)
        {
            if (enemy[i] != CurrentAgent)
            {
                agentPosition = Utility.PositionToNode(enemy[i].transform.position);
                allTileCoords[agentPosition.x * (int)CurrentMap.mapSize.y + agentPosition.z].isWalkable = false;
                allTileCoords[agentPosition.x * (int)CurrentMap.mapSize.y + agentPosition.z].Tag = (int)Node.NodeTag.AGENT;
            }
        }
    }

    //현재 입력된 캐릭터가 움직일 수 있는 공간 반환
    public List<Vector3> MovableSpace(Agent agent)
    {
        int agentMove = agent.agentStatus.MOVE;
        bool[,] mapFlags = new bool[(int)CurrentMap.mapSize.x , (int)CurrentMap.mapSize.y];
        Queue<Node> queue = new Queue<Node>();
        List<Vector3> moveable = new List<Vector3>();
        int moveCount = 0;

        SetTileState(agent);
        
        queue.Enqueue(Utility.PositionToNode(agent.transform.position));
        int count = queue.Count;

        while (moveCount <= agentMove)
        {
            while(count > 0)
            {
                Node tile = queue.Dequeue();
                moveable.Add(Utility.NodeToPosition(tile));

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        int neighbourX = tile.x + x;
                        int neighbourY = tile.z + y;
                        if (x == 0 || y == 0)
                        {
                            if (neighbourX >= 0 && neighbourX < (int)CurrentMap.mapSize.x && neighbourY >= 0 && neighbourY < (int)CurrentMap.mapSize.y)
                            {
                                if (!mapFlags[neighbourX, neighbourY] && allTileCoords[neighbourX * (int)CurrentMap.mapSize.y + neighbourY].isWalkable)
                                {
                                    mapFlags[neighbourX, neighbourY] = true;
                                    queue.Enqueue(new Node(neighbourX, neighbourY));
                                }
                            }
                        }
                    }
                }
                count--;
            }
            count = queue.Count;
            moveCount++;
        }
        return moveable;
    }

    //움직일 수 있는 범위를 표시
    public void ShowMoveableSpace(Agent SelectedAgent)
    {
        string holderName = "Instance Block";
        if (transform.Find(holderName))
        {
                DestroyImmediate(transform.Find(holderName).gameObject);
        }
        Transform BlockHolder = new GameObject(holderName).transform;
        BlockHolder.parent = this.transform;

        List<Vector3> moveSpace = MovableSpace(SelectedAgent);

        for (int i = 0; i < moveSpace.Count; i++)
        {
            Transform newMoveableBlock = Instantiate(MoveableBlockPrefab, moveSpace[i], Quaternion.identity);
            newMoveableBlock.parent = BlockHolder;
        }
    }

    //범위 표시 제거
    public void RemoveInstanceBlock()
    {
        string holderName = "Instance Block";
        if (transform.Find(holderName))
        {
            DestroyImmediate(transform.Find(holderName).gameObject);
        }
    }

    //입력된 위치의 주변 타일을 반환
    public List<Node> AdjacentNode(Vector3 target)
    {
        Node targetNode = Utility.PositionToNode(target);
        List<Node> nodeReturn = new List<Node>();
        for(int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if((x == 0 || z == 0) && !(x == 0 && z == 0))
                {
                    if(targetNode.x + x < CurrentMap.mapSize.x && targetNode.x + x >= 0 && targetNode.z + z < CurrentMap.mapSize.y && targetNode.z + z >= 0)
                        nodeReturn.Add(allTileCoords[(targetNode.x + x) * (int)CurrentMap.mapSize.x + targetNode.z + z]);
                }
            }
        }
        return nodeReturn;
    }
    
    //적 캐릭터가 움직일 최적의 위치 계산
    public Node BestPosition(Agent CurrentAgent, Vector3 TargetAgentPosition)
    {
        List<Node> SpaceToMove = Utility.PositionToNode(MovableSpace(CurrentAgent));
        Node bestPosition;

        Vector3 direction = new Vector3();
        float onX = TargetAgentPosition.x - CurrentAgent.transform.position.x;
        float onZ = TargetAgentPosition.z - CurrentAgent.transform.position.z;

        if (SpaceToMove.Count != 0)
        {
            if (Mathf.Abs(onX) > Mathf.Abs(onZ))
            {
                if (onX > 0)
                    direction += new Vector3(1, 0, 0);
                else
                    direction += new Vector3(-1, 0, 0);
            }
            else
            {
                if (onZ > 0)
                    direction += new Vector3(0, 0, 1);
                else
                    direction += new Vector3(0, 0, -1);
            }

            for (int i = 0; i < SpaceToMove.Count; i++)
            {
                Node nodeAt = allTileCoords[(int)(SpaceToMove[i].x + direction.x) * (int)CurrentMap.mapSize.x
                    + (int)(SpaceToMove[i].z + direction.z)];
                
                float distance = Vector3.Distance(Utility.NodeToPosition(nodeAt), TargetAgentPosition);

                if (distance > CurrentAgent.CurrentAgentStatus.SIGHT * 0.5f)
                {
                    if (nodeAt.Tag == (int)Node.NodeTag.HIGH_OBSTACLE)
                        SpaceToMove[i].normalCost += 0.5f;
                    else if (nodeAt.Tag == (int)Node.NodeTag.OBSTACLE)
                        SpaceToMove[i].normalCost += 0.25f;

                    SpaceToMove[i].normalCost -= distance * 0.2f;
                }
                else
                {
                    if (nodeAt.Tag == (int)Node.NodeTag.HIGH_OBSTACLE)
                        SpaceToMove[i].normalCost += 1f;
                    else if (nodeAt.Tag == (int)Node.NodeTag.OBSTACLE)
                        SpaceToMove[i].normalCost += 0.5f;

                    SpaceToMove[i].normalCost += distance * 0.2f;
                }
            }

            bestPosition = SpaceToMove[0];

            for (int i = 0; i < SpaceToMove.Count; i++)
            {
                if(SpaceToMove[i].normalCost > bestPosition.normalCost)
                    bestPosition = SpaceToMove[i];
            }
        }
        else
            bestPosition = Utility.PositionToNode(CurrentAgent.transform.position);

        return bestPosition;
    }

    //폭발로 엄폐물들의 상태를 변경
    public void InExplosionArea(GameObject objectInArea)
    {
        Node positionNode = Utility.PositionToNode(objectInArea.transform.position);

        if (objectInArea.tag == "Obstacle")
        {
            GameManager.GetInstance().EffectOnDestroy(objectInArea.transform, Color.black);

            if (allTileCoords[positionNode.x * (int)CurrentMap.mapSize.y + positionNode.z].Tag == (int)Node.NodeTag.OBSTACLE)
            {
                Destroy(objectInArea);
                allTileCoords[positionNode.x * (int)CurrentMap.mapSize.y + positionNode.z].Tag = (int)Node.NodeTag.TILE;
                allTileCoords[positionNode.x * (int)CurrentMap.mapSize.y + positionNode.z].isWalkable = true;
            }
            else if (allTileCoords[positionNode.x * (int)CurrentMap.mapSize.y + positionNode.z].Tag == (int)Node.NodeTag.HIGH_OBSTACLE)
            {
                objectInArea.transform.localScale = new Vector3(1, 1f, 1);
                objectInArea.transform.position = objectInArea.transform.position + new Vector3(0, -0.5f, 0);
                allTileCoords[positionNode.x * (int)CurrentMap.mapSize.y + positionNode.z].Tag = (int)Node.NodeTag.OBSTACLE;
            }
        }
    }

    public Node GetRandomCoord()
    {
        Node randomCoord = shuffledTileCoords.Dequeue();
        shuffledTileCoords.Enqueue(randomCoord);
        return randomCoord;
    }

    [System.Serializable]
    public class Map
    {
        public Vector2 mapSize;
        [Range(0,1)]
        public float obstaclePercent;
        public int seed;
        public float minObstacleHeight;
        public float maxObstacleHeight;
        public Color foregroundColour;
        public Color backgroundColour;

        public Node mapCentre
        {
            get{ return new Node((int)mapSize.x / 2, (int)mapSize.y / 2); }
        }
    }

    //PathFinder
    List<Node> openList = new List<Node>(); //이동 가능한 노드들의 집합
    List<Node> closedList = new List<Node>(); //이미 지나온 노드들의 집합
    public List<Node> agentPath = new List<Node>();

    public bool SearchListForEqual(List<Node> nodeList, Node node)
    {
        for (int i = 0; i < nodeList.Count; i++)
        {
            if (nodeList[i].EqualNode(node))
                return true;
        }
        return false;
    }

    public void PathGenerate(Node destination)
    {
        agentPath.Clear();
        Node pathNode = destination;
        while (pathNode.previousNode != null)
        {
            agentPath.Add(pathNode);
            pathNode = pathNode.previousNode;
        }
    }

    //길찾기에서의 각 위치의 점수 계산
    public List<Node> FindMovableNeighbor(Node parent)
    {
        List<Node> NeighborNode = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (parent.x + x < 0 || parent.x + x > CurrentMap.mapSize.x - 1)
                    continue;
                if (parent.z + z < 0 || parent.z + z > CurrentMap.mapSize.y - 1)
                    continue;

                if (x == 0 && z == 0)
                    continue;

                if (x != 0 || z != 0)//대각선 블록
                { //옆에 블록이 있을경우 대각선으로 움직일 수 없으므로 넘긴다
                    if (!allTileCoords[(parent.x + x) * (int)CurrentMap.mapSize.x + parent.z].isWalkable)
                        continue;
                    if (!allTileCoords[parent.x * (int)CurrentMap.mapSize.x + parent.z + z].isWalkable)
                        continue;
                }
                if (!allTileCoords[(parent.x + x)* (int)CurrentMap.mapSize.x + parent.z + z].isWalkable)//장애물일경우 지나갈 수 없으므로 넘긴다
                    continue;

                Node addedNode = new Node(parent.x + x, parent.z + z);
                
                if(x == 0 || z ==0)
                    addedNode.Gcost = parent.Gcost + 1f;
                else
                    addedNode.Gcost = parent.Gcost + 1.4f;

                addedNode.previousNode = parent;

                NeighborNode.Add(addedNode);
            }
        }

        return NeighborNode;
    }

    //길찾기 알고리즘
    public bool FindPath(Node begin, Node destination)
    {
        openList.Clear();
        closedList.Clear();

        Node CurrentPosition = new Node(begin.x, begin.z);
        CurrentPosition.Gcost = 0;
        CurrentPosition.Hcost = Mathf.Abs(CurrentPosition.x - destination.x) + Mathf.Abs(CurrentPosition.z - destination.z);
        openList.Add(CurrentPosition);
        
        while (openList != null)
        {
            Node OptimalNode = openList[0];
            int OptimalNodeNumber = 0;

            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].Fcost < OptimalNode.Fcost)
                {
                    OptimalNode = openList[i];
                    OptimalNodeNumber = i;
                }
            }
            CurrentPosition = OptimalNode;

            if (CurrentPosition.EqualNode(destination))
            {
                PathGenerate(CurrentPosition);
                return true;
            }

            openList.Remove(openList[OptimalNodeNumber]);
            closedList.Add(CurrentPosition);
            
            foreach(Node neighbor in FindMovableNeighbor(CurrentPosition))
            { 
                if (!SearchListForEqual(closedList, neighbor)) //고정된 리스트에 선택한 블록이 없을 때 실행
                {
                    neighbor.Hcost = Mathf.Abs(neighbor.x - destination.x) + Mathf.Abs(neighbor.z - destination.z);

                    if (!SearchListForEqual(openList, neighbor))//오픈리스트에 없다면
                        openList.Add(neighbor); //이동 가능한 노드를 오픈리스트에 추가
                    else
                    {
                        Node openNeighbor = openList.Find(item => item.EqualNode(neighbor));
                        if(neighbor.Gcost < openNeighbor.Gcost)
                        {
                            openNeighbor.Gcost = neighbor.Gcost;
                            openNeighbor.previousNode = neighbor.previousNode;
                        }
                    }
                }
            }
        }
        return false;
    }
    
    void Start () {
        GenerateMap();
	}
    
}

public class Node
{
    public enum NodeTag { TILE, OBSTACLE, HIGH_OBSTACLE, AGENT };

    private int m_pointx;
    private int m_pointz;

    private bool m_walkable;
    private int m_tag;

    private float m_gcost;
    private float m_hcost;
    private float cost;

    private Node m_previousNode;

    public Node(int x, int z)
    {
        m_pointx = x;
        m_pointz = z;

        m_gcost = Mathf.Infinity;
        m_hcost = Mathf.Infinity;
        m_walkable = true;
        m_previousNode = null;
    }

    public int x
    {
        get { return m_pointx; }
    }

    public int z
    {
        get { return m_pointz; }
    }

    public bool isWalkable
    {
        set { m_walkable = value; }
        get { return m_walkable; }
    }

    public int Tag
    {
        set { m_tag = value; }
        get { return m_tag; }
    }

    public float Gcost
    {
        get { return m_gcost; }
        set { m_gcost = value; }
    }

    public float Hcost
    {
        get { return m_hcost; }
        set { m_hcost = value; }
    }

    public float Fcost
    {
        get { return m_gcost + m_hcost; }
    }

    public float normalCost
    {
        get { return cost; }
        set { cost = value; }
    }

    public Node previousNode
    {
        get { return m_previousNode; }
        set { m_previousNode = value; }
    }

    public bool EqualNode(int x, int z)
    {
        if (m_pointx == x && m_pointz == z)
            return true;
        else
            return false;
    }

    public bool EqualNode(Node node)
    {
        if (this.x == node.x && this.z == node.z)
            return true;
        else
            return false;
    }
}
