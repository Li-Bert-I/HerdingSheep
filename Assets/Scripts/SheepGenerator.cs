using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;


public class SheepGenerator : MonoBehaviour
{
    public float updateFrequency;
    public float randomBehaviorProbability;
    [SerializeField]
    public Sheep.BehaviourCoefficients.MinMaxBehaviourCoefficients minMaxCoefficients;

    static float NextGaussian() 
    {
        float v1, v2, s;
        do 
        {
            v1 = 2.0f * UnityEngine.Random.Range(0.0f, 1.0f) - 1.0f;
            v2 = 2.0f * UnityEngine.Random.Range(0.0f, 1.0f) - 1.0f;
            s = v1 * v1 + v2 * v2;
        } while (s >= 1.0f || s == 0.0f);
        s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);
    
        return v1 * s;
    }

    static float TruncatedNextGaussian(float min, float max, float center=-10000.0f, float curvature=1.0f)
    {
        if (min == max)
        {
            return min;
        }
        if (center == -10000.0f) 
        {
            center = (max + min) / 2.0f;
        }

        float left = 2 * (center - min) * curvature / (max - min);
        float right = 2 * (max - center) * curvature / (max - min);

        float res = NextGaussian();
        if (res < -curvature || res > curvature)
        {
            res = UnityEngine.Random.Range(-curvature, curvature);
        }

        res = (res - (-left + right) / 2.0f) * (max - min) / 2.0f + (min + max) / 2.0f;
        return res;
    }

    public Sheep.BehaviourCoefficients GenerateCoefficients()
    {
        var coefficients = new Sheep.BehaviourCoefficients();
        object boxed = (object)coefficients;
        System.Reflection.FieldInfo[] coefficientsFields = typeof(Sheep.BehaviourCoefficients).GetFields(BindingFlags.Instance |
                                                                                                        BindingFlags.NonPublic |
                                                                                                        BindingFlags.Public);
        System.Reflection.FieldInfo[] minMaxFields = 
            typeof(Sheep.BehaviourCoefficients.MinMaxBehaviourCoefficients).GetFields(BindingFlags.Instance |
                                                                                     BindingFlags.NonPublic |
                                                                                     BindingFlags.Public);
        foreach (var coefficientField in coefficientsFields)
        {
            System.Reflection.FieldInfo minMaxField = minMaxFields[0];
            bool found = false;
            foreach (var field in minMaxFields)
            {
                if (field.Name == coefficientField.Name)
                {
                    minMaxField = field;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                throw new Exception("Не найдены минимальное/максимальное значение для коэффициентов поведения овцы");
            }
            
            if (minMaxField.FieldType.IsArray)
            {
                Sheep.BehaviourCoefficients.MinMaxDistribution[] minMaxArray = 
                    (Sheep.BehaviourCoefficients.MinMaxDistribution[])minMaxField.GetValue(minMaxCoefficients);
                float[] res = new float[minMaxArray.Length];
                for (int i = 0; i < minMaxArray.Length; ++i)
                {
                    res[i] = TruncatedNextGaussian(minMaxArray[i].min, minMaxArray[i].max, minMaxArray[i].center);
                }
                coefficientField.SetValue(boxed, res);
            } else {
                Sheep.BehaviourCoefficients.MinMaxDistribution minMaxValue = new Sheep.BehaviourCoefficients.MinMaxDistribution();
                minMaxValue = (Sheep.BehaviourCoefficients.MinMaxDistribution)minMaxField.GetValue(minMaxCoefficients);
                float res = TruncatedNextGaussian(minMaxValue.min, minMaxValue.max, minMaxValue.center);
                // Debug.Log(string.Format("Got min: {0}, max: {1}, center: {2}, return: {3}", minMaxValue.min, minMaxValue.max, minMaxValue.center, res));
                coefficientField.SetValue(boxed, res);
                // Debug.Log(string.Format("Result: {0} has value {1}", coefficientField.Name, (float)coefficientField.GetValue(coefficients)));
            }
        }
        coefficients = (Sheep.BehaviourCoefficients)boxed;
        return coefficients;
    }

    public float GenerateRank()
    {
        return UnityEngine.Random.Range(0.0f, 1.0f);
    }

    public float GenerateUpdateTime()
    {
        return 1 / updateFrequency;
    }

    public float GenerateRandomBehaviorProbability()
    {
        return randomBehaviorProbability;
    }
}
