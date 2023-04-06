using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;


// [ExecuteInEditMode]
public class Sheep : MonoBehaviour
{
    [System.Serializable]
    public struct Status
    {
        private static string[] idToName = {"Idle", "Search food", "Want lie down", "Escape", "Fight", "Eat", "Lie down"};
        private int idValue;
        
        public int id;
        // {
        //     get { return idValue; }
        //     set
        //     {
        //         if (!(value >= 0 && value < idToName.Length))
        //         {
        //             throw new Exception("Недопустимый id статуса овцы");
        //         }
        //         idValue = value;
        //     }
        // }
        public string name
        {
            get { return idToName[id]; }
            private set { name = value; }
        }
    }

    public struct BehaviourCoefficients
    {
        public float hungerSpeed;
        public float eatSpeed;
        public float fatigueSpeed;
        public float restSpeed;
        public float visionDistance;
        public float fightDistance;
        public float tendencyToIdle;
        public float tendencyToEat;
        public float tendencyToLieDown;
        public float tendencyToEscape;
        public float tendencyToFight;
        public float tendencyToContinueFighting;
        public float tendencyToStayInStateWithTime;
        public float tendencyToStayInState;
        public float tendencyToStartEating;
        public float tendencyToStartLyingDown;
        public float tendencyToContinueEating;
        public float tendencyToContinueLyingDown;
        public float optimalRankDiffToFight;
        public float[] typicalTimeStaying;
        public float herdInstinct;
        public float minDistanceToEat;
        public float hungerForUnstoppableEating;
        public float fatigueForUnstoppableLying;

        [System.Serializable]
        public struct MinMaxDistribution
        {
            public float min;
            public float max;
            public float center;
        }

        [System.Serializable]
        public struct MinMaxBehaviourCoefficients
        {
            public MinMaxDistribution hungerSpeed;
            public MinMaxDistribution eatSpeed;
            public MinMaxDistribution fatigueSpeed;
            public MinMaxDistribution restSpeed;
            public MinMaxDistribution visionDistance;
            public MinMaxDistribution fightDistance;
            public MinMaxDistribution tendencyToIdle;
            public MinMaxDistribution tendencyToEat;
            public MinMaxDistribution tendencyToLieDown;
            public MinMaxDistribution tendencyToEscape;
            public MinMaxDistribution tendencyToFight;
            public MinMaxDistribution tendencyToContinueFighting;
            public MinMaxDistribution tendencyToStayInStateWithTime;
            public MinMaxDistribution tendencyToStayInState;
            public MinMaxDistribution tendencyToStartEating;
            public MinMaxDistribution tendencyToStartLyingDown;
            public MinMaxDistribution tendencyToContinueEating;
            public MinMaxDistribution tendencyToContinueLyingDown;
            public MinMaxDistribution optimalRankDiffToFight;
            public MinMaxDistribution[] typicalTimeStaying;
            public MinMaxDistribution herdInstinct;
            public MinMaxDistribution minDistanceToEat;
            public MinMaxDistribution hungerForUnstoppableEating;
            public MinMaxDistribution fatigueForUnstoppableLying;
        }
    }
    
    [SerializeField]
    public Status status = new Status();
    public float hunger = 0.0f;
    public float fatigue = 0.0f;
    public float rank;
    public BehaviourCoefficients coefficients;
    private Sheep[] sheepsInVision = new Sheep[0];
    private EatZone[] foodInVision = new EatZone[0];
    private SleepZone[] sleepInVision = new SleepZone[0];
    private Sheep[] sheepsInFightDistance = new Sheep[0];
    private float timeInCurrentState = 0.0f;
    private float timeOfLastTransition = 0.0f;
    private float stressSoundVolume;
    private Sheep sheepToFight;
    private Vector3 targetPosition;
    private bool hasTargetPosition = false;
    private Vector3 direction;
    private float speed;
    private float[] tendencyForTransition = new float[7];
    private float[] probabilitiesForTransition = new float[7];

    // Used to limit status update rate 
    private float behaviorUpdateTime;
    private float behaviorUpdatedAt;

    private float randomBehaviorProbability;
    private float timeWalkInDirection = 0.0f;

