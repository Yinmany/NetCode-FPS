using System.Collections.Generic;
using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    [Tooltip("Enemies that will be assigned to this path on Start")]
    public List<EnemyController> enemiesToAssign = new List<EnemyController>();
    [Tooltip("The Nodes making up the path")]
    public List<Transform> pathNodes = new List<Transform>();

    private void Start()
    {
        foreach (var enemy in enemiesToAssign)
        {
            enemy.patrolPath = this;
        }
    }

    public float GetDistanceToNode(Vector3 origin, int destinationNodeIndex)
    {
        if(destinationNodeIndex < 0 || destinationNodeIndex >= pathNodes.Count || pathNodes[destinationNodeIndex] == null)
        {
            return -1f;
        }

        return (pathNodes[destinationNodeIndex].position - origin).magnitude;
    }

    public Vector3 GetPositionOfPathNode(int NodeIndex)
    {
        if (NodeIndex < 0 || NodeIndex >= pathNodes.Count || pathNodes[NodeIndex] == null)
        {
            return Vector3.zero;
        }

        return pathNodes[NodeIndex].position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < pathNodes.Count; i++)
        {
            int nextIndex = i + 1;
            if(nextIndex >= pathNodes.Count)
            {
                nextIndex -= pathNodes.Count;
            }

            Gizmos.DrawLine(pathNodes[i].position, pathNodes[nextIndex].position);
            Gizmos.DrawSphere(pathNodes[i].position, 0.1f);
        }
    }
}
