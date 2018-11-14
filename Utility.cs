using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utility : MonoBehaviour
{
    //맵 랜덤 생성용
    public static T[] ShuffleArray<T>(T[] array, int seed)
    {
        System.Random prng = new System.Random(seed);

        for(int i = 0; i< array.Length - 1; i++)
        {
            int randomIndex = prng.Next(i, array.Length);
            T tempItem = array[randomIndex];
            array[randomIndex] = array[i];
            array[i] = tempItem;
        }

        return array;
    }

    public static Node PositionToNode(Vector3 position)
    {
        return new Node(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
    }

    public static List<Node> PositionToNode(List<Vector3> position)
    {
        List<Node> returnList = new List<Node>();

        for(int i = 0; i < position.Count; i++)
        {
            returnList.Add(new Node(Mathf.RoundToInt(position[i].x), Mathf.RoundToInt(position[i].z)));
        }

        return returnList;
    }

    public static Vector3 NodeToPosition(int x, int y)
    {
        return new Vector3(x, 0, y);
    }

    public static Vector3 NodeToPosition(Node node)
    {
        return new Vector3(node.x, 0, node.z);
    }

    public static Vector3 PointOnGrid(Vector3 point)
    {
        int GridX = Mathf.RoundToInt(point.x);
        //int GridY = Mathf.RoundToInt(point.z);
        int GridZ = Mathf.RoundToInt(point.z);

        Vector3 GridPoint = new Vector3(GridX, 0, GridZ);

        return GridPoint;
    }

    public static bool CompareTwoVector(Vector3 one, Vector3 two, float distance)
    {
        if(Mathf.Abs(one.x - two.x) < distance && Mathf.Abs(one.y - two.y) < distance && Mathf.Abs(one.z - two.z) < distance)
        {
            return true;
        }
        return false;
    }
}