    private void UpdateVision()
    {
        sheepsInVision = Array.ConvertAll(Physics.OverlapSphere(transform.position, coefficients.visionDistance, (1 << 6)), 
            item => item.gameObject.GetComponent<Sheep>());
        foodInVision = Array.ConvertAll(Physics.OverlapSphere(transform.position, coefficients.visionDistance, (1 << 7)), 
            item => item.gameObject.GetComponent<EatZone>());
        sleepInVision = Array.ConvertAll(Physics.OverlapSphere(transform.position, coefficients.visionDistance, (1 << 8)), 
            item => item.gameObject.GetComponent<SleepZone>());
        sheepsInFightDistance = Array.ConvertAll(Physics.OverlapSphere(transform.position, coefficients.fightDistance, (1 << 6)), 
            item => item.gameObject.GetComponent<Sheep>());
    }

    private float GetProbabilityToStayInState(float typicalTime)
    {
        // return MathF.Exp(-timeInCurrentState / typicalTime);
        if (timeInCurrentState < typicalTime)
        {
            return MathF.Pow((typicalTime - timeInCurrentState) / typicalTime, 2.0f);
        } else
        {
            return 0.0f;
        }
    }

    private float GetProbabilityToLieDown()
    {
        return fatigue;
    }

    private float GetProbabilityToFight(float rankDifference)
    {
        if (rankDifference <= 0 || rankDifference >= coefficients.optimalRankDiffToFight * 2)
        {
            return 0.0f;
        }
        return MathF.Exp(-MathF.Pow(rankDifference - coefficients.optimalRankDiffToFight, 2.0f) / 
                         (MathF.Pow(coefficients.optimalRankDiffToFight, 2.0f) - MathF.Pow(rankDifference - coefficients.optimalRankDiffToFight, 2.0f)));
    }

    private float GetHerdInstinct(Sheep sheep)
    {
        float distance = (sheep.transform.position - transform.position).magnitude;
        float rankDifference = sheep.rank - rank;
        return 1.0f;
    }

    private float[] ProbabilitiesFromTendencies(float[] tendencies)
    {
        float[] probabilities = new float[7];
        float sum = 0.0f;
        for (int i = 0; i < tendencies.Length; ++i)
        {
            sum += tendencies[i];
        }
        for (int i = 0; i < tendencies.Length; ++i)
        {
            probabilities[i] = tendencies[i] / sum;
        }
        return probabilities;
    }

    private int ChooseWithProbabilities(float[] probabilities)
    {
        float value = UnityEngine.Random.Range(0.0f, 1.0f);
        float startValue = value;
        for (int i = 0; i < probabilities.Length; ++i)
        {
            value -= probabilities[i];
            if (value < 0) // It means value > sum(prob, 0, i-1) and value < sum(prob, 0, i)
            {
                // Debug.Log(startValue);
                return i;
            }
        }
        for (int i = probabilities.Length - 1; i >= 0 ; --i)
        {
            if (probabilities[i] > 0) // If sum(prob) is not enough, return last non-zero state
            {
                return i;
            }
        }
        float sum = 0.0f;
        for (int i = 0; i < probabilities.Length; ++i)
        {
            sum += probabilities[i];
        }
        throw new Exception(string.Format("Неудалось совершить вероятностный переход. Значение для перехода {0}, сумма вероятностей {1}", startValue, sum));
    }

    private int ChooseMax(float[] probabilities)
    {
        int maxI = 0;
        for (int i = 1; i < probabilities.Length; ++i)
        {
            if (probabilities[i] > probabilities[maxI])
            {
                maxI = i;
            }
        }
        return maxI;
    }

    private bool ChangeStatus(int newStatus, float[] tendencies = null)
    {
        if (newStatus != status.id)
        {
            if (tendencies == null)
            {
                Debug.Log(string.Format("{0}: {1} -> {2}", gameObject.name, status.id, newStatus));
            } else
            {
                string resText = "";
                for (int i = 0; i < tendencies.Length; ++i)
                {
                    resText += tendencies[i].ToString("0.000") + " ";
                }
                Debug.Log(string.Format("{0}: {1} -> {2} based on |{3}|", gameObject.name, status.id, newStatus, resText));
            }
            timeOfLastTransition = Time.time;
            status.id = newStatus;
            hasTargetPosition = false;
            sheepToFight = null;
            return true;
        } else 
        {
            return false;
        }
    }

