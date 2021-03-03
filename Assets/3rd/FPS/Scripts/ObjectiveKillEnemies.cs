using UnityEngine;

[RequireComponent(typeof(Objective))]
public class ObjectiveKillEnemies : MonoBehaviour
{
    [Tooltip("Chose whether you need to kill every enemies or only a minimum amount")]
    public bool mustKillAllEnemies = true;
    [Tooltip("If MustKillAllEnemies is false, this is the amount of enemy kills required")]
    public int killsToCompleteObjective = 5;
    [Tooltip("Start sending notification about remaining enemies when this amount of enemies is left")]
    public int notificationEnemiesRemainingThreshold = 3;

    EnemyManager m_EnemyManager;
    Objective m_Objective;
    int m_KillTotal;

    void Start()
    {
        m_Objective = GetComponent<Objective>();
        DebugUtility.HandleErrorIfNullGetComponent<Objective, ObjectiveKillEnemies>(m_Objective, this, gameObject);

        m_EnemyManager = FindObjectOfType<EnemyManager>();
        DebugUtility.HandleErrorIfNullFindObject<EnemyManager, ObjectiveKillEnemies>(m_EnemyManager, this);
        m_EnemyManager.onRemoveEnemy += OnKillEnemy;

        if (mustKillAllEnemies)
            killsToCompleteObjective = m_EnemyManager.numberOfEnemiesTotal;
        

        // set a title and description specific for this type of objective, if it hasn't one
        if (string.IsNullOrEmpty(m_Objective.title))
            m_Objective.title = "Eliminate " + (mustKillAllEnemies ? "all the" : killsToCompleteObjective.ToString()) + " enemies";

        if (string.IsNullOrEmpty(m_Objective.description))
            m_Objective.description = GetUpdatedCounterAmount();
    }

    void OnKillEnemy(EnemyController enemy, int remaining)
    {
        if (m_Objective.isCompleted)
            return;

        if (mustKillAllEnemies)
            killsToCompleteObjective = m_EnemyManager.numberOfEnemiesTotal;

        m_KillTotal = m_EnemyManager.numberOfEnemiesTotal - remaining;
        int targetRemaning = mustKillAllEnemies ? remaining : killsToCompleteObjective - m_KillTotal;

        // update the objective text according to how many enemies remain to kill
        if (targetRemaning == 0)
        {
            m_Objective.CompleteObjective(string.Empty, GetUpdatedCounterAmount(), "Objective complete : " + m_Objective.title);
        }
        else
        {

            if (targetRemaning == 1)
            {
                string notificationText = notificationEnemiesRemainingThreshold >= targetRemaning ? "One enemy left" : string.Empty;
                m_Objective.UpdateObjective(string.Empty, GetUpdatedCounterAmount(), notificationText);
            }
            else if (targetRemaning > 1)
            {
                // create a notification text if needed, if it stays empty, the notification will not be created
                string notificationText = notificationEnemiesRemainingThreshold >= targetRemaning ? targetRemaning + " enemies to kill left" : string.Empty;

                m_Objective.UpdateObjective(string.Empty, GetUpdatedCounterAmount(), notificationText);
            }
        }
    }

    string GetUpdatedCounterAmount()
    {
        return m_KillTotal + " / " + killsToCompleteObjective;
    }

}