    private void UpdateState()
    {
        UpdateVision();
        tendencyForTransition = new float[7];

        tendencyForTransition[0] = coefficients.tendencyToIdle;
        tendencyForTransition[1] = hunger * coefficients.tendencyToEat;
        tendencyForTransition[2] = GetProbabilityToLieDown() * coefficients.tendencyToLieDown;
        tendencyForTransition[3] = stressSoundVolume * coefficients.tendencyToEscape;
        float maxProbabilityToFight = 0.0f;
        foreach (var sheep in sheepsInFightDistance)
        {
            if (GetProbabilityToFight(sheep.rank - rank) > maxProbabilityToFight)
            {
                maxProbabilityToFight = GetProbabilityToFight(sheep.rank - rank);
                sheepToFight = sheep;
            }
        }
        tendencyForTransition[4] = maxProbabilityToFight * coefficients.tendencyToFight;
        foreach (var sheep in sheepsInVision)
        {
            if ((status.id <= 2 || status.id >= 5) && sheep.status.id <= 2)
            {
                tendencyForTransition[sheep.status.id] += GetHerdInstinct(sheep) * coefficients.herdInstinct / sheepsInVision.Length / 3.0f;
            }
            if (sheep.status.id < 5)
            {
                tendencyForTransition[sheep.status.id] += GetHerdInstinct(sheep) * coefficients.herdInstinct / sheepsInVision.Length;
            } else if (status.id < 5 && status.id > 2)
            {
                tendencyForTransition[sheep.status.id - 4] += GetHerdInstinct(sheep) * coefficients.herdInstinct / sheepsInVision.Length / 3.0f;
            }
        }

        tendencyForTransition[status.id] += GetProbabilityToStayInState(coefficients.typicalTimeStaying[status.id]) * 
            coefficients.tendencyToStayInStateWithTime + coefficients.tendencyToStayInState;

        if (status.id == 0)
        {
        } else if (status.id == 1)
        {
            foreach (var eatZone in foodInVision)
            {
                if (eatZone.IsInside(transform.position))
                {
                    tendencyForTransition[5] += hunger * coefficients.tendencyToStartEating / 2.0f;
                    break;
                }
            }
            if (hasTargetPosition)
            {
                if ((transform.position - targetPosition).magnitude < coefficients.minDistanceToEat)
                {
                    tendencyForTransition[5] += hunger * coefficients.tendencyToStartEating / 2.0f;
                }
            }
        } else if (status.id == 2)
        {
            tendencyForTransition[6] += GetProbabilityToLieDown() * coefficients.tendencyToStartLyingDown / 3.0f;
            foreach (var sleepZone in sleepInVision)
            {
                if (sleepZone.IsInside(transform.position))
                {
                    tendencyForTransition[6] += GetProbabilityToLieDown() * coefficients.tendencyToStartLyingDown / 3.0f;
                    break;
                }
            }
            if (hasTargetPosition)
            {
                if ((transform.position - targetPosition).magnitude < coefficients.minDistanceToEat)
                {
                    tendencyForTransition[6] += GetProbabilityToLieDown() * coefficients.tendencyToStartLyingDown / 3.0f;
                }
            }
        } else if (status.id == 3)
        {
            tendencyForTransition[4] = 0.0f;
        } else if (status.id == 4)
        {
            tendencyForTransition[4] -= maxProbabilityToFight * coefficients.tendencyToFight;
            tendencyForTransition[4] += coefficients.tendencyToContinueFighting;
        } else if (status.id == 5)
        {
            tendencyForTransition[5] += (hunger - coefficients.hungerForUnstoppableEating) * coefficients.tendencyToContinueEating;
            tendencyForTransition[5] -= coefficients.hungerForUnstoppableEating * coefficients.tendencyToEat;
            tendencyForTransition[5] -= coefficients.tendencyToStayInState;
            tendencyForTransition[4] = 0.0f;
        } else if (status.id == 6)
        {
            tendencyForTransition[6] += (GetProbabilityToLieDown() - coefficients.fatigueForUnstoppableLying) * coefficients.tendencyToContinueLyingDown;
            tendencyForTransition[6] -= coefficients.fatigueForUnstoppableLying * coefficients.tendencyToLieDown;
            tendencyForTransition[6] -= coefficients.tendencyToStayInState;
            tendencyForTransition[4] = 0.0f;
        }

        if (foodInVision.Length == 0)
        {
            tendencyForTransition[1] = 0.0f;
        }
        if (sleepInVision.Length == 0)
        {
            tendencyForTransition[2] = 0.0f;
        }
        if (status.id != 4)
        {
            if (sheepsInFightDistance.Length == 0)
            {
                tendencyForTransition[4] = 0.0f;
            }
            bool foundSheepToFight = false;
            foreach (var sheep in sheepsInFightDistance)
            {
                if (sheep.status.id != 4)
                {
                    foundSheepToFight = true;
                }
            }
            if (!foundSheepToFight)
            {
                tendencyForTransition[4] = 0.0f;
            }
        }

        if (status.id == 4 && sheepToFight != null && sheepToFight.status.id != 4)
        {
            Debug.Log(gameObject.name + " WON!");
            Debug.Log(sheepToFight.gameObject.name + " LOST!");
            tendencyForTransition[4] = 0.0f;
        }
        
        probabilitiesForTransition = ProbabilitiesFromTendencies(tendencyForTransition);
        
        int newStatus = 0;
        if (ChooseWithProbabilities(new float[2]{1 - randomBehaviorProbability, randomBehaviorProbability}) == 0)
        {
            newStatus = ChooseMax(probabilitiesForTransition);
        } else {
            Debug.Log(gameObject.name + " makes random decision");
            newStatus = ChooseWithProbabilities(probabilitiesForTransition);
        }

        ChangeStatus(newStatus, tendencyForTransition);
    }

    void Start()
    {
        var sheepGenerator = FindObjectOfType<SheepGenerator>();
        coefficients = sheepGenerator.GenerateCoefficients();
        rank = sheepGenerator.GenerateRank();
        behaviorUpdateTime = sheepGenerator.GenerateUpdateTime();
        behaviorUpdatedAt = -behaviorUpdateTime;
        randomBehaviorProbability = sheepGenerator.GenerateRandomBehaviorProbability();
    }

    Zone ChooseClosestZone(Zone[] zones)
    {
        Zone closestZone = zones[0];
        float minDistance = (closestZone.colliderMesh.ClosestPoint(transform.position) - transform.position).magnitude;
        foreach (var zone in zones)
        {
            float distance = (zone.colliderMesh.ClosestPoint(transform.position) - transform.position).magnitude;
            if (distance < minDistance)
            {
                closestZone = zone;
                minDistance = distance;
            }
        }
        return closestZone;
    }

    void Update()
    {
        timeInCurrentState = Time.time - timeOfLastTransition;
        timeWalkInDirection += Time.deltaTime;
        if (status.id != 5)
        {
            hunger = Math.Min(1.0f, hunger + coefficients.hungerSpeed * Time.deltaTime);
        }
        if (status.id != 6)
        {
            fatigue = Math.Min(1.0f, fatigue + coefficients.fatigueSpeed * Time.deltaTime);
        }

        if (behaviorUpdatedAt + behaviorUpdateTime <= Time.time)
        {
            behaviorUpdatedAt = Time.time;
            UpdateState();
        }

        if (status.id == 0)
        {
            // Make random walk
            if (direction == Vector3.zero || timeWalkInDirection > 2.0f)
            {
                timeWalkInDirection = 0.0f;
                direction = UnityEngine.Random.onUnitSphere;
                direction -= direction.y * Vector3.up;
                direction /= direction.magnitude;
            }
            speed = 0.5f;
        } else if (status.id == 1)
        {
            if (!hasTargetPosition)
            {
                targetPosition = ChooseClosestZone(foodInVision).GetPointInDistanceFrom(transform.position, coefficients.visionDistance);
                hasTargetPosition = true;
            }
            // Make targeted walk
            direction = targetPosition - transform.position;
            direction -= direction.y * Vector3.up;
            direction /= direction.magnitude;
            speed = 1;
        } else if (status.id == 2)
        {
            if (!hasTargetPosition)
            {
                targetPosition = ChooseClosestZone(sleepInVision).GetPointInDistanceFrom(transform.position, coefficients.visionDistance);
                hasTargetPosition = true;
            }
            // Make targeted walk
            direction = targetPosition - transform.position;
            direction -= direction.y * Vector3.up;
            direction /= direction.magnitude;
            speed = 1;
        } else if (status.id == 3)
        {
            // Make runaway
        } else if (status.id == 4)
        {
            if (sheepToFight == null)
            {
                foreach (var sheep in sheepsInFightDistance)
                {
                    if (sheep.status.id != 4)
                    {
                        sheepToFight = sheep;
                    }
                }
                if (sheepToFight == null)
                {
                    Debug.LogWarning("Не найдена свободная овца для драки");
                    ChangeStatus(0);
                } else {
                    Debug.Log("Change status for " + sheepToFight.name);
                    if (!sheepToFight.ChangeStatus(4))
                    {
                        throw new Exception("FAIL!");
                    }
                    if (sheepToFight.status.id != 4)
                    {
                        throw new Exception("FAIL!");
                    }
                    Debug.Log("Status changed");
                    sheepToFight.sheepToFight = this;
                }
            }
            if (direction == Vector3.zero || timeWalkInDirection > 0.2f)
            {
                timeWalkInDirection = 0.0f;
                direction = UnityEngine.Random.onUnitSphere;
                direction -= direction.y * Vector3.up;
                direction /= direction.magnitude;
            }
            speed = 0.5f;
        } else if (status.id == 5)
        {
            // Make eating
            speed = 0;
            hunger = Math.Max(0.0f, hunger - coefficients.eatSpeed * (0.5f + hunger) * Time.deltaTime);
        } else if (status.id == 6)
        {
            // Make sleeping
            speed = 0;
            fatigue = Math.Max(0.0f, fatigue - coefficients.restSpeed * (0.5f + fatigue) * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        transform.position += direction * speed * Time.fixedDeltaTime;
    }

    int movePos(ref int pos, int step)
    {
        pos += step;
        return pos;
    }
    void OnGUI()
    {
        if (Selection.activeGameObject != gameObject)
        {
            return;
        }
        var textStyle = new GUIStyle();
        textStyle.fontSize = 15;
        int step = textStyle.fontSize;
        int pos = 5;
        EditorGUI.LabelField(new Rect(10, pos, 150, movePos(ref pos, step)), "Status: " + status.name + " " + timeInCurrentState.ToString("0.00") + "s", textStyle);
        string resText = "";
        for (int i = 0; i < tendencyForTransition.Length; ++i)
        {
            resText += tendencyForTransition[i].ToString("0.000") + " ";
        }
        EditorGUI.LabelField(new Rect(10, pos, 350, movePos(ref pos, step)), resText, textStyle);
        resText = "";
        for (int i = 0; i < probabilitiesForTransition.Length; ++i)
        {
            resText += probabilitiesForTransition[i].ToString("0.000") + " ";
        }
        EditorGUI.LabelField(new Rect(10, pos, 350, movePos(ref pos, step)), resText, textStyle);
        EditorGUI.LabelField(new Rect(10, pos, 350, movePos(ref pos, step)), "Hunger: " + hunger.ToString("0.0000"), textStyle);
        EditorGUI.LabelField(new Rect(10, pos, 350, movePos(ref pos, step)), "Fatigue: " + fatigue.ToString("0.0000"), textStyle);
        System.Reflection.FieldInfo[] coefficientsFields = typeof(Sheep.BehaviourCoefficients).GetFields(BindingFlags.Instance |
                                                                                                        BindingFlags.NonPublic |
                                                                                                        BindingFlags.Public);
        foreach (var coefficientField in coefficientsFields)
        {
            if (coefficientField.FieldType.IsArray)
            {
                float[] value = (float[])coefficientField.GetValue(coefficients);
                resText = "";
                if (value != null)
                {
                    for (int i = 0; i < value.Length; ++i)
                    {
                        resText += value[i].ToString("0.00") + " ";
                    }
                }
            } else {
                float value = (float)coefficientField.GetValue(coefficients);
                if (coefficientField.Name == "hungerSpeed" || 
                    coefficientField.Name == "eatSpeed" || 
                    coefficientField.Name == "restSpeed" || 
                    coefficientField.Name == "fatigueSpeed")
                {
                    resText = value.ToString("0.0000");
                } else 
                {
                    resText = value.ToString("0.00");
                }
            }
            EditorGUI.LabelField(new Rect(10, pos, 350, movePos(ref pos, step)), coefficientField.Name + ": " + resText, textStyle);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        // Gizmos.color = new Color(1, 1, 0, 0.5f);
        // foreach (var item in sheepsInVision)
        // {
        //     Gizmos.DrawLine(transform.position, item.transform.position);
        // }
        // Gizmos.color = new Color(0.1f, 1, 0, 0.5f);
        // foreach (var item in foodInVision)
        // {
        //     Gizmos.DrawLine(transform.position, item.transform.position);
        // }
        // Gizmos.color = new Color(0.1f, 0, 1.0f, 0.5F);
        // foreach (var item in sleepInVision)
        // {
        //     Gizmos.DrawLine(transform.position, item.transform.position);
        // }
        Gizmos.color = new Color(1, 0, 0, 0.5F);
        foreach (var item in sheepsInFightDistance)
        {
            Gizmos.DrawLine(transform.position, item.transform.position);
        }
        Gizmos.color = new Color(0.0f, 0.3f, 0.3f, 0.2F);
        Gizmos.DrawWireSphere(transform.position, coefficients.visionDistance);
        Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.5F);
        if (hasTargetPosition)
        {
            Gizmos.DrawSphere(targetPosition, 0.5f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
        if (status.id == 4)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5F);
            Gizmos.DrawSphere(transform.position + Vector3.up, 0.5f);
            Gizmos.DrawSphere(sheepToFight.transform.position + Vector3.up, 0.5f);
        }
    }
}
